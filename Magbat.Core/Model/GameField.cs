using System;

namespace Magbat.Core
{
    /// <summary>
    /// Игровое поле
    /// </summary>
    public class GameField
    {
        private readonly Piece?[,] _cells;
        public int Width { get; }
        public int Height { get; }

        public GameField(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Piece[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public Piece? GetAt(int x, int y) => _cells[x, y];
        public bool IsEmpty(int x, int y) => _cells[x, y] == null;

        public void Put(Piece piece) => _cells[piece.X, piece.Y] = piece;
        public void Remove(int x, int y) => _cells[x, y] = null;

        public static int Distance(int x1, int y1, int x2, int y2)
            => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
        
        /// <summary>
        /// Ищет обозначенную фигуру на поле
        /// </summary>
        /// <param name="id">ID искомой фигуры</param>
        /// <returns>Найденная фигура или null, если такой нет</returns>
        public Piece? FindById(int id)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var piece = _cells[x, y];
                if (piece != null && piece.Id == id) return piece;
            }
            return null;
        }
    }
}