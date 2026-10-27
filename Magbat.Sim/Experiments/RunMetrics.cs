namespace Magbat.Sim;

/// <summary>
/// Измерения за окно замера
/// </summary>
public sealed class RunMetrics
{
    public double ElapsedSec { get; set; }
    public int GamesFinished { get; set; }
    public long UpMessages { get; set; }
    public long UpBytes { get; set; }
    public long DownMessages { get; set; }
    public long DownBytes { get; set; }
    public double ServerBusyMs { get; set; }
    public double? ServerCpuMs { get; set; }
    public double ProcessCpuMs { get; set; }
    public long ServerAllocatedBytes { get; set; }
    public int Gc0 { get; set; }
    public int Gc1 { get; set; }
    public int Gc2 { get; set; }
    public List<double> LatenciesMs { get; } = new List<double>();
}