using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Сообщение, отправляемое клиентом серверу
/// </summary>
public class ClientMessage
{
    /// <summary>Номер партии</summary>
    public int GameId { get; }
    /// <summary>Команда игрока</summary>
    public Command Command { get; }
    /// <summary>Время отправки команды, тики Stopwatch</summary>
    public long SentAt { get; }

    public ClientMessage(int gameId, Command command, long sentAt)
    {
        GameId = gameId;
        Command = command;
        SentAt = sentAt;
    }
}