using System.Diagnostics;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Соединяет сервер, каналы и клиентов и играет набор партий
/// </summary>
/// <remarks>
/// Владеет сервером и закрывает его при Dispose. Один объект рассчитан на один вызов Run или Measure
/// </remarks>
public sealed class GameSimulation : IDisposable
{
    private readonly IGameServer _server;
    private readonly GameRules _rules;
    private readonly SimulationOptions _options;
    private readonly DelayedChannel<ClientMessage> _up;
    private readonly DelayedChannel<ServerMessage>[] _down;
    // Заполняется целиком до запуска потоков и потом только читается,
    // поэтому защита от многопоточности не нужна
    private readonly Dictionary<(int Game, int Player), GameClient> _clients =
        new Dictionary<(int Game, int Player), GameClient>();
    private readonly ManualResetEventSlim _failed = new ManualResetEventSlim(); // флаг: клиент сообщил об ошибке
    private CountdownEvent? _finished; // считает клиентов, у которых партия закончилась
    // volatile: значения меняются в одних потоках, а читаются в других
    private volatile string? _failure; // причина сбоя
    private volatile bool _stopped;    // прогон закончен, новые сообщения не отправляются
    private bool _disposed;

    /// <summary>
    /// Создаёт симуляцию без пауз ботов и без подсчёта трафика
    /// </summary>
    /// <param name="server">Сервер</param>
    /// <param name="rules">Правила игры</param>
    /// <param name="delayMs"> Задержка каждого канала, мс</param>
    public GameSimulation(IGameServer server, GameRules rules, int delayMs)
        : this(server, rules, new SimulationOptions { DelayMs = delayMs })
    {
    }

    /// <summary>
    /// Создаёт каналы и подключает их к серверу
    /// </summary>
    /// <param name="server">Сервер</param>
    /// <param name="rules">Правила игры</param>
    /// <param name="options">Параметры симуляции</param>
    public GameSimulation(IGameServer server, GameRules rules, SimulationOptions options)
    {
        _server = server;
        _rules = rules;
        _options = options;

        // Размер считается в каналах, а не в серверах: сериализация не должна попадать в нагрузку сервера
        var upSize = options.CountTraffic ? (Func<ClientMessage, int>)MessageSize.Of : null;
        var downSize = options.CountTraffic ? (Func<ServerMessage, int>)MessageSize.Of : null;

        _up = new DelayedChannel<ClientMessage>(options.DelayMs, server.Submit, upSize);
        // Каналов «вниз» два: по одному на номер игрока, общие для всех партий.
        _down = new[]
        {
            new DelayedChannel<ServerMessage>(options.DelayMs, m => _clients[(m.GameId, 0)].OnServerMessage(m), downSize),
            new DelayedChannel<ServerMessage>(options.DelayMs, m => _clients[(m.GameId, 1)].OnServerMessage(m), downSize)
        };
        server.Updated += OnServerUpdated;
    }

    /// <summary>
    /// Рассылает результат команды клиентам через каналы «вниз»
    /// </summary>
    /// <remarks>Вызывается в потоке сервера, поэтому только кладёт сообщение в канал</remarks>
    /// <param name="message">Сообщение сервера</param>
    private void OnServerUpdated(ServerMessage message)
    {
        if (_stopped) return;

        // Успех получают оба игрока, отказ только приславший команду
        if (message.Error == ApplyError.None)
        {
            _down[0].Send(message);
            _down[1].Send(message);
        }
        else
        {
            _down[message.Player].Send(message);
        }
    }

    /// <summary>
    /// Отправляет команду клиента серверу через канал «вверх»
    /// </summary>
    /// <remarks>
    /// После остановки прогона сообщения отбрасываются: отправка в закрытый канал вызвала бы исключение
    /// </remarks>
    /// <param name="message">Сообщение клиента</param>
    private void SendUp(ClientMessage message)
    {
        if (_stopped) return;

        try
        {
            _up.Send(message);
        }
        catch (Exception e) when (_stopped && (e is InvalidOperationException || e is ObjectDisposedException))
        {
            // Канал закрыт между проверкой и отправкой, команда уже не нужна
        }
    }

    /// <summary>
    /// Запоминает причину сбоя и будит основной поток
    /// </summary>
    /// <param name="reason">Описание ошибки</param>
    private void Fail(string reason)
    {
        _failure = reason;
        _failed.Set();
    }

    /// <summary>
    /// Играет партии до победы во всех и возвращает их итоги
    /// </summary>
    /// <param name="seeds">
    /// Номера партий. Каждый служит и основой для генераторов случайных чисел ботов
    /// </param>
    /// <param name="timeout">Предельное время ожидания окончания всех партий</param>
    /// <returns>Итоговые состояния и отклики</returns>
    /// <exception cref="TimeoutException">
    /// Партии не закончились за отведённое время
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Повторный вызов,
    /// Клиент сообщил об ошибке
    /// </exception>
    public SimulationResult Run(IReadOnlyList<int> seeds, TimeSpan timeout)
    {
        var (result, finished, firstMovers) = Prepare(seeds);
        Launch(firstMovers);

        // Ждём, пока сработает одно из двух событий или истечёт время.
        // WaitAny возвращает номер сработавшего: 0 партии закончены, 1 сбой, WaitTimeout время вышло
        int index = WaitHandle.WaitAny(new[] { finished.WaitHandle, _failed.WaitHandle }, timeout);
        if (index == WaitHandle.WaitTimeout)
            throw new TimeoutException("Games did not finish in time");
        if (index == 1)
            throw new InvalidOperationException(_failure);

        foreach (var client in _clients.Values)
            result.LatenciesMs.AddRange(client.LatenciesMs);

        return result;
    }

