# Этап 1. Аудит ядра и выбор точки подключения способностей

Дата: 21.10.2026
Репозиторий: `C:/Users/gtrr/RiderProjects/Magbat`
Ветка: `experiment/abilities`
Базовая точка отсчёта: тег `exp-baseline` → `27abbdedfe271c11676e79b9164eae3000dad660`
Тег этапа: `exp/01-audit`

Документ отвечает на шесть вопросов, поставленных перед началом эксперимента,
и фиксирует выбранный вариант подключения способностей. Все ответы даны
**по факту чтения кода**, с указанием файла и метода.

---

## 1. Шесть вопросов к ядру

### Вопрос 1. Как описывается фигура и какие у неё поля?

Фигура описывается в двух классах: **описание (шаблон)** и **экземпляр**.

**`Magbat.Core/Model/PieceDefinition.cs`, класс `PieceDefinition`** — описание типа фигуры:

| Поле | Тип | Смысл |
|---|---|---|
| `Name` | `string` | уникальное имя типа (`"Warrior_One"`, `"Standard_Bearer"`) |
| `Type` | `PieceType` | роль: `Warrior, Support, Mage, Cannon, Totem, King` |
| `DefHp` | `int` | базовое здоровье |
| `DefDamage` | `int` | базовый урон атаки |
| `MoveRange` | `int` | дальность хода |
| `AttackRange` | `int` | дальность атаки |
| `AbilityFactory` | `Func<Piece, IAbility>?` | **добавлено в ходе эксперимента**: фабрика способностей фигуры |

**`Magbat.Core/Model/Piece.cs`, класс `Piece`** — экземпляр фигуры на поле:

| Поле | Тип | Смысл |
|---|---|---|
| `Id` | `int` | идентификатор экземпляра, уникален в партии (1, 2, 3, …) |
| `Owner` | `int` | владелец: `0` или `1` |
| `Definition` | `PieceDefinition` | ссылка на описание |
| `Hp` | `int` | текущее здоровье (изменяемое) |
| `X`, `Y` | `int` | координаты на поле (изменяемые) |
| `Abilities` | `IReadOnlyList<IAbility>` | **добавлено в ходе эксперимента**: список способностей экземпляра |

Создание экземпляра: `Piece` constructor → `CreateAbilities(definition, this)` —
для каждой фигуры создаются **собственные** экземпляры способностей
(это нужно для способностей с внутренним состоянием, например для Тотета).

Реестр описаний: **`Magbat.Core/Model/PieceCatalog.cs`**, метод
`PieceCatalog.Register(PieceDefinition)` (в ходе эксперимента изменён с
`private` на `public`) и `PieceCatalog.Get(string name)`.

### Вопрос 2. Где хранится поле и позиции фигур, как найти фигуру по клетке и по Id?

**`Magbat.Core/Model/GameState.cs`, класс `GameState`** — состояние партии:

| Поле | Тип | Смысл |
|---|---|---|
| `Field` | `GameField` | само поле |
| `Hands` | `List<string>[]` | руки игроков (списки названий фигур) |
| `CurrentPlayer` | `int` | чей ход |
| `Winner` | `int?` | победитель, `null` — партия идёт |
| `NextPieceId` | `int` | счётчик выданных `Id` |
| `ConsecutivePasses` | `int[2]` | счётчик пассов подряд (для определения ничьей) |

**`Magbat.Core/Model/GameField.cs`, класс `GameField`** — поле как сетка
`Piece?[,] _cells` (фигура → её клетка). Позиция фигуры хранится **дважды**:
в клетке (`_cells[x, y]`) и в самом объекте (`Piece.X`, `Piece.Y`).

Способы найти фигуру:

| Что нужно | Метод | Файл |
|---|---|---|
| фигура по клетке | `GameField.GetAt(int x, int y)` | `Model/GameField.cs:24` |
| фигура по `Id` | `GameField.FindById(int id)` | `Model/GameField.cs:44` |
| все фигуры поля (по возрастанию `Id`) | `GameField.GetAllPieces()` | `Model/GameField.cs:59` — **добавлено в ходе эксперимента** |
| фигуры в радиусе от клетки | `GameField.GetPiecesInRadius(int x, int y, int radius)` | `Model/GameField.cs:80` — **добавлено в ходе эксперимента** |
| фигуры в соседних клетках | `GameField.GetNeighbors(int x, int y)` | `Model/GameField.cs:98` — **добавлено в ходе эксперимента** |
| расстояние между клетками | `GameField.Distance(...)` (static) / `GetDistance(...)` | `Model/GameField.cs:30` / `:36` |

