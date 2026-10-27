using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Итоги партий, сыгранных через сервер
/// </summary>
public class SimulationResult
{
    /// <summary>Итоговое состояние каждой партии на сервере. Ключ: номер партии (seed)</summary>
    public Dictionary<int, GameState> ServerStates { get; } = new Dictionary<int, GameState>();

    /// <summary>Итоговая копия состояния у каждого клиента. Ключ: номер партии (seed) и номер игрока</summary>
    public Dictionary<(int Seed, int Player), GameState> ClientStates { get; } =
        new Dictionary<(int Seed, int Player), GameState>();

    /// <summary>Все измеренные задержки отклика, мс</summary>
    public List<double> LatenciesMs { get; } = new List<double>();
}