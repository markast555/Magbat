using Magbat.Core.Abilities;

namespace Magbat.Abilities
{
    /// <summary>
    /// Способность «Знаменосец».
    /// Усиливает атаки союзных фигур (кроме себя) в радиусе 2 на +1 к урону.
    /// Бонусы нескольких знаменосцев складываются.
    /// Мёртвый знаменосец не даёт бонуса (HP <= 0).
    /// Радиус считается по метрике Чебышёва, граница радиуса включается —
    /// тем же правилом, что и <see cref="IGameFieldView.GetPiecesInRadius"/>.
    /// </summary>
    public class StandardBearerAbility : IAbility, IDamageModifier
    {
        /// <summary>
        /// Радиус ауры знаменосца в клетках, граница включается.
        /// </summary>
        public const int Radius = 2;

        private readonly IPieceView _owner;

        public StandardBearerAbility(IPieceView owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Возвращает добавку (delta) к урону атаки.
        /// 0 — знаменосец не влияет на эту атаку, 1 — даёт бонус +1 к урону.
        /// Несколько знаменосцев складывают свои добавки.
        /// </summary>
        public int ModifyAttackDamage(DamageContext context)
        {
            // Знаменосец не усиливает себя
            if (context.Attacker.Id == _owner.Id)
                return 0;

            // Знаменосец усиливает только союзников
            if (context.Attacker.Owner != _owner.Owner)
                return 0;

            // Знаменосец должен быть жив
            if (_owner.Hp <= 0)
                return 0;

            // Союзник должен быть в радиусе 2 (граница радиуса включается)
            if (!context.Field.IsInRadius(_owner.X, _owner.Y, context.Attacker.X, context.Attacker.Y, Radius))
                return 0;

            return 1;
        }
    }
}
