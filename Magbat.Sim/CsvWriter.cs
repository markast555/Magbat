using System.Globalization;
using System.Text;

namespace Magbat.Sim;

/// <summary>
/// Дописывает результаты прогонов в CSV-файл
/// </summary>
public static class CsvWriter
{
    // Русская культура и разделитель «;», чтобы Excel открывал файл двойным щелчком
    private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

    public static void Append(string path, ExperimentResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        
        bool isNew = !File.Exists(path);
        using var writer = new StreamWriter(path, true, new UTF8Encoding(true));

        if (isNew)
            writer.WriteLine(string.Join(";", result.Columns.Select(c => c.Name)));
        writer.WriteLine(string.Join(";", result.Columns.Select(c => Format(c.Value))));
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        double d => d.ToString("0.###", Culture),
        IFormattable f => f.ToString(null, Culture),
        _ => value.ToString() ?? ""
    };
}