    /// <summary>
    /// Играет партии заданное время и возвращает измерения
    /// </summary>
    /// <remarks>После вызова симуляция остановлена</remarks>
    /// <param name="seeds">Номера партий</param>
    /// <param name="duration">Длительность окна замера</param>
    /// <returns>Измерения за окно замера</returns>
    /// <exception cref="InvalidOperationException">
    /// Повторный вызов,
    /// Клиент сообщил об ошибке
    /// </exception>
    public RunMetrics Measure(IReadOnlyList<int> seeds, TimeSpan duration)
    {
        var (_, finished, firstMovers) = Prepare(seeds);
        using var process = Process.GetCurrentProcess();

        // Базовые значения берутся прямо перед запуском
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        var processCpuAtStart = process.TotalProcessorTime;
        long startedAt = Stopwatch.GetTimestamp();

        Launch(firstMovers);
        int index = WaitHandle.WaitAny(new[] { finished.WaitHandle, _failed.WaitHandle }, duration);
        if (index == 1)
            throw new InvalidOperationException(_failure);

        // Конец окна: сначала запрещаем новую отправку, затем снимаем счётчики до остановки потоков
        _stopped = true;
        var metrics = new RunMetrics
        {
            ElapsedSec = Stopwatch.GetElapsedTime(startedAt).TotalSeconds,
            GamesFinished = (seeds.Count * 2 - finished.CurrentCount) / 2,
            UpMessages = _up.MessagesDelivered,
            UpBytes = _up.BytesDelivered,
            DownMessages = _down.Sum(c => c.MessagesDelivered),
            DownBytes = _down.Sum(c => c.BytesDelivered),
            ServerBusyMs = _server.Stats.BusyMs,
            ServerCpuMs = _server.Stats.ReadCpuTime()?.TotalMilliseconds,
            ServerAllocatedBytes = _server.Stats.AllocatedBytes,
            Gc0 = GC.CollectionCount(0) - gc0,
            Gc1 = GC.CollectionCount(1) - gc1,
            Gc2 = GC.CollectionCount(2) - gc2
        };
        process.Refresh();
        metrics.ProcessCpuMs = (process.TotalProcessorTime - processCpuAtStart).TotalMilliseconds;

        // Списки откликов читаются после остановки потоков, пока они ещё меняются
        Dispose();
        foreach (var client in _clients.Values)
            metrics.LatenciesMs.AddRange(client.LatenciesMs);

        return metrics;
    }

    /// <summary>
    /// Создаёт партии и клиентов
    /// </summary>
    /// <param name="seeds">Номера партий</param>
    /// <returns>Результат, счётчик окончаний и клиенты, которые ходят первыми</returns>
    /// <exception cref="InvalidOperationException">Повторный вызов</exception>
    private (SimulationResult Result, CountdownEvent Finished, List<GameClient> FirstMovers) Prepare(
        IReadOnlyList<int> seeds)
    {
        if (_finished != null)
            throw new InvalidOperationException("A simulation can be run only once");

        var result = new SimulationResult();
        var finished = new CountdownEvent(seeds.Count * 2);
        _finished = finished;
        var firstMovers = new List<GameClient>();

        foreach (int seed in seeds)
        {
            // Серверу и клиентам нужны разные объекты состояния: у клиентов собственные копии
            var serverState = GameFactory.NewGame();
            result.ServerStates[seed] = serverState;
            _server.AddGame(seed, serverState);

            for (int player = 0; player < 2; player++)
            {
                // Seed пауз отличается от seed бота, чтобы паузы не влияли на выбор ходов
                var pacing = _options.MeanThinkMs > 0
                    ? new Pacing(_options.MeanThinkMs, seed * 2 + player + 7919)
                    : null;

                var client = new GameClient(
                    seed, player, GameFactory.NewGame(), _rules,
                    RandomBot.ForGame(seed, player),
                    SendUp, () => finished.Signal(), Fail, pacing);
                _clients[(seed, player)] = client;
                result.ClientStates[(seed, player)] = client.State;

                // Запускать нужно только того, кто ходит первым. Второй клиент ждёт сообщения сервера:
                // если бы его запускал основной поток, он мог бы отправить вторую команду параллельно
                // с обработкой ответа в потоке канала
                if (client.State.CurrentPlayer == player)
                    firstMovers.Add(client);
            }
        }

        return (result, finished, firstMovers);
    }

    /// <summary>
    /// Запускает клиентов и сервер
    /// </summary>
    /// <param name="firstMovers">Клиенты, которые ходят первыми</param>
    private void Launch(List<GameClient> firstMovers)
    {
        foreach (var client in firstMovers)
            client.Start();

        // Сервер запускается после клиентов: в режиме снимков он рассылает состояние
        // без команд, и клиент мог бы получить его во время запуска
        _server.Start();
    }

    /// <summary>
    /// Останавливает каналы и сервер
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Порядок идёт по пути сообщения: сначала запрещаем новую отправку, затем закрываем
        // канал «вверх» (он доставит остаток серверу), потом сервер и каналы «вниз»
        _stopped = true;
        _up.Dispose();
        _server.Dispose();
        foreach (var channel in _down)
            channel.Dispose();
        _finished?.Dispose();
        _failed.Dispose();
    }
}