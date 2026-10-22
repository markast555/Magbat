namespace Magbat.Sim;

/// <summary>
/// Итог партии, сыгранной напрямую
/// </summary>
public class GameResult
{
    /// <summary>
    /// Номер победителя. null, если партия не закончилась до лимита ходов
    /// </summary>
    public int? Winner { get; set; }
    /// <summary>Число сделанных ходов</summary>
    public int Turns { get; set; }
    /// <summary>Партия остановлена по лимиту ходов, победителя нет</summary>
    public bool HitTurnLimit { get; set; }
}