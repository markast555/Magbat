using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Сообщение, отправляемое сервером клиентам
/// </summary>
public class ServerMessage
{
    /// <summary>Номер партии</summary>
    public int GameId { get; }
    /// <summary>Игрок, приславший исходную команду</summary>
    public int Player { get; } 
    /// <summary>
    /// Время отправки исходной команды, скопированное из ClientMessage. Нужно клиенту для расчёта отклика
    /// </summary>
    public long CommandSentAt { get; }
    /// <summary>Причина отказа, None если команда принята</summary>
    public ApplyError Error { get; }
    /// <summary>Изменения состояния игры, к которым привела команда. При отказе список пуст</summary>
    public IReadOnlyList<Change> Changes { get; }

    public ServerMessage(int gameId, int player, long commandSentAt, ApplyError error, IReadOnlyList<Change> changes)
    {
        GameId = gameId;
        Player = player;
        CommandSentAt = commandSentAt;
        Error = error;
        Changes = changes;
    }
}