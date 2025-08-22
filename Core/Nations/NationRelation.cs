namespace Civ2Like.Core.Nations;

public sealed class NationRelation : IEquatable<NationRelation>
{
    // All in [0..1]; derived attitude can be computed
    public float Trust { get; set; } = 0.5f;       // long-term reliability

    public float Grievance { get; set; } = 0.0f;   // historical hurts

    public float CulturalAffinity { get; set; } = 0.5f; // language/religion/history

    public required Nation NationA { get; init; }

    public required Nation NationB { get; init; }

    public float Attitude =>
        Math.Clamp(0.60f * Trust
                 + 0.25f * CulturalAffinity
                 - 0.50f * Grievance, 0f, 1f);

    // Helper to map CompatibilityScore (−1..+1) into approval [0..1]
    public static class ApprovalMath
    {
        public static float FromCompatibility(int score /* −4..+4 */)
            => Math.Clamp(0.5f + 0.125f * score, 0f, 1f); // linear map
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
