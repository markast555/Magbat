namespace Magbat.Sim;

/// <summary>
/// Параметры одного прогона
/// </summary>
public sealed class ExperimentConfig
{
    public ServerKind Server { get; set; }
    public int TickHz { get; set; }
    public BroadcastMode Mode { get; set; }
    public int Games { get; set; }
    public int MeanThinkMs { get; set; }
    public int DelayMs { get; set; }
    public int DurationSec { get; set; }
    public int Repeat { get; set; }

    public string ServerLabel => Server == ServerKind.Event
        ? "event"
        : $"tick{TickHz}_{(Mode == BroadcastMode.Delta ? "delta" : "snapshot")}";
}