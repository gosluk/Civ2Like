using System.Text;

namespace Civ2Like.Core.NameGeneration
{
    /// <summary>
    /// Generates names for players / countries / empires.
    /// - Reproducible via optional seed
    /// - Avoids repeats (EnsureUnique)
    /// - Themes adjust the stem phonemes a bit
    /// - Government type influences the suffix / pattern
    /// Patterns like:
    ///   "Kingdom of Norwyn", "Xenoria Empire", "The Jade Confederation",
    ///   "United Valoria", "Grand Duchy of Karsin", "Nova Sargath Republic"
    /// </summary>
    public sealed class FactionNameGenerator
    {
        public enum FactionTheme
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

        public enum GovernmentType
        {
            Auto,        // choose a suitable one at random
            Empire,
            Kingdom,
            Republic,
            Federation,
            Confederation,
            Dominion,
            Sultanate,
            Khanate,
            Caliphate,
            Commonwealth,
            Principality,
            Duchy,
            CityState,
            Theocracy,
            Union
        }

        private readonly Random _rng;
        private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public FactionTheme Theme { get; set; } = FactionTheme.Generic;
        public GovernmentType Government { get; set; } = GovernmentType.Auto;

        /// <summary>Ensure unique names across this generator instance.</summary>
        public bool EnsureUnique { get; set; } = true;

        /// <summary>Max attempts for a "nice" + unique output.</summary>
        public int MaxAttempts { get; set; } = 64;

        /// <summary>Chance to prepend geo/power adjectives like "United", "Great", "Grand", "New".</summary>
        public double PowerPrefixChance { get; set; } = 0.35;

        /// <summary>Chance to use the "of" pattern: "Kingdom of X".</summary>
        public double OfPatternChance { get; set; } = 0.55;

        /// <summary>Chance to make a two-word root (e.g., "Nova Sargath").</summary>
        public double TwoWordRootChance { get; set; } = 0.22;

