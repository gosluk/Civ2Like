using System;
using System.Collections.Immutable;

namespace Civ2Like
{
    public readonly struct Hex : IEquatable<Hex>
    {
        public int Q { get; }
        public int R { get; }

        public Hex(int q, int r) { Q = q; R = r; }

        public int S => -Q - R;

        public static readonly Hex[] NeighborDirs =
        [
            new(+1,0), new(+1,-1), new(0,-1), new(-1,0), new(-1,+1), new(0,+1)
        ];

        public Hex Add(Hex b) => new(Q + b.Q, R + b.R);

        public static int Distance(Hex a, Hex b)
        {
            var aq = a.Q; var ar = a.R; var @as = -a.Q - a.R;
            var bq = b.Q; var br = b.R; var @bs = -b.Q - b.R;
            return (Math.Abs(aq - bq) + Math.Abs(ar - br) + Math.Abs(@as - @bs)) / 2;
        }

        public int Distance(Hex other) => Distance(this, other);

        public bool Equals(Hex other) => Q == other.Q && R == other.R;
        public override bool Equals(object? obj) => obj is Hex h && Equals(h);
        public override int GetHashCode() => HashCode.Combine(Q, R);

        public static bool operator ==(Hex h0, Hex h1) => h0.Equals(h1);
        public static bool operator !=(Hex h0, Hex h1) => !h0.Equals(h1);
    }
}
