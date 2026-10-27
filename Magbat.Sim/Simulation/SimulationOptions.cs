namespace Magbat.Sim;

/// <summary>
/// Параметры симуляции
/// </summary>
public sealed class SimulationOptions
{
    /// <summary>Задержка каждого канала, мс</summary>
    public int DelayMs { get; set; }

    /// <summary>Средняя пауза бота перед ходом, мс. 0 означает ход без паузы</summary>
    public int MeanThinkMs { get; set; }

    /// <summary>Считать размер сообщений в байтах</summary>
    public bool CountTraffic { get; set; }
}