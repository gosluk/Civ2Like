using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Civ2Like.Core.NameGeneration;

/// <summary>
/// Generates flavorful unit names:
/// - Reproducible via optional seed
/// - Avoids repeats (EnsureUnique)
/// - Themes: Generic, Land, Naval, Air, Fantasy
/// - Patterns: e.g. "XII Legion", "3rd Company", "Iron Wolves", "Raven Guard",
///             "HMS Dawnfire", "Stormwing Squadron", "Order of the Silver Flame"
/// </summary>
public sealed class UnitNameGenerator
{
    public enum UnitNameTheme
    {
        Generic,
        Land,
        Naval,
        Air,
        Fantasy
    }

    private readonly Random _rng;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UnitNameTheme Theme { get; set; } = UnitNameTheme.Generic;

    /// <summary>When true, generator will avoid returning names that were produced before during this instance's lifetime.</summary>
    public bool EnsureUnique { get; set; } = true;

    /// <summary>Max attempts to find a unique name before best-effort fallback.</summary>
    public int MaxAttempts { get; set; } = 64;

    /// <summary>Chance to include numeric designation like "VII" or "3rd" (0..1).</summary>
    public double NumberingChance { get; set; } = 0.55;

    /// <summary>Use Roman numerals (true) or ordinals (false) when numbering is applied.</summary>
    public bool UseRomanNumerals { get; set; } = true;

    /// <summary>Chance to use a "The ..." article for group names (0..1).</summary>
    public double ArticleChance { get; set; } = 0.28;

    /// <summary>Naval ship prefix used by the Naval theme (e.g., "HMS").</summary>
    public string NavalShipPrefix { get; set; } = "HMS";

