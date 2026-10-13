using System;
using System.Collections.Generic;
using System.Linq;

namespace Magbat.Core
{
    /// <summary>
    /// Правила игры
    /// </summary>
    public class GameRules
    {
        public const int MaxConsecutivePasses = 3;
        
        /// <summary>
        /// Применяет поступившую команду и изменяет состояние игры
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="command">Поступившая команда</param>
        /// <returns>Результат применения: успех или причина отказа</returns>
        public ApplyResult Apply(GameState state, Command command)
        {
            if (state.Winner != null)
                return ApplyResult.Fail("Game is over");
            if (command.Player != state.CurrentPlayer)
                return ApplyResult.Fail("Not your turn");

            ApplyResult result;
            switch (command)
            {
                case PlaceCommand place:   result = ApplyPlace(state, place); break;
                case MoveCommand move:     result = ApplyMove(state, move); break;
                case AttackCommand attack: result = ApplyAttack(state, attack); break;
                case PassCommand pass:     result = ApplyPass(state, pass); break;
                default: return ApplyResult.Fail("Unknown command");
            }

            if (result.Success && !(command is PassCommand))
                state.ConsecutivePasses[command.Player] = 0;
            
            if (result.Success && state.Winner == null)
                state.CurrentPlayer = 1 - state.CurrentPlayer;
            
            return result;
        }
        
        /// <summary>
        /// Применяет команду выставления фигуры на поле
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="cmd">Команда выставления фигуры</param>
        /// <returns>Результат применения</returns>
        private ApplyResult ApplyPlace(GameState state, PlaceCommand cmd)
        {
            var field = state.Field;
            var hand = state.Hands[cmd.Player];

            if (!hand.Contains(cmd.PieceName))
                return ApplyResult.Fail("Piece is not in hand");
            if (!field.InBounds(cmd.X, cmd.Y))
                return ApplyResult.Fail("Cell is out of bounds");
            if (!field.IsEmpty(cmd.X, cmd.Y))
                return ApplyResult.Fail("Cell is occupied");

            var definition = PieceCatalog.Get(cmd.PieceName);
            var piece = new Piece(state.NextPieceId++, cmd.Player, definition, cmd.X, cmd.Y);
            field.Put(piece);
            hand.Remove(cmd.PieceName);
            return ApplyResult.Ok();
        }
        
        /// <summary>
        /// Применяет команду перемещения фигуры
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="cmd">Команда перемещения фигуры</param>
        /// <returns>Результат применения</returns>
        private ApplyResult ApplyMove(GameState state, MoveCommand cmd)
        {
            var field = state.Field;
            var piece = FindPiece(state, cmd.PieceId);
            if (piece == null || piece.Owner != cmd.Player)
                return ApplyResult.Fail("No such piece of yours");
            if (!field.InBounds(cmd.ToX, cmd.ToY))
                return ApplyResult.Fail("Cell is out of bounds");
            if (!field.IsEmpty(cmd.ToX, cmd.ToY))
                return ApplyResult.Fail("Cell is occupied");

            int distance = GameField.Distance(piece.X, piece.Y, cmd.ToX, cmd.ToY);
            if (distance == 0 || distance > piece.Definition.MoveRange)
                return ApplyResult.Fail("Too far");

            field.Remove(piece.X, piece.Y);
            piece.X = cmd.ToX;
            piece.Y = cmd.ToY;
            field.Put(piece);
            return ApplyResult.Ok();
        }
        
        /// <summary>
        /// Применяет команду атаки фигуры
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="cmd">Команда атаки фигуры</param>
        /// <returns>Результат применения</returns>
        private ApplyResult ApplyAttack(GameState state, AttackCommand cmd)
        {
            var attacker = FindPiece(state, cmd.AttackerId);
            var target = FindPiece(state, cmd.TargetId);
            if (attacker == null || attacker.Owner != cmd.Player)
                return ApplyResult.Fail("No such attacker of yours");
            if (target == null || target.Owner == cmd.Player)
                return ApplyResult.Fail("Invalid target");

            int distance = GameField.Distance(attacker.X, attacker.Y, target.X, target.Y);
            if (distance > attacker.Definition.AttackRange)
                return ApplyResult.Fail("Target out of range");

            target.Hp -= attacker.Definition.DefDamage;
            if (target.Hp <= 0)
            {
                state.Field.Remove(target.X, target.Y);
                if (CountPieces(state, target.Owner) == 0 && state.Hands[target.Owner].Count == 0)
                    state.Winner = cmd.Player;
            }
            return ApplyResult.Ok();
        }

