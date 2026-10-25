using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Определяет функциональность игрового сервера
/// </summary>
public interface IGameServer : IDisposable
{
    /// <summary>
    /// Запускает обработку команд
    /// </summary>
    /// <remarks>Вызывается один раз, после AddGame для всех партий</remarks>
    void Start();
    
    /// <summary>
    /// Добавляет партию на сервер
    /// </summary>
    /// <param name="gameId">Номер партии</param>
    /// <param name="initialState">Начальное состояние. Дальше им владеет сервер</param>
    void AddGame(int gameId, GameState initialState);
    
    /// <summary>
    /// Принимает команду клиента на обработку
    /// </summary>
    /// <remarks>
    /// Не ожидает обработки: результат приходит через Updated.
    /// Вызывается из потока канала «вверх»
    /// </remarks>
    /// <param name="message">Сообщение клиента</param>
    void Submit(ClientMessage message);
    
    /// <summary>
    /// Возникает после обработки команды и несёт её результат
    /// </summary>
    /// <remarks>Подписчики вызываются в потоке сервера, поэтому должны работать быстро</remarks>
    event Action<ServerMessage>? Updated;
    
    /// <summary>
    /// Статистика потока сервера
    /// </summary>
    ServerStats Stats { get; }
}