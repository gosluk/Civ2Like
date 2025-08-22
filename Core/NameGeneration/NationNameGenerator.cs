using System.Collections.Immutable;
using System.Text;

namespace Civ2Like.Core.NameGeneration;

/// <summary>
/// Generates nation/country names (e.g., "Valoria", "New Norland", "Azmar", "Upper Karsenia")
/// and can derive a simple demonym ("Valorian", "Norlander", "Azmari", "Karsenian").
///
//// - Reproducible: optional seed
/// - Unique outputs (EnsureUnique)
/// - Themed phoneme palettes
/// - Lightweight phonotactics for pleasant results
/// </summary>
public sealed class NationNameGenerator
{
    public enum NationTheme
    {
        Generic,
        Nordic,
        Desert,
        Slavic,
        Latin,
        EastAsian,
        Steppe,
        Celtic,
        Islander,
        Fantasy
    }

    private readonly Random _rng;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public NationTheme Theme { get; set; } = NationTheme.Generic;

    /// <summary>Guarantee unique names across this generator instance.</summary>
    public bool EnsureUnique { get; set; } = true;

    /// <summary>Max attempts to find a nice & unique name before falling back.</summary>
    public int MaxAttempts { get; set; } = 64;

    /// <summary>Chance (0..1) to produce a two-word root (e.g., "New Valoria", "Nova Sargath").</summary>
    public double TwoWordChance { get; set; } = 0.22;

    /// <summary>Chance (0..1) to prepend a geo/power prefix like "New", "United", "Grand".</summary>
    public double GeoPrefixChance { get; set; } = 0.30;

    public NationNameGenerator(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>Reset the uniqueness set.</summary>
    public void ResetUsed()
    {
        lock (_lock)
        {
            _used.Clear();
        }
    }

    /// <summary>Pre-seed with names from a savegame to avoid duplicates.</summary>
    public void MarkAsUsed(IEnumerable<string> names)
    {
        lock (_lock)
        {
            foreach (string n in names)
            {
                if (!string.IsNullOrWhiteSpace(n))
                {
                    _used.Add(n.Trim());
                }
            }
        }
    }

    /// <summary>Generate one nation name.</summary>
    public string Next()
    {
        lock (_lock)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                string name = ToTitleCase(MakeNation());
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

            // Fallback: force-unique numeric suffix
            string baseName = ToTitleCase(MakeNation());
            if (EnsureUnique)
            {
                string candidate = baseName;
                int i = 2;
                while (_used.Contains(candidate) && i < 1000)
                {
                    candidate = $"{baseName} {i++}";
                }

                _used.Add(candidate);
                return candidate;
            }
            return baseName;
        }
    }

