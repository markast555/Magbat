using System.Diagnostics;
using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Клиент одного игрока в одной партии: хранит свою копию состояния, выбирает ходы
/// и обновляет копию по сообщениям сервера
/// </summary>
public sealed class GameClient
{
    // Сообщения сервера и запланированные ходы приходят из разных потоков
    private readonly object _sync = new object();
    
    private readonly int _gameId;
    private readonly int _player;
    private readonly GameRules _rules;
    private readonly RandomBot _bot;
    private readonly Action<ClientMessage> _send; // отправляет команду серверу
    private readonly Action _onFinished;          // вызывается, когда партия окончена
    private readonly Action<string> _onFailed;    // вызывается при ошибке, получает её описание
    private readonly Pacing? _pacing;
    private bool _finished;
    private bool _awaitingResponse; // флаг ожидания, нужен при периодических снимках

    // Ход запланирован, но ещё не отправлен
    private bool _moveScheduled;
    
    /// <summary>Собственная копия состояния партии</summary>
    public GameState State { get; }

    /// <summary>Задержки отклика на собственные команды, мс</summary>
    public List<double> LatenciesMs { get; } = new List<double>();

    /// <summary>
    /// Создаёт клиента
    /// </summary>
    /// <remarks> Для начала игры нужно вызвать Start</remarks>
    /// <param name="gameId">ID партии</param>
    /// <param name="player">Номер игрока (0 или 1)</param>
    /// <param name="state">Собственная копия начального состояния</param>
    /// <param name="rules">Правила игры</param>
    /// <param name="bot">Бот, выбирающий ходы</param>
    /// <param name="send">Метод отправки команды серверу</param>
    /// <param name="onFinished">Метод, вызываемый при окончании партии</param>
    /// <param name="onFailed">Метод, вызываемый при ошибке</param>
    /// <param name="pacing">Паузы перед ходами</param>
    public GameClient(int gameId, int player, GameState state, GameRules rules, RandomBot bot,
        Action<ClientMessage> send, Action onFinished, Action<string> onFailed, Pacing? pacing = null)
    {
        _gameId = gameId;
        _player = player;
        State = state;

        _rules = rules;
        _bot = bot;
        _send = send;
        _onFinished = onFinished;
        _onFailed = onFailed;
        _pacing = pacing;
    }

    /// <summary>
    /// Делает первый ход
    /// </summary>
    /// <remarks>Вызывать только для игрока, который ходит первым</remarks>
    public void Start()
    {
        lock (_sync)
            PlanMove();
    }

    /// <summary>
    /// Обновляет копию состояния по сообщению сервера и делает следующий ход, если очередь клиента
    /// </summary>
    /// <remarks>Вызывается в потоке канала «вниз»</remarks>
    /// <param name="message">Сообщение сервера</param>
    public void OnServerMessage(ServerMessage message)
    {
        lock (_sync) 
        {
            if (message.Player == _player)
            {
                _awaitingResponse = false;
                LatenciesMs.Add(Stopwatch.GetElapsedTime(message.CommandSentAt).TotalMilliseconds);

                // Бот выбирает только допустимые команды, поэтому отказ означает ошибку в программе
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

            if (State.CurrentPlayer == _player && !_awaitingResponse)
                PlanMove();
        }
    }
    
    /// <summary>
    /// Отправляет ход сразу или после паузы
    /// </summary>
    private void PlanMove()
    {
        if (_moveScheduled) return;

        if (_pacing == null)
        {
            SendNext();
            return;
        }

        _moveScheduled = true;
        _pacing.Schedule(SendScheduled);
    }
    
    /// <summary>
    /// Отправляет запланированный ход, если он всё ещё актуален
    /// </summary>
    private void SendScheduled()
    {
        // Исключение в потоке планировщика завершило бы программу, поэтому передаём его наверх
        try
        {
            lock (_sync)
            {
                _moveScheduled = false;
                if (State.Winner == null && State.CurrentPlayer == _player && !_awaitingResponse)
                    SendNext();
            }
        }
        catch (Exception e)
        {
            _onFailed(e.Message);
        }
    }

    /// <summary>
    /// Выбирает ход ботом по своей копии состояния и отправляет его серверу
    /// </summary>
    private void SendNext()
    {
        _awaitingResponse = true;
        
        var command = _bot.Choose(_rules.GetLegalCommands(State));
        _send(new ClientMessage(_gameId, command, Stopwatch.GetTimestamp()));
    }
}