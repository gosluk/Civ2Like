namespace Civ2Like.Core.Nations;

public sealed class Nation : UuidObject, IEquatable<Nation>
{
    public required string Name { get; set; }

    // Affiliated ideologies: the nation’s lean over time
    public required IdeologyProfile Ideology { get; set; }

    public List<NationBonus> Bonuses { get; } = [];

    public bool Equals(Nation? other) => Id.Equals(other?.Id);

    public override string ToString() => $"{Name} ({Id}), {Bonuses} {Ideology}";
}