    /// <summary>Generate many nation names.</summary>
    public IEnumerable<string> NextMany(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Next();
        }
    }

    /// <summary>Generate a nation name and a derived demonym (e.g., "Valoria" → "Valorian").</summary>
    public string NextWithDemonym(out string demonym)
    {
        string name = Next();
        demonym = MakeDemonym(name);
        return name;
    }

    // ===== Implementation =====

    private string MakeNation()
    {
        ThemeParts t = GetTheme(Theme);

        string prefix = _rng.NextDouble() < GeoPrefixChance ? Pick(GeoPrefixes) + " " : string.Empty;

        string root = _rng.NextDouble() < TwoWordChance
            ? $"{MakeRoot(t, allowShort: true)} {MakeRoot(t)}"
            : MakeRoot(t);

        return prefix + root;
    }

    private string MakeRoot(ThemeParts t, bool allowShort = false)
    {
        // 2–3 syllables; occasionally 1 if allowed
        double roll = _rng.NextDouble();
        int syllables = (allowShort && roll < 0.15) ? 1 : (roll < 0.70 ? 2 : 3);

        StringBuilder sb = new(16);
        string prev = string.Empty;

        for (int i = 0; i < syllables; i++)
        {
            string chunk = Pick(t.Cores);
            if (i == 0 && t.Prefixes.Length > 0 && _rng.NextDouble() < 0.35)
            {
                chunk = SmoothJoin(Pick(t.Prefixes), chunk, onlyJoin: true);
            }

            if (i == syllables - 1 && t.Suffixes.Length > 0 && _rng.NextDouble() < 0.55)
            {
                chunk = SmoothJoin(chunk, Pick(t.Suffixes), onlyJoin: true);
            }

            sb.Append(SmoothJoin(prev, chunk, onlyJoin: true));
            prev = chunk;
        }

        // Sometimes add a country-ish ending to strengthen the vibe
        if (_rng.NextDouble() < 0.38)
        {
            return SmoothJoin(sb.ToString(), Pick(CountryEndings), onlyJoin: true);
        }

        return sb.ToString();
    }

    // ===== Demonym rules (simple & gamey) =====

    public string MakeDemonym(string nationName)
    {
        if (string.IsNullOrWhiteSpace(nationName))
        {
            return string.Empty;
        }

        string n = nationName.Trim();

        string lower = n.ToLowerInvariant();

        // Specific suffixes
        if (lower.EndsWith("land"))
        {
            return ToTitleCase(n + "er");     // Norland -> Norlander
        }

        if (lower.EndsWith("lands"))
        {
            return ToTitleCase(n + "er");
        }

        if (lower.EndsWith("stan"))
        {
            return ToTitleCase(n + "i");      // -stan -> -stani
        }

        if (lower.EndsWith("ia"))
        {
            return ToTitleCase(n.Substring(0, n.Length - 2) + "ian"); // -ia -> -ian
        }

        if (lower.EndsWith("ium"))
        {
            return ToTitleCase(n.Substring(0, n.Length - 3) + "ian");
        }

        if (lower.EndsWith("a"))
        {
            return ToTitleCase(n + "n");      // -a -> -an
        }

        if (lower.EndsWith("us"))
        {
            return ToTitleCase(n.Substring(0, n.Length - 2) + "an");
        }

        if (lower.EndsWith("i"))
        {
            return ToTitleCase(n + "te");     // -i -> -ite
        }

        if (lower.EndsWith("e"))
        {
            return ToTitleCase(n + "an");     // -e -> -ean/-an (keep simple)
        }

        if (lower.EndsWith("y") && n.Length > 1 && !IsVowel(n[^2]))
        {
            return ToTitleCase(n.Substring(0, n.Length - 1) + "ian");    // -y (consonant+y) -> -ian
        }

        // Default
        return ToTitleCase(n + "ian");
    }

    // ===== Data =====

    private sealed class ThemeParts
    {
        public ImmutableArray<string> Prefixes { get; init; } = ImmutableArray<string>.Empty;
        public ImmutableArray<string> Cores { get; init; } = ImmutableArray<string>.Empty;
        public ImmutableArray<string> Suffixes { get; init; } = ImmutableArray<string>.Empty;
    }

    private ThemeParts GetTheme(NationTheme theme)
    {
        return theme switch
        {
            NationTheme.Nordic => Nordic,
            NationTheme.Desert => Desert,
            NationTheme.Slavic => Slavic,
            NationTheme.Latin => Latin,
            NationTheme.EastAsian => EastAsian,
            NationTheme.Steppe => Steppe,
            NationTheme.Celtic => Celtic,
            NationTheme.Islander => Islander,
            NationTheme.Fantasy => Fantasy,
            _ => Generic,
        };
    }

    private static readonly ImmutableArray<string> GeoPrefixes = ["New", "United", "Great", "Grand", "Upper", "Lower", "Free", "Greater", "Nova"];

    private static readonly ImmutableArray<string> CountryEndings = ["ia", "a", "ar", "or", "on", "en", "an", "um", "eria", "ora", "ana", "enia", "ara"];

    // Themes

    private static readonly ThemeParts Generic = new()
    {
        Prefixes = ["Val", "Nor", "Cal", "Mar", "Bel", "Dor", "Ald", "El", "Gar", "Kar", "Lor"],
        Cores = ["val", "nor", "cal", "dor", "el", "mar", "ver", "sil", "tor", "ran", "par", "kel", "mor", "hal", "zan", "quin", "lor", "gar", "lin", "ria", "vyr"
],
        Suffixes = ["eth", "or", "en", "ar", "in", "ion", "oth", "ir", "an", "um"]
    };

    private static readonly ThemeParts Nordic = new()
    {
        Prefixes = ["As", "Bjorn", "Ul", "Thor", "Skj", "Sve", "Rag", "Sig", "Hjal"],
        Cores = ["fjord", "heim", "lund", "vik", "havn", "bjorn", "ulv", "grim", "sten", "vald", "haug", "nor", "skar"],
        Suffixes = ["gard", "heim", "vik", "fjord", "by", "land"]
    };

    private static readonly ThemeParts Desert = new()
    {
        Prefixes = ["Al", "Az", "Sar", "Kal", "Mir", "Ras", "Zar", "Qas", "Har"],
        Cores = ["sah", "qar", "dar", "mir", "kal", "bad", "ram", "zan", "far", "naf", "had", "zar", "qir", "bah"],
        Suffixes = ["abad", "mir", "dar", "pur", "zar", "rah", "stan"]
    };

    private static readonly ThemeParts Slavic = new()
    {
        Prefixes = ["Nov", "Star", "Vel", "Zag", "Niz", "Slav", "Bor", "Kras", "Mosk", "Vlad"],
        Cores = ["grad", "slav", "bor", "gor", "mir", "pol", "vol", "ros", "lav", "dan", "nik", "mil"],
        Suffixes = ["ia", "ava", "ovo", "ino", "any", "insk"]
    };

    private static readonly ThemeParts Latin = new()
    {
        Prefixes = ["San", "Santa", "Aqua", "Val", "Porta", "Monte", "Nova"],
        Cores = ["aqua", "mar", "val", "terra", "luna", "sol", "flor", "ver", "vent", "port", "cast"],
        Suffixes = ["ia", "ium", "ana", "ora", "ona", "tia", "iana"]
    };

    private static readonly ThemeParts EastAsian = new()
    {
        Prefixes = ["Kai", "Shin", "Hana", "Ling", "Yue", "Rin", "Kyo", "Tian", "Sora", "Mei"],
        Cores = ["kai", "shin", "han", "lin", "yue", "rin", "kyo", "tian", "sora", "mei", "kan", "tan", "jin", "li", "su", "to", "ya", "na"],
        Suffixes = ["kyo", "to", "shan", "lin", "jin", "min", "wan"]
    };

    private static readonly ThemeParts Steppe = new()
    {
        Prefixes = ["Alt", "Or", "Nog", "Tim", "Kip", "Sarm", "Turg", "Kaz", "Khaz"],
        Cores = ["alt", "orda", "nog", "kara", "tura", "aral", "sari", "saka", "ural", "khan", "karak"],
        Suffixes = ["ord", "stan", "gar", "dar", "ar"]
    };

    private static readonly ThemeParts Celtic = new()
    {
        Prefixes = ["Bryn", "Dun", "Kil", "Glen", "Ail", "Bal", "Inver", "Aber"],
        Cores = ["bryn", "dun", "kil", "glen", "loch", "cairn", "ard", "aval", "muir", "mor", "tor"],
        Suffixes = ["more", "mere", "loch", "shire", "ness"]
    };

    private static readonly ThemeParts Islander = new()
    {
        Prefixes = ["Cor", "Ari", "Bora", "Navi", "Mira", "Zea", "Lumi", "Tahi"],
        Cores = ["cora", "ari", "bora", "navi", "mira", "zea", "lumi", "tahi", "lago", "nalu", "tiare"],
        Suffixes = ["lua", "nui", "tua", "haka", "laga", "cay"]
    };

    private static readonly ThemeParts Fantasy = new()
    {
        Prefixes = ["Aether", "Eld", "Umber", "Myth", "Star", "Sun", "Moon", "Obsid", "Run"],
        Cores = ["aeth", "eld", "umbr", "myth", "star", "sun", "moon", "obsid", "run", "drak", "valar", "seraph", "arc"],
        Suffixes = ["ion", "ara", "orium", "heim", "hold", "spire", "ia", "oth"]
    };

    // ===== Helpers =====

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
            return a + b.Substring(1); // collapse duplicated boundary
        }

        bool lv = IsVowel(last);
        bool fv = IsVowel(first);

        if (lv && fv)
        {
            return a + (last is 'a' or 'o' ? "r" : "n") + b; // light linker
        }

        if (!lv && !fv)
        {
            return a + "a" + b; // avoid hard consonant clusters
        }

        return a + b;
    }

    private static bool IsVowel(char c) => "aeiouy".IndexOf(char.ToLowerInvariant(c)) >= 0;

    private static bool LooksNice(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string t = s.Trim();
        if (t.Length is < 3 or > 40)
        {
            return false;
        }

        int vRun = 0, cRun = 0;
        foreach (char ch in t.ToLowerInvariant())
        {
            if (!char.IsLetter(ch))
            {
                vRun = cRun = 0;
                continue;
            }
            if (IsVowel(ch))
            {
                vRun++; cRun = 0; if (vRun >= 3)
                {
                    return false;
                }
            }
            else
            {
                cRun++; vRun = 0; if (cRun >= 4)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static string ToTitleCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw ?? string.Empty;
        }

        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string p = parts[i];
            if (p.Length == 1)
            {
                parts[i] = char.ToUpperInvariant(p[0]).ToString();
                continue;
            }
            parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
        }
        return string.Join(' ', parts);
    }

    private static T Pick<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            throw new InvalidOperationException("Empty list.");
        }

        return list[_sharedRng.Next(list.Count)];
    }

    private static readonly Random _sharedRng = new();
}
