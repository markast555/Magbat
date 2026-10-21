using System.Collections.Generic;

namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Read-only представление игрового поля, доступное способностям.
    /// Все фигуры отдаются как <see cref="IPieceView"/> — только чтение:
    /// у способности нет доступа к мутаторам фигуры и к рукам игроков.
    /// </summary>
    public interface IGameFieldView
    {
        /// <summary>
        /// Получить фигуру по координатам.
        /// </summary>
        IPieceView? GetAt(int x, int y);

        /// <summary>
        /// Получить фигуру по ID.
        /// </summary>
        IPieceView? FindById(int id);

        /// <summary>
        /// Получить все фигуры на поле в порядке возрастания Id.
        /// </summary>
        IEnumerable<IPieceView> GetAllPieces();

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
        /// Проверяет, находится ли клетка (x, y) в радиусе radius от клетки
        /// (centerX, centerY). Радиус включительно: подходят расстояния
        /// от 0 до radius включительно, центральная клетка тоже подходит.
        /// </summary>
        bool IsInRadius(int centerX, int centerY, int x, int y, int radius);

        /// <summary>
        /// Получить все фигуры в радиусе от клетки. Радиус включительно:
        /// в результат попадают фигуры на расстоянии от 0 до radius
        /// (включая центральную клетку), метрика — Чебышёва.
        /// Фигуры возвращаются в порядке возрастания Id.
        /// </summary>
        IEnumerable<IPieceView> GetPiecesInRadius(int x, int y, int radius);

        /// <summary>
        /// Получить фигуры в соседних клетках (расстояние 1 по метрике Чебышёва).
        /// </summary>
        IEnumerable<IPieceView> GetNeighbors(int x, int y);
    }
}
