using System.Collections.Concurrent;
using System.Diagnostics;
using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class EventServerTests
{
    [Fact]
    public void Submit_ValidCommand_ProducesUpdateWithChanges()
    {
        using var received = new ManualResetEventSlim();
        using var server = new EventServer(new GameRules());
        ServerMessage? message = null;
        server.Updated += m => { message = m; received.Set(); };
        server.AddGame(1, GameFactory.NewGame());
        server.Start();

        server.Submit(new ClientMessage(1, new PlaceCommand(0, "Warrior_One", 3, 3), Stopwatch.GetTimestamp()));

        Assert.True(received.Wait(2000));
        Assert.Equal(ApplyError.None, message!.Error);
        Assert.Contains(message.Changes, c => c is PieceAdded);
        Assert.Contains(message.Changes, c => c is TurnChanged);
    }

    [Fact]
    public void Submit_InvalidCommand_ReturnsErrorWithoutChanges()
    {
        using var received = new ManualResetEventSlim();
        using var server = new EventServer(new GameRules());
        ServerMessage? message = null;
        server.Updated += m => { message = m; received.Set(); };
        server.AddGame(1, GameFactory.NewGame());
        server.Start();

        // сейчас ход игрока 0, а команду прислал игрок 1
        server.Submit(new ClientMessage(1, new PlaceCommand(1, "Warrior_One", 3, 3), Stopwatch.GetTimestamp()));

        Assert.True(received.Wait(2000));
        Assert.Equal(ApplyError.NotYourTurn, message!.Error);
        Assert.Empty(message.Changes);
    }

    [Fact]
    public void Submit_KeepsCommandOrderAndCopiesSendTime()
    {
        using var done = new CountdownEvent(2);
        using var server = new EventServer(new GameRules());
        var messages = new ConcurrentQueue<ServerMessage>();
        server.Updated += m => { messages.Enqueue(m); done.Signal(); };
        server.AddGame(1, GameFactory.NewGame());
        server.Start();

        server.Submit(new ClientMessage(1, new PlaceCommand(0, "Warrior_One", 3, 3), 111));
        server.Submit(new ClientMessage(1, new PlaceCommand(1, "Mage_One", 6, 6), 222));

        Assert.True(done.Wait(2000));
        var list = messages.ToArray();
        Assert.Equal(0, list[0].Player);
        Assert.Equal(111, list[0].CommandSentAt);
        Assert.Equal(1, list[1].Player);
        Assert.Equal(222, list[1].CommandSentAt);
        Assert.All(list, m => Assert.Equal(ApplyError.None, m.Error));
    }
}