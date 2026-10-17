using Magbat.Core;

namespace Magbat.Sim;

public interface IGameServer : IDisposable
{
    void Start();
    void AddGame(int gameId, GameState initialState);
    void Submit(ClientMessage message);
    event Action<ServerMessage>? Updated;
}