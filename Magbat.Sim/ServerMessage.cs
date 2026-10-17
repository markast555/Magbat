using Magbat.Core;

namespace Magbat.Sim;

public class ServerMessage
{
    public int GameId { get; }
    public Command Command { get; }
    public ApplyError Error { get; }
    public int? Winner { get; }
    public long CommandSentAt { get; } // время отправки исходной команды: от него считается задержка

    public ServerMessage(int gameId, Command command, ApplyError error, int? winner, long commandSentAt)
    {
        GameId = gameId;
        Command = command;
        Error = error;
        Winner = winner;
        CommandSentAt = commandSentAt;
    }
}