using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Соединяет сервер, каналы и клиентов и играет набор партий. Владеет сервером и закрывает его.
/// Один объект рассчитан на один вызов Run
/// </summary>
public sealed class GameSimulation : IDisposable
{
    private readonly IGameServer _server;
    private readonly GameRules _rules;
    private readonly DelayedChannel<ClientMessage> _up;
    private readonly DelayedChannel<ServerMessage>[] _down;
    private readonly Dictionary<(int Game, int Player), GameClient> _clients =
        new Dictionary<(int Game, int Player), GameClient>();
    private readonly ManualResetEventSlim _failed = new ManualResetEventSlim();
    private CountdownEvent? _finished;
    private volatile string? _failure;
    private volatile bool _stopped;

    public GameSimulation(IGameServer server, GameRules rules, int delayMs)
    {
        _server = server;
        _rules = rules;
        _up = new DelayedChannel<ClientMessage>(delayMs, server.Submit);
        _down = new[]
        {
            new DelayedChannel<ServerMessage>(delayMs, m => _clients[(m.GameId, 0)].OnServerMessage(m)),
            new DelayedChannel<ServerMessage>(delayMs, m => _clients[(m.GameId, 1)].OnServerMessage(m))
        };
        server.Updated += OnServerUpdated;
    }

    /// <summary>Сервер сообщил результат: успех получают оба игрока, отказ только приславший команду</summary>
    private void OnServerUpdated(ServerMessage message)
    {
        if (_stopped) return;

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

    private void SendUp(ClientMessage message)
    {
        if (!_stopped) _up.Send(message);
    }

    private void Fail(string reason)
    {
        _failure = reason;
        _failed.Set();
    }

    /// <summary>Играет партии с указанными seed до победы и возвращает итоги</summary>
    public SimulationResult Run(IReadOnlyList<int> seeds, TimeSpan timeout)
    {
        var result = new SimulationResult();
        var finished = new CountdownEvent(seeds.Count * 2);
        _finished = finished;

        foreach (int seed in seeds)
        {
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

        _server.Start();
        foreach (var client in _clients.Values)
            client.Start();

        int index = WaitHandle.WaitAny(new[] { finished.WaitHandle, _failed.WaitHandle }, timeout);
        if (index == WaitHandle.WaitTimeout)
            throw new TimeoutException("Games did not finish in time");
        if (index == 1)
            throw new InvalidOperationException(_failure);

        foreach (var client in _clients.Values)
            result.LatenciesMs.AddRange(client.LatenciesMs);

        return result;
    }

    public void Dispose()
    {
        _stopped = true;
        _up.Dispose();
        _server.Dispose();
        foreach (var channel in _down)
            channel.Dispose();
        _finished?.Dispose();
        _failed.Dispose();
    }
}