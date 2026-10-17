using System.Diagnostics;
using Magbat.Sim;

namespace Magbat.Tests;

public class DelayedChannelTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Delivery_HappensAfterConfiguredDelay(int delayMs)
    {
        using var done = new ManualResetEventSlim();
        long deliveredAt = 0;
        using var channel = new DelayedChannel<int>(delayMs, _ =>
        {
            deliveredAt = Stopwatch.GetTimestamp();
            done.Set();
        });

        long sentAt = Stopwatch.GetTimestamp();
        channel.Send(1);
        Assert.True(done.Wait(2000));

        double actualMs = Stopwatch.GetElapsedTime(sentAt, deliveredAt).TotalMilliseconds;
        Assert.InRange(actualMs, delayMs, delayMs + 25);
    }

    [Fact]
    public void Delivery_KeepsOrder()

    {
        var received = new List<int>();
        using (var channel = new DelayedChannel<int>(20, m => received.Add(m)))
        {
            for (int i = 0; i < 50; i++) channel.Send(i);
        } // Dispose дождётся доставки всего

        Assert.Equal(Enumerable.Range(0, 50), received);
    }
    
    [Fact]
    public void Delivery_CountsMessagesAndBytes()
    {
        var channel = new DelayedChannel<int>(0, _ => { }, m => 10);
        for (int i = 0; i < 5; i++) channel.Send(i);
        channel.Dispose();

        Assert.Equal(5, channel.MessagesDelivered);
        Assert.Equal(50, channel.BytesDelivered);
    }
}