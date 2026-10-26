using Magbat.Sim;

namespace Magbat.Tests;

public class StatsTests
{
    [Fact]
    public void Percentile_UsesNearestRank()
    {
        var sorted = Enumerable.Range(1, 100).Select(i => (double)i).ToList();

        Assert.Equal(95, Stats.Percentile(sorted, 0.95));
        Assert.Equal(7, Stats.Percentile(new List<double> { 7 }, 0.95));
    }
}