using System.Collections.Immutable;
using System.Text;

namespace Civ2Like.Core.NameGeneration;

/// <summary>
/// Generates pleasant, varied, fairly pronounceable city names.
/// - Reproducible: optional seed
/// - Unique: avoids repeats (configurable)
/// - Themes: small set of phoneme palettes (Generic, Nordic, Desert, Slavic, EastAsian, Islander, Latin)
/// - Templates: single-word (morphemes) and two-word (adjective+noun, Port/Fort/… + core)
/// </summary>
public sealed class CityNameGenerator
{
    public enum CityNameTheme
    {
        Generic,
        Nordic,
        Desert,
        Slavic,
        EastAsian,
        Islander,
        Latin,
    }

    private readonly Random _rng;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public CityNameTheme Theme { get; set; } = CityNameTheme.Generic;

    /// <summary>When true, generator will avoid returning names that were produced before during this instance's lifetime.</summary>
    public bool EnsureUnique { get; set; } = true;

    /// <summary>Max attempts to find a unique/pronounceable name before returning best-effort.</summary>
    public int MaxAttempts { get; set; } = 64;

    /// <summary>Controls the chance of two-word names (0..1).</summary>
    public double TwoWordChance { get; set; } = 0.28;

    /// <summary>Controls the chance of geographic prefixes like "Port", "Fort", "New" (0..1).</summary>
    public double GeoPrefixChance { get; set; } = 0.22;

    public CityNameGenerator(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>Clears the internal used-name registry.</summary>
    public void ResetUsed()
    {
        lock (_lock)
        {
            _used.Clear();
        }
    }

    /// <summary>Pre-populate the "used" set to avoid duplicates across saves or sessions.</summary>
    public void MarkAsUsed(IEnumerable<string> names)
    {
        if (names == null)
        {
            return;
        }

        lock (_lock)
        {
            foreach (var n in names)
            {
                if (!string.IsNullOrWhiteSpace(n))
                {
                    _used.Add(n.Trim());
                }
            }
        }
    }

    /// <summary>Generates n names.</summary>
    public IEnumerable<string> NextMany(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Next();
        }
    }

