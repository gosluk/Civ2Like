namespace Civ2Like.Core.Nations;

public sealed class NationRelation : IEquatable<NationRelation>
{
    // All in [0..1]; derived attitude can be computed
    public decimal Trust { get; set; } = 0.5m;       // long-term reliability

    public decimal Grievance { get; set; } = 0.0m;   // historical hurts

    public decimal CulturalAffinity { get; set; } = 0.5m; // language/religion/history

    public required Nation NationA { get; init; }

    public required Nation NationB { get; init; }

    public decimal Attitude =>
        Math.Clamp(0.60m * Trust
                 + 0.25m * CulturalAffinity
                 - 0.50m * Grievance, 0m, 1m);

    // Helper to map CompatibilityScore (−1..+1) into approval [0..1]
    public static class ApprovalMath
    {
        public static decimal FromCompatibility(decimal score /* −4..+4 */)
            => Math.Clamp(0.5m + 0.125m * score, 0m, 1m); // linear map
    }

    public bool Equals(NationRelation? other)
    {
        if (other is NationRelation nr)
        {
            return (NationA == nr.NationA && NationB == nr.NationB)
                || (NationA == nr.NationB && NationB == nr.NationA);
        }

        return false;
    }

    public override bool Equals(object? obj) => Equals(obj as NationRelation);

    public override int GetHashCode()
    {
        int hashA = NationA.GetHashCode();
        int hashB = NationB.GetHashCode();
        return HashCode.Combine(Math.Min(hashA, hashB), Math.Max(hashA, hashB));
    }
}
