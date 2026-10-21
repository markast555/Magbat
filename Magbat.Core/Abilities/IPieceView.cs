namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Read-only представление фигуры, доступное способностям.
    /// У интерфейса нет ни одного сеттера: способность может прочитать
    /// состояние фигуры, но не может его изменить.
    /// Реальный класс <see cref="Piece"/> реализует этот интерфейс,
    /// поэтому ядро может отдавать способностям тот же объект фигуры,
    /// но только под read-only-типизацией.
    /// </summary>
    public interface IPieceView
    {
        /// <summary>
        /// Идентификатор экземпляра фигуры, уникален в партии.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Владелец фигуры: 0 — первый игрок, 1 — второй.
        /// </summary>
        int Owner { get; }

        /// <summary>
        /// Текущее здоровье. Значение 0 или меньше — фигура погибла.
        /// </summary>
        int Hp { get; }

        /// <summary>
        /// Координата X на поле.
        /// </summary>
        int X { get; }

        /// <summary>
        /// Координата Y на поле.
        /// </summary>
        int Y { get; }

        /// <summary>
        /// Роль фигуры: воин, маг, пушка, опора, тотем, король.
        /// </summary>
        PieceType Type { get; }

        /// <summary>
        /// Имя типа фигуры из её определения.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Дальность хода из определения фигуры.
        /// </summary>
        int MoveRange { get; }

        /// <summary>
        /// Дальность атаки из определения фигуры.
        /// </summary>
        int AttackRange { get; }

        /// <summary>
        /// Базовый урон атаки из определения фигуры, до применения способностей.
        /// </summary>
        int DefDamage { get; }

        /// <summary>
        /// Базовое здоровье из определения фигуры.
        /// </summary>
        int DefHp { get; }
    }
}
