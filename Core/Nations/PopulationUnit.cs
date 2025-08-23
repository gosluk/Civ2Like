namespace Civ2Like.Core.Nations;

public sealed class PopulationUnit
{
    public required Nation Nation { get; init; }

    public ulong Value { get; set; }

    public uint Units => (uint)(Value / 1000);

    public PopulationUnit Clone() => new PopulationUnit { Nation = Nation, Value = Value, };
}
