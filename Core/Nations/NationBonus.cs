namespace Civ2Like.Core.Nations;

public sealed class NationBonus
{
    public decimal ProductionMultiplier { get; set; }

    public decimal GrowthMultiplier { get; set; }

    public decimal CombatStrengthMultiplier { get; set; }

    public decimal MigrationMultiplier { get; set; }

    public override string ToString() =>
        $"[Prod x{ProductionMultiplier:0.00}, Growth x{GrowthMultiplier:0.00}, Combat x{CombatStrengthMultiplier:0.00}, Migration x{MigrationMultiplier:0.00}]";
}
