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

    /// <summary>Generates n names.</summary>
    public IEnumerable<string> NextMany(int count)
    {
        for (int i = 0; i < count; i++) yield return Next();
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

                // Pronounceability + uniqueness checks
                if (!LooksNice(name)) continue;
                if (EnsureUnique && _used.Contains(name)) continue;

                _used.Add(name);
                return name;
            }

            // Fallback: try appending a suffix number to force uniqueness
            best = MakeSingleWord();
            best = ToTitleCase(best);
            if (EnsureUnique)
            {
                var baseName = best;
                int i = 2;
                while (_used.Contains(best) && i < 1000)
                    best = $"{baseName} {i++}";
                _used.Add(best);
            }
            return best;
        }
    }

    // ===== Implementation =====

    private string MakeSingleWord()
    {
        var t = GetTheme(Theme);

        // Choose a structure like: Core+Suffix, Prefix+Core+Suffix, Core only
        int roll = _rng.Next(100);
        if (roll < 15 && t.Prefixes.Length > 0 && t.Suffixes.Length > 0)
            return SmoothJoin(SmoothJoin(Pick(t.Prefixes), MakeCore(t)), Pick(t.Suffixes));
        if (roll < 55 && t.Suffixes.Length > 0)
            return SmoothJoin(MakeCore(t), Pick(t.Suffixes));
        if (roll < 75 && t.Prefixes.Length > 0)
            return SmoothJoin(Pick(t.Prefixes), MakeCore(t));
        return MakeCore(t);
    }

    private string MakeTwoWord()
    {
        var t = GetTheme(Theme);

        // a) Adjective + Noun  b) GeoPrefix + Core(+Suffix)  c) Noun + Core
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

        // Fallback
        return MakeSingleWord();
    }

    private string MakeCore(ThemeParts t)
    {
        // 2–3 syllables with bias towards 2
        int syllables = _rng.NextDouble() < 0.65 ? 2 : 3;
        var sb = new StringBuilder(12);

        string prev = string.Empty;
        for (int i = 0; i < syllables; i++)
        {
            var s = Pick(t.Cores);
            // avoid identical consecutive syllables and harsh repeats
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
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        char x = char.ToLowerInvariant(a[^1]);
        char y = char.ToLowerInvariant(b[0]);
        // Avoid triple vowels/consonants across boundary
        return (IsVowel(x) && IsVowel(y)) || (!IsVowel(x) && !IsVowel(y));
    }

    private static string SmoothJoin(string a, string b, bool onlyJoin = false)
    {
        if (string.IsNullOrEmpty(a)) return b ?? string.Empty;
        if (string.IsNullOrEmpty(b)) return a ?? string.Empty;

        char last = char.ToLowerInvariant(a[^1]);
        char first = char.ToLowerInvariant(b[0]);

        // Drop duplicated boundary letter: "Bel" + "lor" => "Bellor"
        if (last == first) return a + b.Substring(1);

        // Small softeners: if vowel-vowel, insert 'r' or 'n' occasionally (but keep simple/deterministic)
        if (IsVowel(last) && IsVowel(first))
            return a + (last is 'a' or 'o' ? "r" : "n") + b;

        return a + b;
    }

    private static bool LooksNice(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        // Avoid awkward sequences: 3 consonants or vowels in a row (with a few allowed clusters)
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

            if (vRun >= 3) return false;
            if (cRun >= 3)
            {
                // Allow some pleasant clusters
                if (!AllowsCluster(lower, i - 2, i))
                    return false;
            }
        }

        // Avoid trailing hyphens/oddities
        if (lower.EndsWith('-') || lower.StartsWith('-')) return false;

        // Avoid names that are too short/long
        int letters = lower.Count(char.IsLetter);
        if (letters < 3 || letters > 16) return false;

        return true;
    }

    private static bool AllowsCluster(string s, int start, int endInclusive)
    {
        if (start < 0) return false;
        string cluster = s.Substring(start, endInclusive - start + 1);
        return cluster is "str" or "sch" or "chr" or "phr" or "thr";
    }

    private static bool IsVowel(char c) => "aeiouy".IndexOf(char.ToLowerInvariant(c)) >= 0;

    private static string ToTitleCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;
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
        if (list.Count == 0) throw new InvalidOperationException("Empty list.");
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
        string[] Prefixes,
        string[] Cores,
        string[] Suffixes,
        string[] Adjectives,
        string[] Nouns
    );

    private static readonly string[] GeoPrefixes = new[]
    {
        "Port", "Fort", "New", "Old", "Lake", "Mount", "Cape", "Bay", "Glen", "North", "South", "East", "West"
    };

    private static readonly ThemeParts Generic = new(
        Prefixes: new[] { "Bel", "Cal", "Dor", "El", "Mar", "Nor", "Val", "Ald", "Riv", "Stone", "High" },
        Cores: new[] {
            "ar","bel","cal","dor","el","fa","gal","hal","ira","jor","kel","lor","mar","nor","or","par","qua","ran","sil","tor","ur","val","wen","yor","zan",
            "an","bar","cor","den","er","fal","gar","hen","ion","jar","kan","lan","mir","nar","or","per","quin","ras","sar","tan","ul","ver","wen","yorn","zen"
        },
        Suffixes: new[] { "ton", "ford", "field", "dale", "gate", "keep", "haven", "holm", "crest", "wick", "borough", "mouth", "port", "bridge", "burg", "grad", "polis", "shire", "vale", "view" },
        Adjectives: new[] { "Silver", "Golden", "Green", "Grand", "Bright", "Windy", "Stone", "High", "Low", "Red", "White", "Black" },
        Nouns: new[] { "Harbor", "Haven", "Grove", "Hill", "Bay", "Falls", "Reach", "Meadow", "Watch", "Keep", "Cross", "Market" }
    );

    private static readonly ThemeParts Nordic = new(
        Prefixes: new[] { "Skj", "As", "Bjorn", "Ul", "Thor", "Fj", "Sve", "Hjal", "Rag", "Sig" },
        Cores: new[] { "sk", "fj", "vald", "heim", "berg", "lund", "thor", "ulv", "sven", "bjorn", "ra", "haug", "vik", "nar", "grim", "jor", "astr", "fre", "sten" },
        Suffixes: new[] { "fjord", "vik", "stad", "holm", "havn", "gard", "heim", "fjord", "by", "borg" },
        Adjectives: new[] { "Frost", "Iron", "Storm", "Snow", "Pine", "Wolf", "Raven" },
        Nouns: new[] { "Fjord", "Fell", "Skerry", "Cliff", "Harbor", "Hall", "Bridge" }
    );

    private static readonly ThemeParts Desert = new(
        Prefixes: new[] { "Al", "Az", "Sar", "Kal", "Mir", "Ras", "Zar", "Qas", "Bad", "Har" },
        Cores: new[] { "qar", "sar", "mir", "kal", "bad", "dun", "hak", "ram", "zan", "far", "naf", "saf", "had", "dar", "mah", "rah", "zar", "qir", "bah" },
        Suffixes: new[] { "abad", "dar", "mir", "pur", "zar", "bad", "rah", "qat", "ruk" },
        Adjectives: new[] { "Golden", "Saffron", "Amber", "Jade", "Sirocco", "Oasis" },
        Nouns: new[] { "Dune", "Oasis", "Bazaar", "Citadel", "Gate", "Palm", "Well" }
    );

    private static readonly ThemeParts Slavic = new(
        Prefixes: new[] { "Nov", "Star", "Vel", "Zag", "Niz", "Slav", "Bor", "Kras", "Mosk", "Vlad" },
        Cores: new[] { "grad", "slav", "bor", "gor", "mir", "pol", "vol", "vad", "ros", "lav", "dan", "nik", "mil" },
        Suffixes: new[] { "ograd", "opol", "ov", "ovo", "ava", "insk", "ets", "any", "ovo", "ino" },
        Adjectives: new[] { "Red", "White", "Green", "New", "Upper", "Lower" },
        Nouns: new[] { "Bridge", "Harbor", "Hill", "Field", "Market" }
    );

    private static readonly ThemeParts EastAsian = new(
        Prefixes: new[] { "Kai", "Shin", "Hana", "Ling", "Yue", "Rin", "Kyo", "Tian", "Sora", "Mei" },
        Cores: new[] { "kai", "shin", "han", "lin", "yue", "rin", "kyo", "tian", "sora", "mei", "kan", "tan", "jin", "li", "su", "to", "ya", "na" },
        Suffixes: new[] { "kyo", "to", "shan", "lin", "jin", "min", "wan" },
        Adjectives: new[] { "Jade", "Spring", "Moon", "River", "Pearl", "Lotus" },
        Nouns: new[] { "Harbor", "Garden", "Bridge", "Gate", "Hill", "Bay" }
    );

    private static readonly ThemeParts Islander = new(
        Prefixes: new[] { "Cor", "Ari", "Bora", "Navi", "Mira", "Zea", "Lumi", "Tahi" },
        Cores: new[] { "cor", "ari", "bora", "navi", "mira", "zea", "lumi", "tahi", "lago", "maki", "nalu", "tiare" },
        Suffixes: new[] { "lua", "nui", "tua", "haka", "laga", "bay", "cay" },
        Adjectives: new[] { "Coral", "Azure", "Sunny", "Palm", "Lagoon" },
        Nouns: new[] { "Cove", "Lagoon", "Key", "Harbor", "Reef", "Shoal" }
    );

    private static readonly ThemeParts Latin = new(
        Prefixes: new[] { "San", "Santa", "Villa", "Porta", "Aqua", "Val", "Monte" },
        Cores: new[] { "aqua", "mar", "val", "terra", "luna", "sol", "flor", "ver", "vent", "port", "cast" },
        Suffixes: new[] { "polis", "grad", "via", "tia", "ia", "ora", "ona" },
        Adjectives: new[] { "Santa", "San", "Nova", "Alta", "Bella" },
        Nouns: new[] { "Vista", "Mar", "Valle", "Puerto", "Monte", "Campo" }
    );
}
