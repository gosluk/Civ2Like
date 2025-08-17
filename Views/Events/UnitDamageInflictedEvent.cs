using Civ2Like.Core.Units;

namespace Civ2Like.Views.Events;

internal class UnitDamageInflictedEvent
{
    public required Unit Attacker { get; init; }

    public required Unit Defender { get; init; }

    public double Damage { get; init; }

    public bool IsRanged { get; init; }

    public bool IsCritical { get; init; }
}
