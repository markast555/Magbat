using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Соединяет сервер, каналы и клиентов и играет набор партий
/// </summary>
/// <remarks>
/// Владеет сервером и закрывает его при Dispose. Один объект рассчитан на один вызов Run
/// </remarks>
public sealed class GameSimulation : IDisposable
{
    private readonly IGameServer _server;
    private readonly GameRules _rules;
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

    /// <summary>
    /// Создаёт каналы и подключает их к серверу
    /// </summary>
    /// <param name="server">Сервер</param>
    /// <param name="rules">Правила игры</param>
    /// <param name="delayMs"> Задержка каждого канала, мс</param>
    public GameSimulation(IGameServer server, GameRules rules, int delayMs)
    {
        _server = server;
        _rules = rules;
        _up = new DelayedChannel<ClientMessage>(delayMs, server.Submit);
        // Каналов «вниз» два: по одному на номер игрока, общие для всех партий.
        _down = new[]
        {
            new DelayedChannel<ServerMessage>(delayMs, m => _clients[(m.GameId, 0)].OnServerMessage(m)),
            new DelayedChannel<ServerMessage>(delayMs, m => _clients[(m.GameId, 1)].OnServerMessage(m))
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
        if (!_stopped) _up.Send(message);
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
    /// Клиент сообщил об ошибке
    /// </exception>
    public SimulationResult Run(IReadOnlyList<int> seeds, TimeSpan timeout)
    {
        var result = new SimulationResult();
        var finished = new CountdownEvent(seeds.Count * 2);
        _finished = finished;

        foreach (int seed in seeds)
        {
            // Серверу и клиентам нужны разные объекты состояния: у клиентов собственные копии
            var serverState = GameFactory.NewGame();
            result.ServerStates[seed] = serverState;
            _server.AddGame(seed, serverState);
            
            for (int player = 0; player < 2; player++)
            {
                var client = new GameClient(
                    seed, player, GameFactory.NewGame(), _rules,
                    new RandomBot(seed * 2 + player),
                    SendUp, () => finished.Signal(), Fail);
                _clients[(seed, player)] = client;
                result.ClientStates[(seed, player)] = client.State;
            }
        }

        // Клиенты запускаются после создания всех: иначе сообщение могло бы прийти
        // к клиенту, которого ещё нет в словаре
        _server.Start();
        foreach (var client in _clients.Values)
            client.Start();

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
    /// Останавливает каналы и сервер
    /// </summary>
    public void Dispose()
    {
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