        public FactionNameGenerator(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void ResetUsed() { lock (_lock) _used.Clear(); }

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

        /// <summary>Generate a single faction name.</summary>
        public string Next()
        {
            lock (_lock)
            {
                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    string name = MakeName();
                    name = ToTitleCase(name);

                    if (!LooksNice(name)) continue;
                    if (EnsureUnique && _used.Contains(name)) continue;

                    _used.Add(name);
                    return name;
                }

                // Fallback: force-unique suffix
                var baseName = ToTitleCase(MakeName());
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

        public IEnumerable<string> NextMany(int count)
        {
            for (int i = 0; i < count; i++) yield return Next();
        }

        // ===== Implementation =====

        private string MakeName()
        {
            var t = GetTheme(Theme);

            // Build a "root" to attach a government suffix to.
            string root = _rng.NextDouble() < TwoWordRootChance
                ? $"{MakeRoot(t, allowShort: true)} {MakeRoot(t)}"
                : MakeRoot(t);

            // Optional power/geo prefix like "United", "Great", "Grand", "New", "Free", "Upper", "Lower"
            string prefix = _rng.NextDouble() < PowerPrefixChance ? Pick(PowerPrefixes) + " " : string.Empty;

            // Choose a government label
            var gov = Government == GovernmentType.Auto ? Pick(AutoGovPool) : Government;

            // Two broad patterns:
            //  A) "<Prefix><Root> <GovSuffix>"     e.g., "United Valoria Republic", "Great Norwyn Empire"
            //  B) "<GovSuffix> of <Prefix><Root>"  e.g., "Kingdom of New Karsin"
            bool useOfPattern = _rng.NextDouble() < OfPatternChance;

            if (useOfPattern)
            {
                return $"{GovernmentToTitle(gov)} of {prefix}{root}";
            }
            else
            {
                // Some governments sound better without trailing "The" etc.
                string suffix = GovernmentToSuffix(gov);
                if (string.IsNullOrEmpty(suffix))
                    return $"{prefix}{root}";
                return $"{prefix}{root} {suffix}";
            }
        }

        private string MakeRoot(ThemeParts t, bool allowShort = false)
        {
            // 2–3 syllables, occasionally 1 if allowShort
            int syllables;
            double roll = _rng.NextDouble();
            if (allowShort && roll < 0.15) syllables = 1;
            else syllables = roll < 0.70 ? 2 : 3;

            var sb = new StringBuilder(16);
            string prev = string.Empty;

            // Rough phonotactics by stitching morphemes and smoothing boundaries
            for (int i = 0; i < syllables; i++)
            {
                string chunk = Pick(t.Cores);
                if (i == 0 && t.Prefixes.Length > 0 && _rng.NextDouble() < 0.35)
                    chunk = SmoothJoin(Pick(t.Prefixes), chunk, onlyJoin: true);

                if (i == syllables - 1 && t.Suffixes.Length > 0 && _rng.NextDouble() < 0.55)
                    chunk = SmoothJoin(chunk, Pick(t.Suffixes), onlyJoin: true);

                sb.Append(SmoothJoin(prev, chunk, onlyJoin: true));
                prev = chunk;
            }

            var raw = sb.ToString();
            // Sometimes add a clear lexical ending (like -ia, -or, -an) to make it country-ish
            if (_rng.NextDouble() < 0.35)
                raw = SmoothJoin(raw, Pick(GenericEndings), onlyJoin: true);

            return raw;
        }

        private static string GovernmentToTitle(GovernmentType g) => g switch
        {
            GovernmentType.Empire => "Empire",
            GovernmentType.Kingdom => "Kingdom",
            GovernmentType.Republic => "Republic",
            GovernmentType.Federation => "Federation",
            GovernmentType.Confederation => "Confederation",
            GovernmentType.Dominion => "Dominion",
            GovernmentType.Sultanate => "Sultanate",
            GovernmentType.Khanate => "Khanate",
            GovernmentType.Caliphate => "Caliphate",
            GovernmentType.Commonwealth => "Commonwealth",
            GovernmentType.Principality => "Principality",
            GovernmentType.Duchy => "Duchy",
            GovernmentType.CityState => "City-State",
            GovernmentType.Theocracy => "Theocracy",
            GovernmentType.Union => "Union",
            _ => "State"
        };

        private static string GovernmentToSuffix(GovernmentType g) => g switch
        {
            GovernmentType.Empire => "Empire",
            GovernmentType.Kingdom => "Kingdom",
            GovernmentType.Republic => "Republic",
            GovernmentType.Federation => "Federation",
            GovernmentType.Confederation => "Confederation",
            GovernmentType.Dominion => "Dominion",
            GovernmentType.Sultanate => "Sultanate",
            GovernmentType.Khanate => "Khanate",
            GovernmentType.Caliphate => "Caliphate",
            GovernmentType.Commonwealth => "Commonwealth",
            GovernmentType.Principality => "Principality",
            GovernmentType.Duchy => "Duchy",
            GovernmentType.CityState => "City-State",
            GovernmentType.Theocracy => "Theocracy",
            GovernmentType.Union => "Union",
            _ => string.Empty
        };

        // ===== Data =====

        private sealed class ThemeParts
        {
            public string[] Prefixes { get; init; } = Array.Empty<string>();
            public string[] Cores { get; init; } = Array.Empty<string>();
            public string[] Suffixes { get; init; } = Array.Empty<string>();
        }

        private static readonly string[] PowerPrefixes =
        {
            "United", "Great", "Grand", "New", "Free", "Upper", "Lower", "Greater", "Nova"
        };

        private static readonly string[] GenericEndings =
        {
            "ia","a","ar","or","on","en","an","um","ara","ora","eria","ora","ara","enia","arae"
        };

        private static readonly GovernmentType[] AutoGovPool =
        {
            GovernmentType.Empire, GovernmentType.Kingdom, GovernmentType.Republic, GovernmentType.Federation,
            GovernmentType.Confederation, GovernmentType.Dominion, GovernmentType.Commonwealth,
            GovernmentType.Principality, GovernmentType.Duchy, GovernmentType.Union
        };

        private ThemeParts GetTheme(FactionTheme theme)
        {
            switch (theme)
            {
                case FactionTheme.Nordic: return Nordic;
                case FactionTheme.Desert: return Desert;
                case FactionTheme.Slavic: return Slavic;
                case FactionTheme.Latin: return Latin;
                case FactionTheme.EastAsian: return EastAsian;
                case FactionTheme.Steppe: return Steppe;
                case FactionTheme.Celtic: return Celtic;
                case FactionTheme.Islander: return Islander;
                case FactionTheme.Fantasy: return Fantasy;
                default: return Generic;
            }
        }

        private static readonly ThemeParts Generic = new ThemeParts
        {
            Prefixes = new[] { "Val", "Nor", "Cal", "Mar", "Bel", "Dor", "Ald", "El", "Gar", "Kar", "Lor" },
            Cores = new[] {
                "val","nor","cal","dor","el","mar","ver","sil","tor","ran","par","kel","mor","hal","zan","quin","lor","gar","lin","ria","vyr"
            },
            Suffixes = new[] { "eth", "or", "en", "ar", "in", "ion", "or", "oth", "ir", "an", "um", "ia" }
        };

        private static readonly ThemeParts Nordic = new ThemeParts
        {
            Prefixes = new[] { "As", "Bjorn", "Ul", "Thor", "Skj", "Sve", "Rag", "Sig", "Hjal" },
            Cores = new[] { "fjord", "heim", "lund", "vik", "havn", "bjorn", "ulv", "stor", "grim", "sten", "vald", "haug", "nor", "skar" },
            Suffixes = new[] { "gard", "heim", "vik", "fjord", "by" }
        };

        private static readonly ThemeParts Desert = new ThemeParts
        {
            Prefixes = new[] { "Al", "Az", "Sar", "Kal", "Mir", "Ras", "Zar", "Qas", "Har" },
            Cores = new[] { "sah", "qar", "dar", "mir", "kal", "bad", "ram", "zan", "far", "naf", "had", "zar", "qir", "bah" },
            Suffixes = new[] { "abad", "mir", "dar", "pur", "zar", "rah" }
        };

        private static readonly ThemeParts Slavic = new ThemeParts
        {
            Prefixes = new[] { "Nov", "Star", "Vel", "Zag", "Niz", "Slav", "Bor", "Kras", "Mosk", "Vlad" },
            Cores = new[] { "grad", "slav", "bor", "gor", "mir", "pol", "vol", "ros", "lav", "dan", "nik", "mil" },
            Suffixes = new[] { "ograd", "opol", "ov", "ovo", "ava", "insk", "any", "ino" }
        };

        private static readonly ThemeParts Latin = new ThemeParts
        {
            Prefixes = new[] { "San", "Santa", "Aqua", "Val", "Porta", "Monte", "Nova" },
            Cores = new[] { "aqua", "mar", "val", "terra", "luna", "sol", "flor", "ver", "vent", "port", "cast" },
            Suffixes = new[] { "ia", "ium", "ana", "ora", "ona", "tia", "polis" }
        };

        private static readonly ThemeParts EastAsian = new ThemeParts
        {
            Prefixes = new[] { "Kai", "Shin", "Hana", "Ling", "Yue", "Rin", "Kyo", "Tian", "Sora", "Mei" },
            Cores = new[] { "kai", "shin", "han", "lin", "yue", "rin", "kyo", "tian", "sora", "mei", "kan", "tan", "jin", "li", "su", "to", "ya", "na" },
            Suffixes = new[] { "kyo", "to", "shan", "lin", "jin", "min", "wan" }
        };

        private static readonly ThemeParts Steppe = new ThemeParts
        {
            Prefixes = new[] { "Alt", "Or", "Nog", "Tim", "Kip", "Sarm", "Turg", "Kaz", "Khaz" },
            Cores = new[] { "alt", "orda", "nog", "kara", "tura", "aral", "sari", "saka", "sary", "ural", "khan", "karak" },
            Suffixes = new[] { "ord", "stan", "gar", "khan", "dar", "ar" }
        };

        private static readonly ThemeParts Celtic = new ThemeParts
        {
            Prefixes = new[] { "Bryn", "Dun", "Kil", "Glen", "Ail", "Bal", "Inver", "Aber" },
            Cores = new[] { "bryn", "dun", "kil", "glen", "loch", "cairn", "ard", "aval", "muir", "mor", "tor" },
            Suffixes = new[] { "more", "mere", "loch", "shire", "ness", "ford" }
        };

        private static readonly ThemeParts Islander = new ThemeParts
        {
            Prefixes = new[] { "Cor", "Ari", "Bora", "Navi", "Mira", "Zea", "Lumi", "Tahi" },
            Cores = new[] { "cora", "ari", "bora", "navi", "mira", "zea", "lumi", "tahi", "lago", "nalu", "tiare" },
            Suffixes = new[] { "lua", "nui", "tua", "haka", "laga", "cay", "atol" }
        };

        private static readonly ThemeParts Fantasy = new ThemeParts
        {
            Prefixes = new[] { "Aether", "Eld", "Umber", "Myth", "Star", "Sun", "Moon", "Obsid", "Run" },
            Cores = new[] { "aeth", "eld", "umbr", "myth", "star", "sun", "moon", "obsid", "run", "drak", "valar", "seraph", "arc" },
            Suffixes = new[] { "ion", "ara", "orium", "heim", "hold", "spire", "ia", "oth" }
        };

        // ===== Small phonotactic helpers =====

        private static string SmoothJoin(string a, string b, bool onlyJoin = false)
        {
            if (string.IsNullOrEmpty(a)) return b ?? string.Empty;
            if (string.IsNullOrEmpty(b)) return a ?? string.Empty;

            char last = char.ToLowerInvariant(a[^1]);
            char first = char.ToLowerInvariant(b[0]);

            if (last == first) return a + b.Substring(1); // collapse double boundary

            bool lv = IsVowel(last);
            bool fv = IsVowel(first);
            if (lv && fv)
                return a + (last is 'a' or 'o' ? "r" : "n") + b; // light linker

            // avoid triple consonant clusters
            if (!lv && !fv) return a + "a" + b;

            return a + b;
        }

        private static bool IsVowel(char c) => "aeiouy".IndexOf(char.ToLowerInvariant(c)) >= 0;

        private static bool LooksNice(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim();
            if (t.Length < 3 || t.Length > 40) return false;

            // avoid 3+ vowels/consonants in a row (roughly)
            int vRun = 0, cRun = 0;
            foreach (var ch in t.ToLowerInvariant())
            {
                if (!char.IsLetter(ch)) { vRun = cRun = 0; continue; }
                if (IsVowel(ch)) { vRun++; cRun = 0; if (vRun >= 3) return false; }
                else { cRun++; vRun = 0; if (cRun >= 4) return false; }
            }

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
                parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
            }
            return string.Join(' ', parts);
        }

        private static T Pick<T>(IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0) throw new InvalidOperationException("Empty list.");
            return list[_sharedRng.Next(list.Count)];
        }

        private static readonly Random _sharedRng = new();
    }
}
