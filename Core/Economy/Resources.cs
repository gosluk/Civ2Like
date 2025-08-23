namespace Civ2Like.Core.Economy;

/// <summary>
/// Core resources. “Abstract” ones (Gold/Science/Culture) aren’t stockpiled per city.
/// Physical ones are stockpiled in cities with storage caps and can be consumed by builds.
/// </summary>
[Flags]
public enum ResourceFlags { None = 0, Storable = 1 }

public enum ResourceType
{
    // Abstract, non-storable (flow to player per turn)
    Gold,
    Science,
    Culture,

    // Storable, physical (live in city stockpiles)
    Food,
    Timber,
    Stone,
    Iron
}

public static class ResourceMeta
{
    public static bool IsStorable(ResourceType r)
        => r is ResourceType.Food or ResourceType.Timber or ResourceType.Stone or ResourceType.Iron;
}

