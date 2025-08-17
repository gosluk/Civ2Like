using Civ2Like.Core;
using Civ2Like.Core.Units;

namespace Civ2Like.Events.Items;

public sealed class UnitAttacksUnitEvent : IGameEvent
{
    public required Guid AttackerId { get; init; }
    
    public required Guid DefenderId { get; init; }

    public void Apply(Core.Game game)
    {
        Random rand = new();

        var attacker = game.Units[AttackerId];
        var defender = game.Units[DefenderId];

        //var attackerProfile = new EffectiveProfile(attacker);
        //var defenderProfile = new EffectiveProfile(defender);

        //bool isRanged = attacker.Pos.Distance(defender.Pos) > 1;

        //while (attackerProfile.Health > 0 && defenderProfile.Health > 0)
        //{

        //}
    }

    //private static void Strike(double attack, Unit attacker, double defense, Unit defender, Random rng)
    //{
    //    const double minHit = 0.10;
    //    const double maxHit = 0.90;
    //    const double S = 6; // Sigmoid steepness
    //    const double gamma = 0.7; // Damage scaling

    //    // Effective stats (add your terrain/promotions here)
    //    double atk = attack * PowerScale(attacker.Health, attacker.UnitType.MaxHealth);
    //    double def = defense * PowerScale(defender.Health, defender.UnitType.MaxHealth);

    //    double pHit = Clamp(MidSigmoid(atk - def, S), minHit, maxHit);
    //    if (rng.NextDouble() < pHit)
    //    {
    //        double ratio = Pow(atk, gamma) / (Pow(atk, gamma) + Pow(def, gamma) + 1e-9);
    //        double v = Lerp(1 - varPct, 1 + varPct, rng.NextDouble());
    //        int dmg = Math.Max(1, (int)Math.Round(baseDmg * ratio * v));
    //        uDef = uDef with { Health = uDef.Health - dmg };
    //    }
    //}

    //private static double PowerScale(double health, double maxHealth) => 0.5 + 0.5 * (double)health / maxHealth;

    //private static double MidSigmoid(double x, double s) => 1.0 / (1.0 + Math.Exp(-x / s));

    //private static double Clamp(double x, double lo, double hi) => x < lo ? lo : (x > hi ? hi : x);

    //private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    //private static double Pow(double x, double y) => Math.Pow(Math.Max(0.000001, x), y);

    //private sealed class EffectiveProfile
    //{
    //    public EffectiveProfile(Unit unit)
    //    {
    //        var stats = unit.EffectiveStats();
    //        AttackRange = stats.AttackRange;
    //        AttackMelee = stats.AttackMelee;
    //        DefenseRange = stats.DefenseRanged;
    //        DefenseMelee = stats.DefenseMelee;
    //        Health = unit.Health;
    //    }

    //    public double AttackRange { get; }

    //    public double AttackMelee { get; }

    //    public double DefenseRange { get; }

    //    public double DefenseMelee { get; }

    //    public double Health { get; set; }

    //    public double GenerateMeleeArrack(Random rand)
    //    {
    //       return AttackMelee * (1 + rand.NextDouble() * 0.2);
    //    }

    //    public double GenerateMeleeDefense(Random rand)
    //    {
    //        return DefenseMelee * (1 + rand.NextDouble() * 0.2);
    //    }

    //    public double GenerateRangeAttack(Random rand)
    //    {
    //        return AttackRange * (1 + rand.NextDouble() * 0.2);
    //    }

    //    public double GenerateRangeDefense(Random rand)
    //    {
    //        return DefenseRange * (1 + rand.NextDouble() * 0.2);
    //    }
    //}
}
