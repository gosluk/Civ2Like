namespace Civ2Like.Core.Nations;

public struct PopulationUnit
{
    public required Nation Nation { get; init; }

    public ulong Value { get; set; }

    public uint Units => (uint)(Value / 1000);
}
