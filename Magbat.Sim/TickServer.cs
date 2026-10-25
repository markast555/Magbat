using System.Collections.Concurrent;
using System.Diagnostics;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Сервер с циклической обработкой: команды накапливаются и обрабатываются на тиках
/// </summary>
public sealed class TickServer : IGameServer
{
    private readonly GameRules _rules;
    private readonly BroadcastMode _mode;
    private readonly long _intervalTicks;
    private readonly ConcurrentDictionary<int, GameState> _games = new();
    private readonly ConcurrentQueue<ClientMessage> _incoming = new();

    // Используются только в потоке сервера
    private readonly Dictionary<int, (int Player, long SentAt)> _pending = new();
    private readonly HashSet<int> _finishedGames = new();
    private long _lastTickAt;
    private long _intervals;
    private double _intervalSumMs;
    private double _minIntervalMs = double.MaxValue;
    private double _maxIntervalMs;

    private Thread? _thread;
    private volatile bool _stopping;

    public event Action<ServerMessage>? Updated;

    /// <summary>Число выполненных тиков</summary>
    /// <remarks>Читать после Dispose</remarks>
    public long Ticks { get; private set; }

    /// <summary>Число тиков без единой команды</summary>
    /// <remarks>Читать после Dispose</remarks>
    public long IdleTicks { get; private set; }

    /// <summary>Средний фактический интервал между тиками, мс</summary>
    /// <remarks>Читать после Dispose</remarks>
    public double MeanIntervalMs => _intervals > 0 ? _intervalSumMs / _intervals : 0;

    /// <summary>Минимальный фактический интервал между тиками, мс</summary>
    /// <remarks>Читать после Dispose</remarks>
    public double MinIntervalMs => _intervals > 0 ? _minIntervalMs : 0;

    /// <summary>Максимальный фактический интервал между тиками, мс</summary>
    /// <remarks>Читать после Dispose</remarks>
    public double MaxIntervalMs => _maxIntervalMs;

    /// <summary>
    /// Создаёт сервер
    /// </summary>
    /// <remarks>Для начала работы нужно вызвать Start</remarks>
    /// <param name="rules">Правила игры</param>
    /// <param name="tickRateHz">Частота тиков, Гц</param>
    /// <param name="mode">Режим рассылки состояния</param>
    public TickServer(GameRules rules, int tickRateHz, BroadcastMode mode)
    {
        if (tickRateHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));

        _rules = rules;
        _mode = mode;
        _intervalTicks = Stopwatch.Frequency / tickRateHz;
    }

    /// <summary>
    /// Регистрирует партию на сервере
    /// </summary>
    /// <param name="gameId">ID игры</param>
    /// <param name="initialState">Состояние игры</param>
    /// <exception cref="ArgumentException">
    /// Передан ID уже добавленной игры
    /// </exception>
    public void AddGame(int gameId, GameState initialState)
    {
        if (!_games.TryAdd(gameId, initialState))
            throw new ArgumentException($"Game {gameId} already exists");
    }

    /// <summary>
    /// Запускает цикл тиков
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Была совершена попытка повторного запуска
    /// </exception>
    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Server is already started");

        _thread = new Thread(Run) { IsBackground = true, Name = "TickServer" };
        _thread.Start();
    }

    /// <summary>
    /// Ставит сообщение клиента в очередь до ближайшего тика
    /// </summary>
    public void Submit(ClientMessage message)
    {
        _incoming.Enqueue(message);
    }

    /// <summary>
    /// Выполняет тики по расписанию до остановки
    /// </summary>
    private void Run()
    {
        // Расписание считается от начальной точки: ошибки ожидания не накапливаются
        long next = Stopwatch.GetTimestamp() + _intervalTicks;
        while (!_stopping)
        {
            WaitUntil(next);
            if (_stopping) break;

            RecordInterval(Stopwatch.GetTimestamp());
            int processed = Tick();
            Ticks++;
            if (processed == 0) IdleTicks++;

            next += _intervalTicks;

            // Если отстали больше чем на тик, не догоняем пачкой тиков подряд
            if (Stopwatch.GetTimestamp() - next > _intervalTicks)
                next = Stopwatch.GetTimestamp();
        }

        // Команды, принятые до остановки, не теряем
        Tick();
    }

    /// <summary>
    /// Ожидает наступления момента тика
    /// </summary>
    private void WaitUntil(long dueAt)
    {
        // Без активного ожидания: оно давало бы постоянную нагрузку на процессор,
        // не связанную с самой циклической моделью. Цена: интервалы между тиками дрожат
        while (!_stopping)
        {
            long remaining = dueAt - Stopwatch.GetTimestamp();
            if (remaining <= 0) return;

            Thread.Sleep(Math.Max(1, (int)(remaining * 1000 / Stopwatch.Frequency)));
        }
    }

    /// <summary>
    /// Фиксирует фактический интервал между тиками
    /// </summary>
    private void RecordInterval(long now)
    {
        if (_lastTickAt != 0)
        {
            double ms = (now - _lastTickAt) * 1000.0 / Stopwatch.Frequency;
            _intervals++;
            _intervalSumMs += ms;
            if (ms < _minIntervalMs) _minIntervalMs = ms;
            if (ms > _maxIntervalMs) _maxIntervalMs = ms;
        }
        _lastTickAt = now;
    }

    /// <summary>
    /// Обрабатывает накопленные команды и рассылает состояние
    /// </summary>
    /// <returns>Число обработанных команд</returns>
    private int Tick()
    {
        int processed = 0;
        while (_incoming.TryDequeue(out var message))
        {
            Process(message);
            processed++;
        }

        if (_mode == BroadcastMode.FullSnapshot)
            BroadcastSnapshots();

        return processed;
    }

    /// <summary>
    /// Применяет команду и сообщает результат или откладывает его до снимка
    /// </summary>
    private void Process(ClientMessage message)
    {
        if (!_games.TryGetValue(message.GameId, out var state))
            throw new InvalidOperationException($"Unknown game {message.GameId}");

        var result = _rules.Apply(state, message.Command);

        if (_mode == BroadcastMode.Delta || !result.Success)
        {
            Updated?.Invoke(new ServerMessage(
                message.GameId, message.Command.Player, message.SentAt, result.Error, result.Changes));
            return;
        }

        // В режиме снимков ответ на команду становится частью снимка в конце тика
        _pending[message.GameId] = (message.Command.Player, message.SentAt);
    }

    /// <summary>
    /// Рассылает полный снимок каждой активной партии
    /// </summary>
    private void BroadcastSnapshots()
    {
        foreach (var pair in _games)
        {
            if (_finishedGames.Contains(pair.Key)) continue;

            // -1: сообщение не отвечает ни на чью команду
            int player = -1;
            long sentAt = 0;
            if (_pending.TryGetValue(pair.Key, out var pending))
            {
                player = pending.Player;
                sentAt = pending.SentAt;
                _pending.Remove(pair.Key);
            }

            Updated?.Invoke(new ServerMessage(
                pair.Key, player, sentAt, ApplyError.None,
                new Change[] { StateSnapshot.From(pair.Value) }));

            // После финального снимка партию больше не рассылаем
            if (pair.Value.Winner != null)
                _finishedGames.Add(pair.Key);
        }
    }

    /// <summary>
    /// Останавливает сервер после обработки уже принятых команд
    /// </summary>
    public void Dispose()
    {
        _stopping = true;
        _thread?.Join();
    }
}