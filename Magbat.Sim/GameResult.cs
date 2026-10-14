namespace Magbat.Sim;

public class GameResult
{
    public int? Winner { get; set; }
    public int Turns { get; set; }
    public bool HitTurnLimit { get; set; }
}