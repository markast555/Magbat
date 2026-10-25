namespace Magbat.Sim;

/// <summary>
/// Определяет режим рассылки состояния для циклического сервера
/// </summary>
public enum BroadcastMode
{
    /// <summary>Полный снимок каждой активной партии на каждом тике</summary>
    FullSnapshot,

    /// <summary>Только изменения после обработанных команд</summary>
    Delta
}