**Метрика расстояния — Чебышёва: `max(|dx|, |dy|)`.** Это та же метрика, что
используется в правилах хода и атаки, поэтому способности считают расстояние
по ней, а не по «эвклидовой» (см. `docs/RULES.md`, раздел «Общие правила»).

### Вопрос 3. Где в `GameRules.Apply` происходит атака и считается урон?

**`Magbat.Core/Rules/GameRules.cs`**:

- `GameRules.Apply(GameState state, Command command)` (строка 21) — единая точка
  входа: проверяет `Winner` и очередь хода, затем по типу команды вызывает
  `ApplyPlace` / `ApplyMove` / `ApplyAttack` / `ApplyPass` (строки 31–34).
- `GameRules.ApplyAttack(GameState state, AttackCommand cmd)` (строка 119) —
  **здесь происходит атака**:
  1. атакующий и цель ищутся через `state.Field.FindById(...)` (строки 121–122);
  2. проверяется, что цель — не своя фигура (строка 125);
  3. проверяется дальность: `state.Field.GetDistance(...) > attacker.Definition.AttackRange` (строки 128–130);
  4. **считается урон** — строка 133:
     `target.Hp -= AttackDamageCalculator.Calculate(state, attacker, target);`
     (в исходной версии было `target.Hp -= attacker.Definition.DefDamage;` — прямой
     расчёт из описания, точки расширения не было);
  5. если `target.Hp <= 0` — фигура снимается с поля (строки 134–138),
     иначе добавляется изменение `PieceDamaged` (строка 147).

### Вопрос 4. Где фигура удаляется после смерти?

В трёх местах, все — в `Magbat.Core/Rules/GameRules.cs`:

1. **Основное место — `ApplyAttack`, строки 134–138:**
   ```csharp
   if (target.Hp <= 0)
   {
       state.Field.Remove(target.X, target.Y);   // ← снятие с поля
       result.Changes.Add(new PieceRemoved(target.Id));
       ...
   }
   ```
   `GameField.Remove(int x, int y)` (строка 28) очищает клетку `_cells[x, y] = null`.
2. **Проверка окончания партии** — `GameRules.CountPieces(GameState, int owner)`
   (строка 176) пересчитывает живые фигуры владельца; если фигур не осталось
   и рука пуста — `state.Winner = cmd.Player`, изменение `GameEnded`.
3. **Клиентская сторона** — `Magbat.Core/Changes/ChangeApplier.cs`, метод
   `Apply` обрабатывает `PieceRemoved` тем же способом (снимает фигуру с поля).

### Вопрос 5. Где переключается ход?

**`Magbat.Core/Rules/GameRules.cs`, метод `Apply` (строки 38–48)** — переключение
хода происходит **после** успешного применения любой команды:

```csharp
if (!(command is PassCommand))
    state.ConsecutivePasses[command.Player] = 0;   // любая не-пасс-команда сбрасывает счётчик пассов

if (state.Winner == null)
{
    state.CurrentPlayer = 1 - state.CurrentPlayer;  // ← переключение хода
    result.Changes.Add(new TurnChanged(state.CurrentPlayer));
}
```

То есть переключение хода — **одна точка** в ядре, общая для всех команд.
Отдельный обработчик пасса — `GameRules.ApplyPass` (строка 158): он только
увеличивает `ConsecutivePasses[cmd.Player]`; при трёх пассах подряд
(`MaxConsecutivePasses = 3`, строка 13) объявляется ничья — `state.Winner = 1 - cmd.Player`
и изменение `GameEnded`. Сам ход переключается после этого в тех же строках 44–48.

**Важно для будущих этапов:** «начало хода владельца» (точка, где должен
срабатывать Тотем) в ядре сейчас **явно не выделено** — есть только конец
текущей фазы команды и факт смены `CurrentPlayer`. Это первое место, где
потребуется точка расширения.

