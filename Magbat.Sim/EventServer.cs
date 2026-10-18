using System.Collections.Concurrent;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Сервер с событийной обработкой: команда обрабатывается сразу при поступлении
/// </summary>
public sealed class EventServer : IGameServer
{
    private readonly GameRules _rules;
    private readonly ConcurrentDictionary<int, GameState> _games = new();
    private readonly BlockingCollection<ClientMessage> _incoming = new();
    private Thread? _thread;

    public event Action<ServerMessage>? Updated;

    public EventServer(GameRules rules)
    {
        _rules = rules;
    }

    public void AddGame(int gameId, GameState initialState)
    {
        if (!_games.TryAdd(gameId, initialState))
            throw new ArgumentException($"Game {gameId} already exists");
    }

    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Server is already started");


        _thread = new Thread(Run) { IsBackground = true, Name = "EventServer" };
        _thread.Start();
    }

    public void Submit(ClientMessage message)
    {
        _incoming.Add(message);
    }

    private void Run()
    {
        foreach (var message in _incoming.GetConsumingEnumerable())
            Process(message);
    }

    private void Process(ClientMessage message)
    {
        if (!_games.TryGetValue(message.GameId, out var state))
            throw new InvalidOperationException($"Unknown game {message.GameId}");

        var result = _rules.Apply(state, message.Command);

        Updated?.Invoke(new ServerMessage(
            message.GameId,
            message.Command.Player,
            message.SentAt,
            result.Error,
            result.Changes));
    }

    public void Dispose()

    {
        _incoming.CompleteAdding();
        _thread?.Join();
        _incoming.Dispose();
    }
}