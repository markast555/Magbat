using Magbat.Core;

namespace Magbat.Tests;

public static class StateAssert
{
    public static void Same(GameState a, GameState b)
    {
        Assert.Equal(a.CurrentPlayer, b.CurrentPlayer);
        Assert.Equal(a.Winner, b.Winner);
        Assert.Equal(a.Hands[0], b.Hands[0]);
        Assert.Equal(a.Hands[1], b.Hands[1]);

        for (int x = 0; x < a.Field.Width; x++)
        for (int y = 0; y < a.Field.Height; y++)
        {
            var pa = a.Field.GetAt(x, y);
            var pb = b.Field.GetAt(x, y);
            Assert.Equal(pa == null, pb == null);
            if (pa == null || pb == null) continue;
            Assert.Equal(pa.Id, pb.Id);
            Assert.Equal(pa.Owner, pb.Owner);
            Assert.Equal(pa.Definition.Name, pb.Definition.Name);
            Assert.Equal(pa.Hp, pb.Hp);
        }
    }
}