### Вопрос 6. Как `GetLegalCommands` определяет допустимые атаки?

**`Magbat.Core/Rules/GameRules.cs`, метод `GetLegalCommands(GameState state)` (строка 194)**:

1. если `state.Winner != null` — список пуст (строка 197);
2. `player = state.CurrentPlayer`; собираются свои фигуры (`own`) и чужие
   (`enemies`) вспомогательным методом `GetPieces(state, owner)` (строка 237);
3. для каждой своей фигуры (строки 212–225):
   * **ход** — перебираются свободные клетки в пределах `MoveRange`;
   * **атака** — строки 222–224:
     ```csharp
     foreach (var enemy in enemies)
         if (field.GetDistance(piece.X, piece.Y, enemy.X, enemy.Y) <= piece.Definition.AttackRange)
             commands.Add(new AttackCommand(player, piece.Id, enemy.Id));
     ```
     То есть допустима **любая** вражеская фигура на расстоянии не больше
     `AttackRange` атакующего по метрике Чебышёва. Никаких правил «нельзя
     бить того, кого защищает знаменосец» в ядре нет и не требуется.
4. в конец добавляется `PassCommand` (строка 227).

---

## 2. Выбор варианта подключения

Рассматривались два варианта.

| | **Вариант А: компонент расчёта урона** | **Вариант Б: адаптер вокруг `GameState`** |
|---|---|---|
| Идея | В `ApplyAttack` — **одно** обращение к новому компоненту расчёта урона | Новый компонент-адаптер оборачивает `GameState`; `Apply` не меняется |
| Что меняется в существующем коде | 1 строка в `GameRules.ApplyAttack` | 0 строк в `GameRules`, но нужен новый способ вызова компонента снаружи |
| Где живёт «знание» об уроне | в ядре (компонент) | в библиотеке способностей (адаптер) |
| Проблема варианта Б | — | адаптер не может перехватить момент атаки: `ApplyAttack` сам списывает `target.Hp` напрямую. Чтобы адаптер влиял на урон, `GameRules` всё равно пришлось бы менять — либо врезать вызов адаптера в `ApplyAttack` (это уже вариант А), либо переписывать расчёт урона целиком |

**Решение: выбран вариант А.**

Причина (фиксируется в `docs/JOURNAL.md`): правило расчёта урона в ядре
**уже** отделено от проверок атаки — это одна строка
`target.Hp -= attacker.Definition.DefDamage;`. Вариант Б невозможно реализовать
без изменения `GameRules`: атака применяется внутри ядра, и внешний адаптер
не имеет точки, где можно подменить урон. Вариант А требует ровно **одного**
изменения в существующем коде — одной строки в `ApplyAttack` — и даёт ту же
изоляцию, что и Б: библиотека способностей не знает о правилах игры, а ядро
не знает о конкретных способностях.

---

## 3. Схема ядра версии 1

Пять-шесть элементов, из которых собирается ядро; пунктиром показано, что
добавляется экспериментом:

```
                     ┌──────────────────────────────────────┐
                     │        GameRules (Commands)          │
                     │  Apply → ApplyPlace/Move/Attack/Pass│
                     └───────────────┬──────────────────────┘
                                     │ 1. ApplyAttack: одно обращение
                                     ▼
        ┌────────────────────────────────────────────────────────┐
        │  AttackDamageCalculator  ← НОВЫЙ (этап 2)               │
        │  base = attacker.Definition.DefDamage                   │
        │  + Σ IDamageModifier.ModifyAttackDamage(ctx)            │
        └───┬────────────────────────────────────────────┬───────┘
            │ ctx.Attacker / ctx.Target / ctx.BaseDamage  │
            │ ctx.Field (IGameFieldView)                  │
            ▼                                            ▼
   ┌────────────────────────┐              ┌──────────────────────────┐
   │  IGameFieldView  ← НОВЫЙ│              │  IDamageModifier ← НОВЫЙ  │
   │  (read-only)            │              │  + IAbility      ← НОВЫЙ  │
   └───────────┬────────────┘              └───────────┬──────────────┘
               │ реализует                              │ реализует
               ▼                                        ▼
   ┌────────────────────────┐              ┌──────────────────────────┐
   │  GameField (сетка)     │              │  Magbat.Abilities        │
   │  Piece X/Y, Id, Owner  │              │  StandardBearerAbility   │
   └───────────┬────────────┘              └──────────────────────────┘
               │ владеет
               ▼
   ┌────────────────────────────────────────────────────────┐
   │  Piece  ──►  PieceDefinition.AbilityFactory  ← НОВЫЙ   │
   │  (Abilities ← НОВЫЙ)                                   │
   └────────────────────────────────────────────────────────┘
```