    /// <summary>Generate a single city name.</summary>
    public string Next()
    {
        lock (_lock)
        {
            string best = string.Empty;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                string name = _rng.NextDouble() < TwoWordChance
                    ? MakeTwoWord()
                    : MakeSingleWord();

                name = ToTitleCase(name);

                if (!LooksNice(name))
                {
                    continue;
                }

                if (EnsureUnique && _used.Contains(name))
                {
                    continue;
                }

                _used.Add(name);
                return name;
            }

            best = MakeSingleWord();
            best = ToTitleCase(best);
            if (EnsureUnique)
            {
                var baseName = best;
                int i = 2;
                while (_used.Contains(best) && i < 1000)
                {
                    best = $"{baseName} {i++}";
                }

                _used.Add(best);
            }
            return best;
        }
    }

    // ===== Implementation =====

    private string MakeSingleWord()
    {
        var t = GetTheme(Theme);

        int roll = _rng.Next(100);
        if (roll < 15 && t.Prefixes.Length > 0 && t.Suffixes.Length > 0)
        {
            return SmoothJoin(SmoothJoin(Pick(t.Prefixes), MakeCore(t)), Pick(t.Suffixes));
        }

        if (roll < 55 && t.Suffixes.Length > 0)
        {
            return SmoothJoin(MakeCore(t), Pick(t.Suffixes));
        }

        if (roll < 75 && t.Prefixes.Length > 0)
        {
            return SmoothJoin(Pick(t.Prefixes), MakeCore(t));
        }

        return MakeCore(t);
    }

    private string MakeTwoWord()
    {
        var t = GetTheme(Theme);

        int roll = _rng.Next(100);

        if (roll < 45 && t.Adjectives.Length > 0 && t.Nouns.Length > 0)
        {
            var left = Pick(t.Adjectives);
            var right = Pick(t.Nouns);
            return $"{ToTitleCase(left)} {ToTitleCase(right)}";
        }

        if (roll < 45 + (int)(GeoPrefixChance * 100))
        {
            var geo = Pick(GeoPrefixes);
            string core = MakeSingleWord();
            return $"{geo} {ToTitleCase(core)}";
        }

        if (t.Nouns.Length > 0)
        {
            var left = Pick(t.Nouns);
            string core = MakeSingleWord();
            return $"{ToTitleCase(left)} {ToTitleCase(core)}";
        }

        return MakeSingleWord();
    }

    private string MakeCore(ThemeParts t)
    {
        int syllables = _rng.NextDouble() < 0.65 ? 2 : 3;
        var sb = new StringBuilder(12);

        string prev = string.Empty;
        for (int i = 0; i < syllables; i++)
        {
            var s = Pick(t.Cores);
            if (i > 0 && (string.Equals(s, prev, StringComparison.OrdinalIgnoreCase) || BadBoundary(prev, s)))
            {
                s = Pick(t.Cores);
            }
            sb.Append(SmoothJoin(prev, s, onlyJoin: true));
            prev = s;
        }

        return sb.ToString();
    }

    private static bool BadBoundary(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        char x = char.ToLowerInvariant(a[^1]);
        char y = char.ToLowerInvariant(b[0]);
        return (IsVowel(x) && IsVowel(y)) || (!IsVowel(x) && !IsVowel(y));
    }

    private static string SmoothJoin(string a, string b, bool onlyJoin = false)
    {
        if (string.IsNullOrEmpty(a))
        {
            return b ?? string.Empty;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a ?? string.Empty;
        }

        char last = char.ToLowerInvariant(a[^1]);
        char first = char.ToLowerInvariant(b[0]);

        if (last == first)
        {
            return a + b.Substring(1);
        }

        if (IsVowel(last) && IsVowel(first))
        {
            return a + (last is 'a' or 'o' ? "r" : "n") + b;
        }

        return a + b;
    }

    private static bool LooksNice(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lower = name.ToLowerInvariant();

        int vRun = 0, cRun = 0;
        for (int i = 0; i < lower.Length; i++)
        {
            char ch = lower[i];
            if (!char.IsLetter(ch))
            {
                vRun = cRun = 0;
                continue;
            }

            bool v = IsVowel(ch);
            if (v) { vRun++; cRun = 0; }
            else { cRun++; vRun = 0; }

            if (vRun >= 3)
            {
                return false;
            }

            if (cRun >= 3)
            {
                if (!AllowsCluster(lower, i - 2, i))
                {
                    return false;
                }
            }
        }

        if (lower.EndsWith('-') || lower.StartsWith('-'))
        {
            return false;
        }

        int letters = lower.Count(char.IsLetter);
        if (letters < 3 || letters > 16)
        {
            return false;
        }

        return true;
    }

    private static bool AllowsCluster(string s, int start, int endInclusive)
    {
        if (start < 0)
        {
            return false;
        }

        string cluster = s.Substring(start, endInclusive - start + 1);
        return cluster is "str" or "sch" or "chr" or "phr" or "thr";
    }

    private static bool IsVowel(char c) => "aeiouy".IndexOf(char.ToLowerInvariant(c)) >= 0;

    private static string ToTitleCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw ?? string.Empty;
        }

        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 1) { parts[i] = char.ToUpperInvariant(p[0]).ToString(); continue; }
            parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
        }
        return string.Join(' ', parts);
    }

    private static T Pick<T>(IReadOnlyList<T> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Empty list.");
        }
        // System.Random.Shared isn’t available in older TFMs; we use instance rng
        return list[_sharedRng.Next(list.Count)];
    }

    // Shared rng for static picks where instance isn't available yet
    private static readonly Random _sharedRng = new();

    private ThemeParts GetTheme(CityNameTheme theme) => theme switch
    {
        CityNameTheme.Nordic => Nordic,
        CityNameTheme.Desert => Desert,
        CityNameTheme.Slavic => Slavic,
        CityNameTheme.EastAsian => EastAsian,
        CityNameTheme.Islander => Islander,
        CityNameTheme.Latin => Latin,
        _ => Generic
    };

    // ===== Data =====

    private readonly record struct ThemeParts(
        ImmutableArray<string> Prefixes,
        ImmutableArray<string> Cores,
        ImmutableArray<string> Suffixes,
        ImmutableArray<string> Adjectives,
        ImmutableArray<string> Nouns
    );

    private static readonly ImmutableArray<string> GeoPrefixes = ["Port", "Fort", "New", "Old", "Lake", "Mount", "Cape", "Bay", "Glen", "North", "South", "East", "West"];

    private static readonly ThemeParts Generic = new(
        Prefixes: ["Bel", "Cal", "Dor", "El", "Mar", "Nor", "Val", "Ald", "Riv", "Stone", "High"],
        Cores: [
            "ar","bel","cal","dor","el","fa","gal","hal","ira","jor","kel","lor","mar","nor","or","par","qua","ran","sil","tor","ur","val","wen","yor","zan",
            "an","bar","cor","den","er","fal","gar","hen","ion","jar","kan","lan","mir","nar","or","per","quin","ras","sar","tan","ul","ver","wen","yorn","zen"
        ],
        Suffixes: ["ton", "ford", "field", "dale", "gate", "keep", "haven", "holm", "crest", "wick", "borough", "mouth", "port", "bridge", "burg", "grad", "polis", "shire", "vale", "view"],
        Adjectives: ["Silver", "Golden", "Green", "Grand", "Bright", "Windy", "Stone", "High", "Low", "Red", "White", "Black"],
        Nouns: ["Harbor", "Haven", "Grove", "Hill", "Bay", "Falls", "Reach", "Meadow", "Watch", "Keep", "Cross", "Market"]
    );

    private static readonly ThemeParts Nordic = new(
        Prefixes: ["Skj", "As", "Bjorn", "Ul", "Thor", "Fj", "Sve", "Hjal", "Rag", "Sig"],
        Cores: ["sk", "fj", "vald", "heim", "berg", "lund", "thor", "ulv", "sven", "bjorn", "ra", "haug", "vik", "nar", "grim", "jor", "astr", "fre", "sten"],
        Suffixes: ["fjord", "vik", "stad", "holm", "havn", "gard", "heim", "fjord", "by", "borg"],
        Adjectives: ["Frost", "Iron", "Storm", "Snow", "Pine", "Wolf", "Raven"],
        Nouns: ["Fjord", "Fell", "Skerry", "Cliff", "Harbor", "Hall", "Bridge"]
    );

    private static readonly ThemeParts Desert = new(
        Prefixes: ["Al", "Az", "Sar", "Kal", "Mir", "Ras", "Zar", "Qas", "Bad", "Har"],
        Cores: ["qar", "sar", "mir", "kal", "bad", "dun", "hak", "ram", "zan", "far", "naf", "saf", "had", "dar", "mah", "rah", "zar", "qir", "bah"],
        Suffixes: ["abad", "dar", "mir", "pur", "zar", "bad", "rah", "qat", "ruk"],
        Adjectives: ["Golden", "Saffron", "Amber", "Jade", "Sirocco", "Oasis"],
        Nouns: ["Dune", "Oasis", "Bazaar", "Citadel", "Gate", "Palm", "Well"]
    );

    private static readonly ThemeParts Slavic = new(
        Prefixes: ["Nov", "Star", "Vel", "Zag", "Niz", "Slav", "Bor", "Kras", "Mosk", "Vlad"],
        Cores: ["grad", "slav", "bor", "gor", "mir", "pol", "vol", "vad", "ros", "lav", "dan", "nik", "mil"],
        Suffixes: ["ograd", "opol", "ov", "ovo", "ava", "insk", "ets", "any", "ovo", "ino"],
        Adjectives: ["Red", "White", "Green", "New", "Upper", "Lower"],
        Nouns: ["Bridge", "Harbor", "Hill", "Field", "Market"]
    );

    private static readonly ThemeParts EastAsian = new(
        Prefixes: ["Kai", "Shin", "Hana", "Ling", "Yue", "Rin", "Kyo", "Tian", "Sora", "Mei"],
        Cores: ["kai", "shin", "han", "lin", "yue", "rin", "kyo", "tian", "sora", "mei", "kan", "tan", "jin", "li", "su", "to", "ya", "na"],
        Suffixes: ["kyo", "to", "shan", "lin", "jin", "min", "wan"],
        Adjectives: ["Jade", "Spring", "Moon", "River", "Pearl", "Lotus"],
        Nouns: ["Harbor", "Garden", "Bridge", "Gate", "Hill", "Bay"]
    );

    private static readonly ThemeParts Islander = new(
        Prefixes: ["Cor", "Ari", "Bora", "Navi", "Mira", "Zea", "Lumi", "Tahi"],
        Cores: ["cor", "ari", "bora", "navi", "mira", "zea", "lumi", "tahi", "lago", "maki", "nalu", "tiare"],
        Suffixes: ["lua", "nui", "tua", "haka", "laga", "bay", "cay"],
        Adjectives: ["Coral", "Azure", "Sunny", "Palm", "Lagoon"],
        Nouns: ["Cove", "Lagoon", "Key", "Harbor", "Reef", "Shoal"]
    );

    private static readonly ThemeParts Latin = new(
        Prefixes: ["San", "Santa", "Villa", "Porta", "Aqua", "Val", "Monte"],
        Cores: ["aqua", "mar", "val", "terra", "luna", "sol", "flor", "ver", "vent", "port", "cast"],
        Suffixes: ["polis", "grad", "via", "tia", "ia", "ora", "ona"],
        Adjectives: ["Santa", "San", "Nova", "Alta", "Bella"],
        Nouns: ["Vista", "Mar", "Valle", "Puerto", "Monte", "Campo"]
    );
}
