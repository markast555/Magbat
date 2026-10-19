using System.Diagnostics;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Клиент одного игрока в одной партии: хранит свою копию состояния, выбирает ходы
/// и обновляет копию по сообщениям сервера
/// </summary>
public sealed class GameClient
{
    private readonly int _gameId;
    private readonly int _player;
    private readonly GameRules _rules;
    private readonly RandomBot _bot;
    private readonly Action<ClientMessage> _send;
    private readonly Action _onFinished;
    private readonly Action<string> _onFailed;
    private bool _finished;

    /// <summary>Собственная копия состояния партии</summary>
    public GameState State { get; }

    /// <summary>Задержки отклика на собственные команды, мс</summary>
    public List<double> LatenciesMs { get; } = new List<double>();

    public GameClient(int gameId, int player, GameState state, GameRules rules, RandomBot bot,
        Action<ClientMessage> send, Action onFinished, Action<string> onFailed)
    {
        _gameId = gameId;
        _player = player;
        State = state;

        _rules = rules;
        _bot = bot;
        _send = send;
        _onFinished = onFinished;
        _onFailed = onFailed;
    }

    /// <summary>Первый ход делает тот, чья очередь в начале партии</summary>
    public void Start()
    {
        if (State.CurrentPlayer == _player)
            SendNext();
    }

    /// <summary>Вызывается каналом «вниз», когда пришло сообщение сервера</summary>
    public void OnServerMessage(ServerMessage message)
    {
        if (message.Player == _player)
        {
            LatenciesMs.Add(Stopwatch.GetElapsedTime(message.CommandSentAt).TotalMilliseconds);

            if (message.Error != ApplyError.None)
            {
                _onFailed($"Game {_gameId}, player {_player}: server rejected the command ({message.Error})");
                return;
            }
        }

        ChangeApplier.Apply(State, message.Changes);

        if (State.Winner != null)
        {

            if (!_finished)
            {
                _finished = true;
                _onFinished();
            }
            return;
        }

        if (State.CurrentPlayer == _player)
            SendNext();
    }

    private void SendNext()
    {
        var command = _bot.Choose(_rules.GetLegalCommands(State));
        _send(new ClientMessage(_gameId, command, Stopwatch.GetTimestamp()));
    }
}