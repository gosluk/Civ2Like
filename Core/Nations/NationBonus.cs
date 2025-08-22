namespace Civ2Like.Core.Nations;

public sealed class NationBonus
{
    public double ProductionMultiplier { get; set; }

    public double GrowthMultiplier { get; set; }

    public double CombatStrengthMultiplier { get; set; }

    public double MigrationMultiplier { get; set; }

    public override string ToString() =>
        $"Prod x{ProductionMultiplier:0.00}, Growth x{GrowthMultiplier:0.00}, Combat x{CombatStrengthMultiplier:0.00}, Migration x{MigrationMultiplier:0.00}";
}
