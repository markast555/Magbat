using System.Collections.Concurrent;
using System.Diagnostics;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Сервер с событийной обработкой: команда обрабатывается сразу при поступлении
/// </summary>
public sealed class EventServer : IGameServer
{
    private readonly GameRules _rules;
    // Партии добавляются из основного потока, а читаются из потока сервера
    private readonly ConcurrentDictionary<int, GameState> _games = new();
    private readonly BlockingCollection<ClientMessage> _incoming = new();
    private Thread? _thread;

    /// <summary>
    /// Возникает после обработки каждой команды
    /// </summary>
    /// <remarks>Подписчики вызываются в потоке сервера, поэтому должны работать быстро</remarks>
    public event Action<ServerMessage>? Updated;
    
    public ServerStats Stats { get; } = new ServerStats();

    /// <summary>
    /// Создаёт сервер
    /// </summary>
    /// <remarks>Для начала работы нужно вызвать Start</remarks>
    /// <param name="rules">Правила игры</param>
    public EventServer(GameRules rules)
    {
        _rules = rules;
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
    /// Запускает поток обработки команд
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Была совершена попытка повторного запуска
    /// </exception>
    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Server is already started");

        _thread = new Thread(Run) { IsBackground = true, Name = "EventServer" };
        _thread.Start();
    }

    /// <summary>
    /// Ставит сообщение клиента в очередь на обработку
    /// </summary>
    /// <remarks>
    /// Не ожидает обработки: команда выполняется потоком сервера, результат приходит через Updated.
    /// После Dispose вызывать нельзя
    /// </remarks>
    /// <param name="message">Сообщение клиента</param>
    public void Submit(ClientMessage message)
    {
        _incoming.Add(message);
    }

    /// <summary>
    /// Обрабатывает сообщения из очереди по одному
    /// </summary>
    private void Run()
    {
        Stats.OnThreadStart();
        
        // Когда очередь пуста, поток спит: в этом отличие от циклического сервера,
        // который просыпается по таймеру независимо от наличия команд
        foreach (var message in _incoming.GetConsumingEnumerable())
            Process(message);
    }

    /// <summary>
    /// Применяет команду к партии и сообщает результат подписчикам
    /// </summary>
    /// <param name="message">Сообщение клиента</param>
    /// <exception cref="InvalidOperationException">
    /// Передан ID несуществующей игры
    /// </exception>
    private void Process(ClientMessage message)
    {
        long started = Stopwatch.GetTimestamp();
        
        if (!_games.TryGetValue(message.GameId, out var state))
            throw new InvalidOperationException($"Unknown game {message.GameId}");

        var result = _rules.Apply(state, message.Command);

        Updated?.Invoke(new ServerMessage(
            message.GameId,
            message.Command.Player,
            message.SentAt,
            result.Error,
            result.Changes));
        
        Stats.AddBusy(started);
    }

    /// <summary>
    /// Завершает работу сервера после обработки уже принятых команд
    /// </summary>
    public void Dispose()
    {
        _incoming.CompleteAdding();
        _thread?.Join();
        _incoming.Dispose();
    }
}