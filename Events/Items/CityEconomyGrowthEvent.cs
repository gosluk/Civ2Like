using Civ2Like.Core;
using Civ2Like.Core.Economy;

namespace Civ2Like.Events.Items;

public sealed class CityEconomyGrowthEvent : IGameEvent
{
    public void Apply(Game game)
    {
        // cities first: stockpiles and production live there
        foreach (var city in game.Cities)
        {
            var yields = CityEconomy.ComputeCityYield(game, city);

            // give abstract flows to the player
            city.Player.Progress.Add(new Yield(gold: yields.Gold, science: yields.Science, culture: yields.Culture));

            // stockpile physicals + growth/starvation
            CityEconomy.ApplyConsumptionAndGrowth(city, yields);

            // production points: simple “hammers” derived from timber/stone this turn
            decimal production = CityEconomy.ComputeProductionPoints(yields);
            CityEconomy.ApplyProduction(city, production);
        }
    }
}
