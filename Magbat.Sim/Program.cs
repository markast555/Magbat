using Magbat.Sim;

const int Repeats = 3;
const int DurationSec = 15;
string OutputPath = $"results_{DateTime.Now:yyyyMMdd_HHmm}.csv";

Console.WriteLine("Warm-up...");
ExperimentRunner.Warmup();

// Повторы снаружи: если прервать серию, у всех конфигураций будет поровну прогонов
var configs = new List<ExperimentConfig>();
for (int repeat = 0; repeat < Repeats; repeat++)
{
    foreach (int games in new[] { 1, 10, 100 })
    {
        foreach (int think in new[] { 500, 3000 })
        {
            foreach (var (kind, hz, mode) in ExperimentRunner.Servers)
            {
                configs.Add(new ExperimentConfig
                {
                    Server = kind, TickHz = hz, Mode = mode,
                    Games = games, MeanThinkMs = think, DelayMs = 0,
                    DurationSec = DurationSec, Repeat = repeat
                });
            }
        }
    }
}

int number = 0;
foreach (var config in configs)
{
    number++;
    var result = ExperimentRunner.Execute(config);
    CsvWriter.Append(OutputPath, result);
    Console.WriteLine($"[{number}/{configs.Count}] {result.Summary}");
}