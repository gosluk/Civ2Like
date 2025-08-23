using System.Collections.Immutable;

namespace Civ2Like.Core.Economy;

public readonly struct Yield
{
    // Fields changed from long to decimal
    public readonly decimal Gold, Science, Culture, Food, Timber, Stone, Iron;

    public Yield(
        decimal gold = 0m,
        decimal science = 0m,
        decimal culture = 0m,
        decimal food = 0m,
        decimal timber = 0m,
        decimal stone = 0m,
        decimal iron = 0m)
    {
        Gold = gold;
        Science = science;
        Culture = culture;
        Food = food;
        Timber = timber;
        Stone = stone;
        Iron = iron;
    }

    public static Yield operator +(Yield a, Yield b)
        => new(
            gold: a.Gold + b.Gold,
            science: a.Science + b.Science,
            culture: a.Culture + b.Culture,
            food: a.Food + b.Food,
            timber: a.Timber + b.Timber,
            stone: a.Stone + b.Stone,
            iron: a.Iron + b.Iron);

    public bool IsZero =>
        Gold == 0m && Science == 0m && Culture == 0m &&
        Food == 0m && Timber == 0m && Stone == 0m && Iron == 0m;

    public IReadOnlyDictionary<ResourceType, decimal> AsDict() => new Dictionary<ResourceType, decimal>
    {
        { ResourceType.Gold, Gold },
        { ResourceType.Science, Science },
        { ResourceType.Culture, Culture },
        { ResourceType.Food, Food },
        { ResourceType.Timber, Timber },
        { ResourceType.Stone, Stone },
        { ResourceType.Iron, Iron }
    }.ToImmutableDictionary();
}
