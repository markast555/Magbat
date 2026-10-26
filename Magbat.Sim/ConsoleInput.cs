namespace Magbat.Sim;

/// <summary>
/// Запрашивает параметры у пользователя в консоли
/// </summary>
public static class ConsoleInput
{
    /// <summary>
    /// Запрашивает целое число. Enter возвращает значение по умолчанию
    /// </summary>
    public static int AskInt(string prompt, int defaultValue, int min)
    {
        while (true)
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return defaultValue;
            if (int.TryParse(line.Trim(), out int value) && value >= min) return value;
            Console.WriteLine($"Введите целое число не меньше {min} или нажмите Enter.");
        }
    }

    /// <summary>
    /// Запрашивает список целых чисел через запятую. Enter возвращает значения по умолчанию
    /// </summary>
    public static List<int> AskIntList(string prompt, IReadOnlyList<int> defaults, int min)
    {
        while (true)
        {
            Console.Write($"{prompt} через запятую [{string.Join(",", defaults)}]: ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return defaults.ToList();


            var numbers = Parse(line);
            if (numbers != null && numbers.All(n => n >= min)) return numbers;
            Console.WriteLine($"Введите целые числа не меньше {min} через запятую или нажмите Enter.");
        }
    }

    /// <summary>
    /// Запрашивает номера пунктов списка. Enter или 0 выбирают все пункты
    /// </summary>
    /// <returns>Индексы выбранных пунктов, начиная с нуля</returns>
    public static List<int> AskIndices(string prompt, int count)
    {
        var all = Enumerable.Range(0, count).ToList();
        while (true)
        {
            Console.Write($"{prompt} (номера через запятую, Enter = все): ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return all;

            var numbers = Parse(line);
            if (numbers != null && numbers.All(n => n >= 0 && n <= count))
                return numbers.Contains(0) ? all : numbers.Distinct().Select(n => n - 1).ToList();
            Console.WriteLine($"Введите номера от 1 до {count} или 0 для всех.");
        }
    }

    /// <summary>
    /// Запрашивает ответ да или нет. Enter возвращает значение по умолчанию
    /// </summary>
    public static bool AskYesNo(string prompt, bool defaultValue)
    {

        while (true)
        {
            Console.Write($"{prompt} [{(defaultValue ? "Y/n" : "y/N")}]: ");
            string? line = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(line)) return defaultValue;
            if (line is "y" or "yes" or "д" or "да") return true;
            if (line is "n" or "no" or "н" or "нет") return false;
            Console.WriteLine("Введите y или n, либо нажмите Enter.");
        }
    }

    private static List<int>? Parse(string line)
    {
        var result = new List<int>();
        foreach (var part in line.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, out int value)) return null;
            result.Add(value);
        }
        return result.Count > 0 ? result : null;
    }
}