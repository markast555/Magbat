namespace Magbat.Sim;

/// <summary>
/// Простая статистика по измерениям
/// </summary>
public static class Stats
{
    /// <summary>
    /// Считает перцентиль по методу ближайшего ранга
    /// </summary>
    /// <param name="sorted">Значения по возрастанию</param>
    /// <param name="p">Доля от 0 до 1, например 0,95</param>
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        int rank = (int)Math.Ceiling(p * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }
}