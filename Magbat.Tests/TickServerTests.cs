using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class TickServerTests
{
    [Fact]
    public void IdleServer_KeepsRequestedTickRateOnAverage()
    {
        var server = new TickServer(new GameRules(), 30, BroadcastMode.Delta);
        server.Start();
        Thread.Sleep(1000);
        server.Dispose();

        Assert.InRange(server.MeanIntervalMs, 30, 37); // цель 33,3 мс
        Assert.Equal(server.Ticks, server.IdleTicks);
    }
}