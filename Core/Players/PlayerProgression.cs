using Civ2Like.Core.Economy;

namespace Civ2Like.Core.Players;

public class PlayerProgression
{
    public decimal Gold { get; private set; }
    public decimal Science { get; private set; }
    public decimal Culture { get; private set; }

    public void Add(Yield y)
    {
        Gold    += y.Gold;
        Science += y.Science;
        Culture += y.Culture;
    }

    public void SpendGold(decimal amount) => Gold = System.Math.Max(0, Gold - amount);
}
