using System;
using System.Collections.Generic;
using Magbat.Core.Abilities;

namespace Magbat.Core
{
    /// <summary>
    /// Игровое поле
    /// </summary>
    public class GameField : IGameFieldView
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
        /// Расстояние между клетками по правилам Magbat (Чебышёво).
        /// </summary>
        public int GetDistance(int x1, int y1, int x2, int y2)
            => Distance(x1, y1, x2, y2);

        /// <summary>
        /// Проверяет, находится ли клетка (x, y) в радиусе radius от клетки
        /// (centerX, centerY). Радиус включительно: расстояние 0..radius,
        /// центральная клетка тоже подходит. Метрика — Чебышёва.
        /// </summary>
        /// <param name="centerX">Координата X центра</param>
        /// <param name="centerY">Координата Y центра</param>
        /// <param name="x">Координата X проверяемой клетки</param>
        /// <param name="y">Координата Y проверяемой клетки</param>
        /// <param name="radius">Радиус, включительно</param>
        /// <returns>true, если клетка в радиусе</returns>
        public bool IsInRadius(int centerX, int centerY, int x, int y, int radius)
            => Distance(centerX, centerY, x, y) <= radius;

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

        /// <summary>
        /// Возвращает все фигуры на поле в порядке возрастания Id
        /// </summary>
        /// <returns>Коллекция фигур</returns>
        public IEnumerable<Piece> GetAllPieces()
        {
            var pieces = new List<Piece>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var piece = _cells[x, y];
                if (piece != null) pieces.Add(piece);
            }

            pieces.Sort((a, b) => a.Id.CompareTo(b.Id));
            return pieces;
        }

        /// <summary>
        /// Возвращает все фигуры в радиусе от клетки. Радиус включительно:
        /// в результат попадают фигуры на расстоянии от 0 до radius включительно
        /// (центральная клетка тоже), метрика — Чебышёва.
        /// Фигуры возвращаются в порядке возрастания Id.
        /// </summary>
        /// <param name="x">Координата X центра</param>
        /// <param name="y">Координата Y центра</param>
        /// <param name="radius">Радиус, включительно</param>
        /// <returns>Коллекция фигур в радиусе</returns>
        public IEnumerable<Piece> GetPiecesInRadius(int x, int y, int radius)
        {
            var result = new List<Piece>();
            foreach (var piece in GetAllPieces())
            {
                if (IsInRadius(x, y, piece.X, piece.Y, radius))
                    result.Add(piece);
            }
            return result;
        }

        /// <summary>
        /// Возвращает фигуры в соседних клетках: расстояние до клетки строго 1
        /// по метрике Чебышёва. Сама клетка (x, y) в результат не входит.
        /// </summary>
        /// <param name="x">Координата X клетки</param>
        /// <param name="y">Координата Y клетки</param>
        /// <returns>Фигуры в соседних клетках</returns>
        public IEnumerable<Piece> GetNeighbors(int x, int y)
        {
            var result = new List<Piece>();
            foreach (var piece in GetAllPieces())
            {
                if (Distance(x, y, piece.X, piece.Y) == 1)
                    result.Add(piece);
            }
            return result;
        }

        // ---------- IGameFieldView: только чтение ----------
        // Явная реализация интерфейса: способности получают IPieceView,
        // а публичные методы поля продолжают отдавать саму Piece.
        // Публичный API ядра при этом не меняется.

        IPieceView? IGameFieldView.GetAt(int x, int y) => GetAt(x, y);

        IPieceView? IGameFieldView.FindById(int id) => FindById(id);

        IEnumerable<IPieceView> IGameFieldView.GetAllPieces() => GetAllPieces();

        IEnumerable<IPieceView> IGameFieldView.GetPiecesInRadius(int x, int y, int radius)
            => GetPiecesInRadius(x, y, radius);

        IEnumerable<IPieceView> IGameFieldView.GetNeighbors(int x, int y) => GetNeighbors(x, y);
    }
}