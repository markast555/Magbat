using System.Linq;
using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities
{
    /// <summary>
    /// Каталог способностей. Регистрирует определения фигур со способностями
    /// в общем PieceCatalog ядра.
    /// </summary>
    public static class AbilityCatalog
    {
        public const string StandardBearerName = "Standard_Bearer";

        /// <summary>
        /// Замок регистрации: каталог ядра общий для процесса, а регистрация
        /// может быть вызвана из разных потоков (тесты, сервер, клиент).
        /// </summary>
        private static readonly object _registrationLock = new object();

        /// <summary>
        /// Регистрирует фигуру «Знаменосец» с одноимённой способностью.
        /// Повторный вызов ничего не делает: каталог ядра общий для процесса.
        /// </summary>
        public static void RegisterStandardBearer()
        {
            lock (_registrationLock)
            {
                if (PieceCatalog.GetAll().Any(d => d.Name == StandardBearerName))
                    return;
    
                // Параметры зафиксированы в RULES.md: в исходной игре они зависят от клана,
                // в эксперименте взят один вариант. Тип — Support, как в описании фигуры.
                // Фабрика возвращает список способностей: сейчас у фигуры одна,
                // но одна фигура может нести и несколько независимых способностей.
                PieceCatalog.Register(new PieceDefinition(
                    name: StandardBearerName,
                    type: PieceType.Support,
                    defHp: 10,
                    defDamage: 2,
                    moveRange: 1,
                    attackRange: 1,
                    abilityFactory: piece => new IAbility[] { new StandardBearerAbility(piece) }
                ));
            }
        }
    }
}
