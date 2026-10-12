using System;
using System.Collections.Generic;

namespace Magbat.Core
{
    /// <summary>
    /// Правила игры
    /// </summary>
    public class GameRules
    {
        public const int MaxConsecutivePasses = 3;
        
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

            return result;
        }
        
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

        private ApplyResult ApplyPass(GameState state, PassCommand cmd)
        {
            state.ConsecutivePasses[cmd.Player]++;
            if (state.ConsecutivePasses[cmd.Player] >= MaxConsecutivePasses)
                state.Winner = 1 - cmd.Player;
            return ApplyResult.Ok();
        }
        
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

        public List<Command> GetLegalCommands(GameState state)
        {
            return new List<Command>();
        }
    }
}