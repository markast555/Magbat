namespace Magbat.Sim;

/// <summary>
/// Определяет папку для результатов экспериментов
/// </summary>
public static class ResultsFolder
{
    /// <summary>Папка Results в корне решения</summary>
    public static string Location { get; } = Find();

    private static string Find()
    {
        // Ищем вверх от места запуска: результаты принадлежат всей работе, а не проекту Sim
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && dir.GetFiles("*.sln").Length == 0 && dir.GetFiles("*.slnx").Length == 0)
            dir = dir.Parent;

        string root = dir?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, "Results");
    }
}