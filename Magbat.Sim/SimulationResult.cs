using Magbat.Core;

namespace Magbat.Sim;

public class SimulationResult
{
    /// <summary>Итоговое состояние каждой партии на сервере (по seed)</summary>
    public Dictionary<int, GameState> ServerStates { get; } = new Dictionary<int, GameState>();

    /// <summary>Итоговая копия состояния у каждого клиента</summary>
    public Dictionary<(int Seed, int Player), GameState> ClientStates { get; } =
        new Dictionary<(int Seed, int Player), GameState>();

    /// <summary>Все измеренные задержки отклика, мс</summary>
    public List<double> LatenciesMs { get; } = new List<double>();
}