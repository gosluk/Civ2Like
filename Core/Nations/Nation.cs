using Civ2Like.Core.Cities;

namespace Civ2Like.Core.Nations;

public sealed class Nation : UuidObject, IEquatable<Nation>
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Unnamed Nation";

    // Affiliated ideologies: the nation’s lean over time
    public IdeologyProfile Ideology { get; set; } = new();

    public bool Equals(Nation? other) => Id.Equals(other?.Id);
}

