using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class MessageSizeTests
{
    [Fact]
    public void Size_IncludesFieldsOfDerivedCommands()
    {
        int pass = MessageSize.Of(new ClientMessage(1, new PassCommand(0), 0));
        int move = MessageSize.Of(new ClientMessage(1, new MoveCommand(0, 5, 3, 4), 0));

        Assert.True(move > pass + 20);
    }

    [Fact]
    public void Size_IgnoresTimestamps()
    {
        int a = MessageSize.Of(new ClientMessage(1, new PassCommand(0), 1));
        int b = MessageSize.Of(new ClientMessage(1, new PassCommand(0), 9_000_000_000_000));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Snapshot_IsLargerThanDelta()
    {
        var state = GameFactory.NewGame();
        new GameRules().Apply(state, new PlaceCommand(0, "Warrior_One", 3, 3));

        var delta = new ServerMessage(1, 0, 0, ApplyError.None, new Change[] { new TurnChanged(1) });
        var snapshot = new ServerMessage(1, 0, 0, ApplyError.None, new Change[] { StateSnapshot.From(state) });

        Assert.True(MessageSize.Of(snapshot) > MessageSize.Of(delta));
    }
}