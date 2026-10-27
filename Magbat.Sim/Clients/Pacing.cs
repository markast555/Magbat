namespace Magbat.Sim;

/// <summary>
/// Запускает действие клиента после случайной паузы
/// </summary>
public sealed class Pacing
{
    private readonly int _meanMs;
    private readonly Random _random;

    public Pacing(int meanMs, int seed)
    {
        _meanMs = meanMs;
        _random = new Random(seed);
    }

    /// <summary>
    /// Запускает действие после случайной паузы
    /// </summary>
    /// <param name="action">Действие, которое нужно отложить</param>
    public void Schedule(Action action)
    {
        // Свой генератор, чтобы паузы не влияли на выбор ходов ботом
        int ms = (int)(_meanMs * (0.5 + _random.NextDouble()));
        Task.Delay(ms).ContinueWith(_ => action());
    }
}