        /// <summary>
        /// Применяет команду пропуска хода
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="cmd">Команда пропуска хода</param>
        /// <returns>Результат применения</returns>
        private ApplyResult ApplyPass(GameState state, PassCommand cmd)
        {
            state.ConsecutivePasses[cmd.Player]++;
            if (state.ConsecutivePasses[cmd.Player] >= MaxConsecutivePasses)
                state.Winner = 1 - cmd.Player;
            return ApplyResult.Ok();
        }
        
        /// <summary>
        /// Ищет обозначенную фигуру на поле
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="id">ID искомой фигуры</param>
        /// <returns>Найденная фигура или null, если такой нет</returns>
        private Piece? FindPiece(GameState state, int id)
        {
            var field = state.Field;
            for (int x = 0; x < field.Width; x++)
            for (int y = 0; y < field.Height; y++)
            {
                var piece = field.GetAt(x, y);
                if (piece != null && piece.Id == id) return piece;
            }
            return null;
        }
        
        /// <summary>
        /// Считает фигуры игрока на поле
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="owner">Владелец</param>
        /// <returns>Количество найденных фигур</returns>
        private int CountPieces(GameState state, int owner)
        {
            int count = 0;
            var field = state.Field;
            for (int x = 0; x < field.Width; x++)
            for (int y = 0; y < field.Height; y++)
            {
                var piece = field.GetAt(x, y);
                if (piece != null && piece.Owner == owner) count++;
            }
            return count;
        }

        /// <summary>
        /// Возвращает список всех доступных команд
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <returns>Список доступных команд</returns>
        public List<Command> GetLegalCommands(GameState state)
        {
            var commands = new List<Command>();
            if (state.Winner != null)
                return commands;

            int player = state.CurrentPlayer;
            var field = state.Field;
            var own = GetPieces(state, player);
            var enemies = GetPieces(state, 1 - player);

            // выставление: каждое название из руки на каждую свободную клетку
            foreach (var name in state.Hands[player].Distinct())
                for (int x = 0; x < field.Width; x++)
                for (int y = 0; y < field.Height; y++)
                    if (field.IsEmpty(x, y))
                        commands.Add(new PlaceCommand(player, name, x, y));

            foreach (var piece in own)
            {
                // перемещение: свободные клетки в пределах дальности хода
                int range = piece.Definition.MoveRange;
                for (int x = Math.Max(0, piece.X - range); x <= Math.Min(field.Width - 1, piece.X + range); x++)
                for (int y = Math.Max(0, piece.Y - range); y <= Math.Min(field.Height - 1, piece.Y + range); y++)
                    if (field.IsEmpty(x, y))
                        commands.Add(new MoveCommand(player, piece.Id, x, y));

                // атака: вражеские фигуры в пределах дальности атаки
                foreach (var enemy in enemies)
                    if (GameField.Distance(piece.X, piece.Y, enemy.X, enemy.Y) <= piece.Definition.AttackRange)
                        commands.Add(new AttackCommand(player, piece.Id, enemy.Id));
            }

            commands.Add(new PassCommand(player));
            return commands;
        }
        
        /// <summary>
        /// Даёт список выставленных фигур
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="owner">Владелец</param>
        /// <returns>Список выставленных фигур</returns>
        private List<Piece> GetPieces(GameState state, int owner)
        {
            var result = new List<Piece>();
            var field = state.Field;
            for (int x = 0; x < field.Width; x++)
            for (int y = 0; y < field.Height; y++)
            {
                var piece = field.GetAt(x, y);
                if (piece != null && piece.Owner == owner)
                    result.Add(piece);
            }
            return result;
        }
    }
}