Связи по направлению «кто кого вызывает»:

```
GameState ──► GameField ──► Piece ──► PieceDefinition ──► IAbility
    │             │                                      (IDamageModifier)
    │             └──► IGameFieldView  ◄── DamageContext.Field
    └──► AttackDamageCalculator ──► IDamageModifier.ModifyAttackDamage
```

Направление зависимостей важно: библиотека `Magbat.Abilities` зависит от
`Magbat.Core` (нужны `IGameFieldView`, `IDamageModifier`, `DamageContext`),
а `Magbat.Core` **не зависит** от `Magbat.Abilities` — циклической зависимости нет.

---

## 4. Нужны ли изменения в системе `Change` и протоколе?

**Не нужны.** Проверено по коду:

- `Magbat.Core/Changes/PieceAdded.cs` переносит только `PieceName` (и `Id`, `Owner`, координаты);
- `Magbat.Core/Changes/ChangeApplier.cs`, обработчик `PieceAdded`, заново получает
  описание через `PieceCatalog.Get(added.PieceName)` и создаёт `Piece` заново;
- так как `Piece` в конструкторе вызывает `definition.AbilityFactory`, а
  `ChangeApplier` создаёт `Piece` из `PieceDefinition`, **способности
  воспроизводятся на клиенте автоматически** — без единого изменения в `Changes/`
  и без изменения сетевого протокола.

Следствие: состояние способностей (счётчик смертей Тотема, таймер горения)
нельзя передавать в `Change` без нового типа изменения — это будет
отдельной задачей на этапе с Тотомом и Горением.

---

## 5. Обнаруженные расхождения (до → после)

| # | Расхождение | Решение | Категория |
|---|---|---|---|
| 1 | `Magbat.Abilities` — пустой проект под `net8.0` | переведён на `netstandard2.1` (совместимость с `Magbat.Core`), добавлен код | А |
| 2 | `Magbat.Abilities.Tests` содержал только шаблон `UnitTest1.cs` | шаблон удалён, добавлены 15 тестов | В |
| 3 | `Magbat.Abilities.Demo` — «Hello World» | заменён на демонстрацию трёх сценариев | В |
| 4 | `PieceCatalog.Register` был `private` | стал `public` — внешняя библиотека должна регистрировать описания | А |
| 5 | Урон считался напрямую (`DefDamage`), точки расширения не было | добавлены `IDamageModifier` и компонент `AttackDamageCalculator` | А |
| 6 | Нет способности читать состояние поля | добавлен read-only интерфейс `IGameFieldView` | А |
| 7 | Папки `docs/` не было | созданы `RULES.md`, `JOURNAL.md`, `RESULTS.md`, `00-audit.md` | В |
| 8 | `GameField.Distance` — статический метод | сохранён как есть (публичный API не ломаем); для интерфейса добавлен экземплярный `GetDistance` | В |
| 9 | У фигуры не было места для способностей | в `PieceDefinition` добавлен `AbilityFactory`, в `Piece` — `Abilities` | А |
| 10 | Нет точки «начало хода» в ядре | на этом этапе не потребовалась; для Тотета — задача следующего этапа | — |

---

## 6. Что на этом этапе НЕ проверялось

- Тотем чёрной крови, Теневой демон, Горение — **не реализовывались** (по условию задачи).
- Событийная система (`Change`-события для триггеров) — не проектировалась и не проверялась.
- Сравнение композиции и наследования — не проводилось.
- Влияние способностей на сетевую игру (`Magbat.Sim`, 1000 партий) — не проверялось:
  регистрация фигуры `Standard_Bearer` в `PieceCatalog` на сервере не выполнялась.
