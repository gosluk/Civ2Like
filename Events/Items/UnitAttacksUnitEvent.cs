using Civ2Like.Core;
using Civ2Like.Core.Units;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging; // if your Unit/UnitState live here

namespace Civ2Like.Events.Items
{
    public partial class UnitAttacksUnitEvent : IGameEvent
    {
        private Random _rand = new();

        public required Guid AttackerUnitId { get; init; }

        public required Guid DefenderUnitId { get; init; }

        public required bool IsRanged { get; init; }

        public void Apply(Game game)
        {
            var attacker = game.Units.FirstOrDefault(u => u.Id == AttackerUnitId);
            var defender = game.Units.FirstOrDefault(u => u.Id == DefenderUnitId);

            if (attacker is null || defender is null)
            {
                throw new ArgumentException($"Both attacker and defender must be valid units. {AttackerUnitId} {DefenderUnitId}");
            }

            // Melee must be adjacent (distance 1). Ranged can be > 1 (no LoS/range check here).
            if (!IsRanged && Hexagon.Hex.Distance(attacker.Pos, defender.Pos) > 1)
            {
                throw new ArgumentException($"Can not do melee attack on a distance. {Hexagon.Hex.Distance(attacker.Pos, defender.Pos)}");
            }

            while (attacker.Health > 0 && defender.Health > 0)
            {
                double damage = DealAttack(game, attacker, defender);

                WeakReferenceMessenger.Default.Send(new UnitDamageInflictedEvent()
                {
                    Attacker = attacker,
                    Defender = defender,
                    Damage = damage,
                    IsRanged = IsRanged,
                    IsCritical = defender.Health == 0,
                });

                // Melee retaliation (only if defender survived and is not a ranged attack)
                if (!IsRanged && defender.Health > 0 && defender.UnitType.AttackMelee > 0)
                {
                    damage = DealAttack(game, defender, attacker);

                    WeakReferenceMessenger.Default.Send(new UnitDamageInflictedEvent()
                    {
                        Attacker = defender,
                        Defender = attacker,
                        Damage = damage,
                        IsRanged = IsRanged,
                        IsCritical = attacker.Health == 0,
                    });
                }

                // ----------- Death & advance resolution -----------
                if (defender.Health <= 0)
                {
                    game.ProcessEvent(new UnitKilledEvent()
                    {
                        UnitId = defender.Id,
                        KillerId = attacker.Id
                    });

                    if (!IsRanged)
                    {
                        // Only melee advances
                        game.ProcessEvent(new UnitMovedEvent()
                        {
                            UnitId = attacker.Id,
                            From = attacker.Pos,
                            To = defender.Pos,
                        });
                        attacker.Pos = defender.Pos;
                    }
                }

                if (attacker.Health <= 0)
                {
                    game.ProcessEvent(new UnitKilledEvent()
                    {
                        UnitId = attacker.Id,
                        KillerId = defender.Id
                    });
                }
            }
        }

        private double DealAttack(Game game, Unit attacker, Unit defender)
        {
            // ----------- Initialization -----------
            var attackerProfile = new EffectiveProfile(attacker);
            var defenderProfile = new EffectiveProfile(defender);

            // ----------- Combat stats -----------
            // Attacker's attack strength
            double attackStrength = IsRanged
                ? attackerProfile.GenerateRangeAttack(_rand)
                : attackerProfile.GenerateMeleeAttack(_rand);

            // Defender's defense vs the attack type
            double baseDefenderDefense = IsRanged
                ? attackerProfile.GenerateRangeDefense(_rand)
                : attackerProfile.GenerateMeleeDefense(_rand);

            // Terrain bonus where the defender stands
            var defTerrain = game.Map[defender.Pos].Terrain;
            double terrainBonus = TerrainDefenseBonus(defTerrain);

            // Fortify bonus applies only if fortified AND on land
            double fortifyBonus = IsFortified(defender);

            double effectiveDefenderDefense = baseDefenderDefense * (1.0 + terrainBonus + fortifyBonus);

            // Damage to defender (always applied)
            double dmgToDefender = Deal(attackStrength, effectiveDefenderDefense, baseDamage: 10);
            defender.Health = (uint)Math.Round(Math.Max(0, defender.Health - dmgToDefender));

            return dmgToDefender;
        }

        // ----------- Helpers -----------


        private sealed class EffectiveProfile
        {
            public EffectiveProfile(Unit unit)
            {
                var stats = unit.EffectiveStats();
                AttackRange = stats.AttackRange;
                AttackMelee = stats.AttackMelee;
                DefenseRange = stats.DefenseRanged;
                DefenseMelee = stats.DefenseMelee;
                Health = unit.Health;
                MaxHealth = stats.MaxHealth;
            }

            public double AttackRange { get; }

            public double AttackMelee { get; }

            public double DefenseRange { get; }

            public double DefenseMelee { get; }

            public double Health { get; set; }

            public double MaxHealth { get; }

            private double GetCoefficient(Random rand) => Health / MaxHealth + (rand.NextDouble() * 0.1);

            public double GenerateMeleeAttack(Random rand) => AttackMelee * GetCoefficient(rand);

            public double GenerateMeleeDefense(Random rand) => DefenseMelee * GetCoefficient(rand);

            public double GenerateRangeAttack(Random rand) => AttackRange * GetCoefficient(rand);

            public double GenerateRangeDefense(Random rand) => DefenseRange * GetCoefficient(rand);
        }

        private static double IsFortified(Unit unit) => unit.State == UnitState.Fortified ? 0.25 : 0.0;

        private static double TerrainDefenseBonus(Terrain t) => t switch
        {
            Terrain.Mountains => 0.50,
            Terrain.Hills => 0.25,
            Terrain.Forest => 0.25,
            Terrain.Coast => 0.10,
            _ => 0.00
        };

        private static double Deal(double attack, double defense, int baseDamage = 10)
        {
            // Smooth, deterministic damage: scales by A / (A + D), clamped a bit.
            if (attack <= 0)
            {
                throw new ArgumentException("Attack must be greater than 0.");
            }
            double ratio = attack / (attack + Math.Max(1.0, defense));
            double dmg = Math.Round(baseDamage * Math.Clamp(ratio, 0.05, 0.95));
            return Math.Max(1, dmg);
        }
    }
}
