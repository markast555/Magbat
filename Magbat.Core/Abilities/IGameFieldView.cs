using System.Collections.Generic;

namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Read-only представление игрового поля, доступное способностям.
    /// </summary>
    public interface IGameFieldView
    {
        /// <summary>
        /// Получить фигуру по координатам.
        /// </summary>
        Piece? GetAt(int x, int y);

        /// <summary>
        /// Получить фигуру по ID.
        /// </summary>
        Piece? FindById(int id);

        /// <summary>
        /// Получить все фигуры на поле.
        /// </summary>
        IEnumerable<Piece> GetAllPieces();

        /// <summary>
        /// Ширина поля.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Высота поля.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Расстояние между клетками по правилам Magbat (Чебышёво).
        /// </summary>
        int GetDistance(int x1, int y1, int x2, int y2);

        /// <summary>
        /// Получить все фигуры в радиусе от клетки.
        /// </summary>
        IEnumerable<Piece> GetPiecesInRadius(int x, int y, int radius);

        /// <summary>
        /// Получить фигуры в соседних клетках (расстояние 1 по метрике Чебышёва).
        /// </summary>
        IEnumerable<Piece> GetNeighbors(int x, int y);
    }
}