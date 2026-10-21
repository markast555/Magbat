# Журнал изменений существующего кода

В журнал попадают **только изменения в уже существовавших файлах** (то, что было
в репозитории до эксперимента). Новые файлы здесь не перечисляются.

Категории:

- **А** — новый механизм (расширение ядра, без которого способность не может
  существовать в принципе);
- **Б** — исправление ошибки в ранее написанном коде;
- **В** — удобство / организация (на поведение игры не влияет).

Каждое изменение фиксируется сразу при внесении. Формат строки:
**№ | Тег | Файл | Что изменено | Почему | Категория**.

---

## Этап 0. Подготовка (`exp/00-setup`)

Основание: `27abbded...` (тег `exp-baseline`).

| № | Тег | Файл | Что изменено | Почему | Категория |
|---|---|---|---|---|---|
| 1 | `exp/00-setup` | `Magbat.sln` | В решение добавлен проект `Magbat.Abilities` | Организация: библиотека способностей должна быть видна в основном решении; на поведение кода не влияет | **В** |

## Этап 1. Аудит и фиксация правил (`exp/01-audit`)

Существующий код на этом этапе **не менялся** — этап был чисто аналитическим:
ответы на шесть вопросов и фиксация правил вынесены в `docs/00-audit.md`
и `docs/RULES.md`. Изменения в существующих файлах отсутствуют.

## Этап 2. Знаменосец (`exp/02-standard-bearer`)

Основание: тег `exp/01-audit`.

### Изменённые файлы ядра `Magbat.Core` — 5 файлов

| № | Тег | Файл | Что изменено | Почему | Категория |
|---|---|---|---|---|---|
| 2 | `exp/02-standard-bearer` | `Magbat.Core/Model/PieceDefinition.cs` | Добавлено свойство `AbilityFactory` типа `Func<Piece, IAbility>?` и необязательный параметр конструктора `abilityFactory` | Точка расширения: внешняя библиотека задаёт способность описанию фигуры, не трогая ядро | **А** |
| 3 | `exp/02-standard-bearer` | `Magbat.Core/Model/Piece.cs` | Добавлено свойство `Abilities` (`IReadOnlyList<IAbility>`); в конструкторе вызывается фабрика; добавлен приватный метод `CreateAbilities` | У каждой фигуры — собственные экземпляры способностей (нужно для состоятельных способностей, например для Тотета) | **А** |
| 4 | `exp/02-standard-bearer` | `Magbat.Core/Model/GameField.cs` | Класс реализует `IGameFieldView`; добавлены `GetAllPieces()` (сортировка по `Id`), `GetPiecesInRadius(x, y, radius)`, `GetNeighbors(x, y)`, экземплярный `GetDistance(...)` | Дать способностям read-only доступ к полю; статический `Distance` сохранён без изменений, чтобы не ломать публичный API | **А** |
| 5 | `exp/02-standard-bearer` | `Magbat.Core/Model/PieceCatalog.cs` | `Register` изменён с `private` на `public` | Внешняя библиотека способностей должна регистрировать своё описание фигуры | **А** |
| 6 | `exp/02-standard-bearer` | `Magbat.Core/Rules/GameRules.cs` | В `ApplyAttack` строка `target.Hp -= attacker.Definition.DefDamage` заменена на `target.Hp -= AttackDamageCalculator.Calculate(state, attacker, target)` (одна строка); три вызова `GameField.Distance(...)` заменены на `field.GetDistance(...)` / `state.Field.GetDistance(...)` | Подключить модификаторы урона к настоящей атаке — это и есть выбранный на этапе 1 вариант А; `GetDistance` — единый способ считать расстояние через интерфейс поля | **А** |

### Изменённые файлы вне ядра — 1 файл

| № | Тег | Файл | Что изменено | Почему | Категория |
|---|---|---|---|---|---|
| 7 | `exp/00-setup` | `Magbat.sln` | В решение добавлен проект `Magbat.Abilities` (см. этап 0) | Организация | **В** |

---

## Итог по журналу

- Изменено существующих файлов: **6** (5 в `Magbat.Core` + 1 файл решения).
- Из них меняющих **поведение** игры: **1** — `Magbat.Core/Rules/GameRules.cs`
  (одна строка в `ApplyAttack`).
- Остальные правки — добавление точек расширения, не меняющие поведение.
- Изменений в системе `Change` и в сетевом протоколе: **0**.
- **Категория Б (ошибки в ранее написанном коде): ошибок не выявлено.**
  Все 34 существующих теста `Magbat.Tests` проходят без единой правки их текста.

### Что не попадает в журнал (новые файлы эксперимента)

`Magbat.Core/Abilities/`: `IAbility.cs`, `IDamageModifier.cs`, `DamageContext.cs`,
`IGameFieldView.cs`, `AttackDamageCalculator.cs`.
`Magbat.Abilities/`: `StandardBearerAbility.cs`, `AbilityCatalog.cs`.
`Magbat.Abilities.Tests/`: `StandardBearerTests.cs`, `FieldViewTests.cs`,
`AttackDamageCalculatorTests.cs`.
`Magbat.Abilities.Demo/`: `Program.cs`.
`Magbat.Experiment.sln`, `docs/00-audit.md`, `docs/RULES.md`, `docs/JOURNAL.md`, `docs/RESULTS.md`.

---

## Проверка регрессии (после этапа 2)

```
dotnet build Magbat.Experiment.sln                  → Предупреждений: 0, Ошибок: 0
dotnet test  Magbat.Abilities.Tests\...csproj      → 15 пройдено, 0 не пройдено, всего 15
dotnet test  Magbat.Tests\Magbat.Tests.csproj       → 34 пройдено, 0 не пройдено, всего 34
```