    public UnitNameGenerator(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>Clear the internal used-name registry.</summary>
    public void ResetUsed() { lock (_lock) _used.Clear(); }

    /// <summary>Pre-populate the "used" set to avoid duplicates across saves or sessions.</summary>
    public void MarkAsUsed(IEnumerable<string> names)
    {
        if (names == null) return;
        lock (_lock)
        {
            foreach (var n in names)
                if (!string.IsNullOrWhiteSpace(n))
                    _used.Add(n.Trim());
        }
    }

    /// <summary>Generate a single unit name.</summary>
    public string Next()
    {
        lock (_lock)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                string name = Theme switch
                {
                    UnitNameTheme.Naval => MakeNaval(),
                    UnitNameTheme.Air => MakeAir(),
                    UnitNameTheme.Fantasy => MakeFantasy(),
                    UnitNameTheme.Land => MakeLand(),
                    _ => MakeGeneric(),
                };

                name = ToTitleCase(CleanupSpaces(name));

                if (!LooksNice(name)) continue;
                if (EnsureUnique && _used.Contains(name)) continue;

                _used.Add(name);
                return name;
            }

            // Fallback: force uniqueness with suffix number
            var baseName = ToTitleCase(CleanupSpaces(MakeGeneric()));
            if (EnsureUnique)
            {
                var candidate = baseName;
                int i = 2;
                while (_used.Contains(candidate) && i < 1000)
                    candidate = $"{baseName} {i++}";
                _used.Add(candidate);
                return candidate;
            }
            return baseName;
        }
    }

    /// <summary>Generate many names.</summary>
    public IEnumerable<string> NextMany(int count)
    {
        for (int i = 0; i < count; i++) yield return Next();
    }

    // ====== Patterns ======

    private string MakeGeneric()
    {
        // Blend between land-style groups and evocative fantasy-ish group names
        var t = GetTheme(UnitNameTheme.Generic);
        return _rng.NextDouble() < 0.6 ? GroupName(t) : FlairGroup(t);
    }

    private string MakeLand()
    {
        var t = GetTheme(UnitNameTheme.Land);
        // 50% numbered formation, 50% adjective/animal regiment
        return _rng.NextDouble() < 0.5 ? FormationName(t) : GroupName(t);
    }

    private string MakeNaval()
    {
        var t = GetTheme(UnitNameTheme.Naval);
        // 60% ship name, 40% fleet/squadron
        return _rng.NextDouble() < 0.6 ? ShipName(t) : NavalGroup(t);
    }

    private string MakeAir()
    {
        var t = GetTheme(UnitNameTheme.Air);
        // 40% numbered squadron/wing, 60% themed group
        return _rng.NextDouble() < 0.4 ? AirNumbered(t) : AirGroup(t);
    }

    private string MakeFantasy()
    {
        var t = GetTheme(UnitNameTheme.Fantasy);
        // 30% "Order of the ...", 70% adjective+noun / animal+title
        return _rng.NextDouble() < 0.3 ? OrderOf(t) : FlairGroup(t);
    }

    // ---- Building blocks ----

    private string FormationName(ThemeParts t)
    {
        string num = _rng.NextDouble() < NumberingChance ? MakeNumberToken() + " " : string.Empty;
        string title = Pick(t.Titles);
        // e.g., "VII Legion", "3rd Company"
        return num + title;
    }

    private string GroupName(ThemeParts t)
    {
        // e.g., "Iron Wolves", "Jade Lancers", "Raven Guard", "Golden Phalanx"
        bool useArticle = _rng.NextDouble() < ArticleChance;
        string left = _rng.NextDouble() < 0.5 ? Pick(t.Adjectives) : Pick(t.Animals);
        string right = Pick(t.NounsPlural);

        var core = $"{left} {right}";
        return useArticle ? $"The {core}" : core;
    }

    private string FlairGroup(ThemeParts t)
    {
        // e.g., "Crimson Vanguard", "Shadow Blades", "Stormriders", "Sun Spear Company"
        if (_rng.NextDouble() < 0.35)
        {
            // animal/title pairing
            return (_rng.NextDouble() < ArticleChance ? "The " : string.Empty) +
                   $"{Pick(t.Animals)} {Pick(t.NounsPlural)}";
        }

        if (_rng.NextDouble() < 0.5)
            return (_rng.NextDouble() < ArticleChance ? "The " : string.Empty) +
                   $"{Pick(t.Adjectives)} {Pick(t.NounsPlural)}";

        // adjective + specific title
        return $"{Pick(t.Adjectives)} {Pick(t.Titles)}";
    }

    private string NavalGroup(ThemeParts t)
    {
        // e.g., "3rd Fleet", "Corsair Squadron", "Azure Mariners"
        string maybeNum = _rng.NextDouble() < NumberingChance ? MakeNumberToken() + " " : string.Empty;
        if (_rng.NextDouble() < 0.5)
            return maybeNum + Pick(t.NavalFormations); // Fleet, Armada, Squadron

        return $"{Pick(t.Adjectives)} {Pick(t.NavalCrewsPlural)}";
    }

    private string ShipName(ThemeParts t)
    {
        // e.g., "HMS Dawnfire", "HMS Wavecutter", "HMS Resolute"
        string prefix = string.IsNullOrWhiteSpace(NavalShipPrefix) ? string.Empty : NavalShipPrefix.Trim() + " ";
        string ship = _rng.NextDouble() < 0.5
            ? Pick(t.ShipProperNames)
            : $"{Pick(t.ShipWordLeft)}{Pick(t.ShipWordRight)}"; // Wave + cutter = "Wavecutter"
        return prefix + ship;
    }

    private string AirNumbered(ThemeParts t)
    {
        // e.g., "VII Wing", "21st Squadron"
        string num = MakeNumberToken();
        string form = Pick(t.AirFormations);
        return $"{num} {form}";
    }

    private string AirGroup(ThemeParts t)
    {
        // e.g., "Stormwing Squadron", "Thunderhawks", "Skyguard"
        if (_rng.NextDouble() < 0.5)
            return $"{Pick(t.AirCompoundLeft)}{Pick(t.AirCompoundRight)}";
        return $"{Pick(t.Adjectives)} {Pick(t.AirUnitsPlural)}";
    }

    private string OrderOf(ThemeParts t)
    {
        // e.g., "Order of the Silver Flame", "Order of the Ember Guard"
        return $"Order of the {Pick(t.Adjectives)} {Pick(t.FantasyObjects)}";
    }

    private string MakeNumberToken()
    {
        int n = _rng.Next(1, 100); // 1..99
        if (UseRomanNumerals) return ToRoman(n);
        return ToOrdinal(n);
    }

    // ===== Utils =====

    private static string CleanupSpaces(string s)
        => string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool LooksNice(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 40) return false;
        if (trimmed.Contains("  ")) return false;
        return true;
    }

    private static string ToTitleCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 1) { parts[i] = char.ToUpperInvariant(p[0]).ToString(); continue; }
            parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
        }
        return string.Join(' ', parts);
    }

    private static string ToOrdinal(int n)
    {
        int mod100 = n % 100;
        string suffix = (mod100 is >= 11 and <= 13) ? "th" :
            (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    private static string ToRoman(int number)
    {
        // 1..3999
        if (number <= 0 || number >= 4000) return number.ToString();
        var map = new (int val, string sym)[]
        {
            (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
            (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
            (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
        };
        var sb = new StringBuilder();
        int n = number;
        foreach (var (v, s) in map)
            while (n >= v) { sb.Append(s); n -= v; }
        return sb.ToString();
    }

    private static T Pick<T>(IReadOnlyList<T> list)
    {
        if (list.Count == 0) throw new InvalidOperationException("Empty list.");
        return list[_sharedRng.Next(list.Count)];
    }

    private static readonly Random _sharedRng = new();

    private ThemeParts GetTheme(UnitNameTheme theme) => theme switch
    {
        UnitNameTheme.Land => Land,
        UnitNameTheme.Naval => Naval,
        UnitNameTheme.Air => Air,
        UnitNameTheme.Fantasy => Fantasy,
        _ => Generic
    };

    // ===== Data =====

    private readonly record struct ThemeParts(
        string[] Adjectives,
        string[] Animals,
        string[] NounsPlural,
        string[] Titles,

        // Naval
        string[] NavalFormations,
        string[] NavalCrewsPlural,
        string[] ShipProperNames,
        string[] ShipWordLeft,
        string[] ShipWordRight,

        // Air
        string[] AirFormations,
        string[] AirUnitsPlural,
        string[] AirCompoundLeft,
        string[] AirCompoundRight,

        // Fantasy
        string[] FantasyObjects
    );

    private static readonly ThemeParts Generic = new(
        Adjectives: new[] { "Iron", "Crimson", "Golden", "Silver", "Jade", "Obsidian", "Azure", "Scarlet", "Ivory", "Emerald", "Onyx", "Storm", "Shadow", "Frost", "Ember", "Thunder", "Radiant", "Valiant" },
        Animals: new[] { "Wolves", "Lions", "Ravens", "Eagles", "Hawks", "Bears", "Tigers", "Dragons", "Stallions", "Cobras", "Boars", "Kraken" },
        NounsPlural: new[] { "Guard", "Lancers", "Vanguard", "Spears", "Blades", "Sentinels", "Rangers", "Wardens", "Phalanx", "Legionnaires", "Skirmishers", "Marauders" },
        Titles: new[] { "Company", "Cohort", "Regiment", "Brigade", "Legion", "Phalanx", "Battalion", "Detachment", "Guard", "Scouts", "Dragoons", "Musketeers", "Infantry", "Cavalry", "Pikemen", "Archers" },

        NavalFormations: new[] { "Fleet", "Armada", "Squadron", "Flotilla", "Corsair Band" },
        NavalCrewsPlural: new[] { "Mariners", "Corsairs", "Privateers", "Sea Guard", "Buccaneers" },
        ShipProperNames: new[] { "Valiant", "Resolute", "Dawnfire", "Wavecutter", "Starfall", "Stormcaller", "Nightwind", "Sunspire", "Ironclad", "Skybreaker", "Tempest", "Radiance", "Seaborn", "Aegis" },
        ShipWordLeft: new[] { "Sea", "Wave", "Star", "Storm", "Sun", "Moon", "Sky", "Wind", "Iron", "Sword", "Dawn", "Night" },
        ShipWordRight: new[] { "cutter", "runner", "breaker", "fire", "chaser", "song", "blade", "spire", "bloom", "drift" },

        AirFormations: new[] { "Wing", "Squadron", "Flight" },
        AirUnitsPlural: new[] { "Hawks", "Riders", "Skyguard", "Stormwings", "Thunderhawks" },
        AirCompoundLeft: new[] { "Storm", "Thunder", "Sky", "Wind", "Sun", "Moon", "Star", "Cloud" },
        AirCompoundRight: new[] { "wing", "talon", "riders", "hawks", "lancers", "guard" },

        FantasyObjects: new[] { "Flame", "Crescent", "Oath", "Phoenix", "Anvil", "Sigil", "Verdant Crown", "Eclipse", "Gleam", "Shard", "Aegis" }
    );

    private static readonly ThemeParts Land = Generic with
    {
        Titles = new[] { "Company", "Cohort", "Regiment", "Brigade", "Legion", "Phalanx", "Battalion", "Guard", "Rangers", "Dragoons", "Sappers", "Pioneers", "Halberdiers", "Hoplites" },
        NounsPlural = new[] { "Guard", "Lancers", "Vanguard", "Spears", "Blades", "Sentinels", "Wardens", "Phalanx", "Legionnaires", "Pikemen", "Arquebusiers", "Skirmishers" }
    };

    private static readonly ThemeParts Naval = Generic with
    {
        NavalFormations = new[] { "Fleet", "Armada", "Squadron", "Flotilla" },
        NavalCrewsPlural = new[] { "Mariners", "Corsairs", "Privateers", "Sea Guard" },
        ShipProperNames = new[] { "Valiant", "Resolute", "Dawnfire", "Wavecutter", "Starfall", "Stormcaller", "Nightwind", "Sunspire", "Ironclad", "Tempest", "Trident", "Leviathan", "Sea Drake" }
    };

    private static readonly ThemeParts Air = Generic with
    {
        AirFormations = new[] { "Wing", "Squadron", "Flight" },
        AirUnitsPlural = new[] { "Hawks", "Riders", "Skyguard", "Stormwings", "Thunderhawks", "Falcons" },
        AirCompoundLeft = new[] { "Storm", "Thunder", "Sky", "Wind", "Star", "Cloud" },
        AirCompoundRight = new[] { "wing", "talons", "riders", "hawks", "guard", "lancers" }
    };

    private static readonly ThemeParts Fantasy = Generic with
    {
        Adjectives = new[] { "Silver", "Golden", "Verdant", "Umbral", "Runed", "Elder", "Arcane", "Sacred", "Abyssal", "Solar", "Lunar", "Ember", "Storm", "Jade", "Onyx" },
        NounsPlural = new[] { "Wardens", "Blades", "Sentinels", "Keepers", "Magi", "Runeguard", "Spellbinders", "Paladins", "Justicars" },
        FantasyObjects = new[] { "Silver Flame", "Emerald Star", "Sun Spear", "Moonstone", "Runebrand", "Aegis", "Crystal Rose", "Obsidian Crown", "Radiant Oath" }
    };
}
