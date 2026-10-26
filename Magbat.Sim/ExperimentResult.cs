namespace Magbat.Sim;

/// <summary>
/// Итог одного прогона в виде строки таблицы
/// </summary>
public sealed class ExperimentResult
{
    public IReadOnlyList<(string Name, object? Value)> Columns { get; }
    public string Summary { get; }

    public ExperimentResult(ExperimentConfig c, RunMetrics m, TickServer? tick)
    {
        var latencies = m.LatenciesMs.OrderBy(x => x).ToList();
        double elapsedMs = m.ElapsedSec * 1000;
        double? mean = latencies.Count > 0 ? latencies.Average() : null;
        double? p95 = latencies.Count > 0 ? Stats.Percentile(latencies, 0.95) : null;
        double busyPct = m.ServerBusyMs / elapsedMs * 100;
        double? cpuPct = m.ServerCpuMs / elapsedMs * 100;
        double bytesPerSec = (m.UpBytes + m.DownBytes) / m.ElapsedSec;
        double? busyPerCmd = m.UpMessages > 0 ? m.ServerBusyMs / m.UpMessages : null;
        double? bytesPerCmd = m.UpMessages > 0 ? (double)(m.UpBytes + m.DownBytes) / m.UpMessages : null;
        
        Columns = new List<(string, object?)>
        {
            ("started_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            ("server", c.ServerLabel),
            ("tick_hz", c.TickHz),
            ("mode", c.Server == ServerKind.Tick ? c.Mode.ToString() : ""),
            ("games", c.Games),
            ("think_ms", c.MeanThinkMs),
            ("delay_ms", c.DelayMs),
            ("repeat", c.Repeat),
            ("elapsed_s", m.ElapsedSec),
            ("games_finished", m.GamesFinished),

            ("commands", m.UpMessages),
            ("cmds_per_s", m.UpMessages / m.ElapsedSec),
            ("lat_count", latencies.Count),
            ("lat_mean_ms", mean),
            ("lat_p95_ms", p95),
            ("lat_max_ms", latencies.Count > 0 ? latencies[^1] : null),
            ("server_busy_pct", busyPct),
            ("server_cpu_pct", cpuPct),
            ("process_cpu_pct", m.ProcessCpuMs / elapsedMs * 100),
            ("up_msgs", m.UpMessages),
            ("down_msgs", m.DownMessages),
            ("up_bytes", m.UpBytes),
            ("down_bytes", m.DownBytes),
            ("traffic_bytes_per_s", bytesPerSec),
            ("busy_ms_per_cmd", busyPerCmd),
            ("bytes_per_cmd", bytesPerCmd),
            ("server_alloc_mb", m.ServerAllocatedBytes / 1048576.0),
            ("gc0", m.Gc0),
            ("gc1", m.Gc1),
            ("gc2", m.Gc2),
            ("ticks", tick?.Ticks),
            ("idle_ticks", tick?.IdleTicks),
            ("tick_mean_ms", tick?.MeanIntervalMs),
            ("tick_min_ms", tick?.MinIntervalMs),
            ("tick_max_ms", tick?.MaxIntervalMs)
        };

        Summary = $"{c.ServerLabel} games={c.Games} think={c.MeanThinkMs} ms: " +
                  $"latency {mean?.ToString("F1") ?? "-"} / p95 {p95?.ToString("F1") ?? "-"} ms, " +
                  $"server busy {busyPct:F2}%, cpu {cpuPct?.ToString("F2") ?? "-"}%, " +
                  $"traffic {bytesPerSec / 1024:F1} KB/s";
    }
}