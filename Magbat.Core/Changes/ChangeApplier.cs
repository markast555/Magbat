using System.Collections.Generic;

namespace Magbat.Core
{
    /// <summary>
    /// Применяет список изменений к копии состояния игры (используется клиентом)
    /// </summary>
    public static class ChangeApplier
    {
        public static void Apply(GameState state, IEnumerable<Change> changes)
        {
            foreach (var change in changes)
            {
                switch (change)
                {
                    case PieceAdded added:
                    {
                        var definition = PieceCatalog.Get(added.PieceName);
                        var piece = new Piece(added.PieceId, added.Owner, definition, added.X, added.Y);
                        state.Field.Put(piece);
                        state.Hands[added.Owner].Remove(added.PieceName);
                        break;
                    }
                    case PieceMoved moved:
                    {
                        var piece = state.Field.FindById(moved.PieceId);
                        if (piece == null) break;
                        state.Field.Remove(piece.X, piece.Y);
                        piece.X = moved.ToX;
                        piece.Y = moved.ToY;
                        state.Field.Put(piece);
                        break;
                    }
                    case PieceDamaged damaged:
                    {
                        var piece = state.Field.FindById(damaged.PieceId);
                        if (piece != null) piece.Hp = damaged.Hp;
                        break;
                    }
                    case PieceRemoved removed:
                    {
                        var piece = state.Field.FindById(removed.PieceId);
                        if (piece != null) state.Field.Remove(piece.X, piece.Y);
                        break;
                    }
                    case TurnChanged turn:
                        state.CurrentPlayer = turn.Player;
                        break;
                    case GameEnded ended:
                        state.Winner = ended.Winner;
                        break;
                    case StateSnapshot snapshot:
                    {
                        state.Field.Clear();
                        foreach (var p in snapshot.Pieces)
                        {
                            var piece = new Piece(p.Id, p.Owner, PieceCatalog.Get(p.PieceName), p.X, p.Y);
                            piece.Hp = p.Hp;
                            state.Field.Put(piece);
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            state.Hands[i].Clear();
                            state.Hands[i].AddRange(snapshot.Hands[i]);
                        }

                        state.CurrentPlayer = snapshot.CurrentPlayer;
                        state.Winner = snapshot.Winner;
                        break;
                    }
                }
            }
        }
    }
}