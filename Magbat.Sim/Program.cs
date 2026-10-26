using Magbat.Sim;

string Label((ServerKind Kind, int Hz, BroadcastMode Mode) s) =>
    new ExperimentConfig { Server = s.Kind, TickHz = s.Hz, Mode = s.Mode }.ServerLabel;

Console.WriteLine("Параметры серии. Enter оставляет значение в скобках.");

int repeats = ConsoleInput.AskInt("Число повторов", 3, 1);
int durationSec = ConsoleInput.AskInt("Длительность одного прогона, с", 15, 1);
var gamesList = ConsoleInput.AskIntList("Число одновременных партий", new[] { 1, 10, 100 }, 1);
var thinkList = ConsoleInput.AskIntList("Средняя пауза бота, мс (0 = без пауз)", new[] { 500, 3000 }, 0);
var delayList = ConsoleInput.AskIntList("Задержка канала, мс", new[] { 0 }, 0);

var servers = ExperimentRunner.Servers;
Console.WriteLine("Модели сервера:");
for (int i = 0; i < servers.Length; i++)
    Console.WriteLine($"  {i + 1}. {Label(servers[i])}");
var picked = ConsoleInput.AskIndices("Какие запускать", servers.Length);

// Повторы снаружи: если серию прервать, у всех конфигураций будет поровну прогонов
var configs = new List<ExperimentConfig>();
for (int repeat = 0; repeat < repeats; repeat++)
foreach (int games in gamesList)
foreach (int think in thinkList)
foreach (int delay in delayList)
foreach (int index in picked)
{
    var (kind, hz, mode) = servers[index];
    configs.Add(new ExperimentConfig
    {
        Server = kind, TickHz = hz, Mode = mode,
        Games = games, MeanThinkMs = think, DelayMs = delay,

        DurationSec = durationSec, Repeat = repeat
    });
}

double minutes = configs.Count * durationSec / 60.0;
Console.WriteLine($"Прогонов: {configs.Count}, примерно {minutes:F0} мин без прогрева.");

if (!ConsoleInput.AskYesNo("Начать серию", true))
    return;

if (ConsoleInput.AskYesNo("Выполнить прогрев перед серией", true))
{
    Console.WriteLine("Warm-up...");
    ExperimentRunner.Warmup();
}

string path = Path.Combine(ResultsFolder.Location, $"results_{DateTime.Now:yyyyMMdd_HHmm}.csv");
Console.WriteLine($"Результаты: {path}");

int number = 0;
foreach (var config in configs)
{
    number++;
    var result = ExperimentRunner.Execute(config);
    CsvWriter.Append(path, result);
    Console.WriteLine($"[{number}/{configs.Count}] {result.Summary}");
}