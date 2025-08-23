namespace Civ2Like.Core.Nations;

public sealed class PopulationUnit
{
    public required Nation Nation { get; init; }

    public decimal Value { get; set; }

    public decimal PopulationUnits => Value * 0.001m;

    public PopulationUnit Clone() => new PopulationUnit { Nation = Nation, Value = Value, };
}
