using System.Collections.Generic;

namespace Magbat.Core
{
    /// <summary>
    /// Полный снимок состояния партии
    /// </summary>
    public class StateSnapshot : Change
    {
        public IReadOnlyList<PieceSnapshot> Pieces { get; }
        public IReadOnlyList<string>[] Hands { get; }
        public int CurrentPlayer { get; }
        public int? Winner { get; }

        public StateSnapshot(IReadOnlyList<PieceSnapshot> pieces, IReadOnlyList<string>[] hands,
            int currentPlayer, int? winner)
        {
            Pieces = pieces;
            Hands = hands;
            CurrentPlayer = currentPlayer;
            Winner = winner;
        }

        /// <summary>
        /// Создаёт снимок текущего состояния партии
        /// </summary>
        public static StateSnapshot From(GameState state)
        {
            var pieces = new List<PieceSnapshot>();
            for (int x = 0; x < state.Field.Width; x++)
            for (int y = 0; y < state.Field.Height; y++)
            {
                var piece = state.Field.GetAt(x, y);
                if (piece != null)
                    pieces.Add(new PieceSnapshot(piece.Id, piece.Owner, piece.Definition.Name, piece.X, piece.Y, piece.Hp));
            }

            var hands = new IReadOnlyList<string>[]
            {
                new List<string>(state.Hands[0]),
                new List<string>(state.Hands[1])
            };
            return new StateSnapshot(pieces, hands, state.CurrentPlayer, state.Winner);
        }
    }
}