namespace Magbat.Core
{
    /// <summary>
    /// Причина отказа в применении команды
    /// </summary>
    public enum ApplyError
    {
        None,
        GameOver,
        NotYourTurn,
        UnknownCommand,
        PieceNotInHand,
        OutOfBounds,
        CellOccupied,
        NoSuchPiece,
        TooFar,
        InvalidTarget,
        TargetOutOfRange
    }
}