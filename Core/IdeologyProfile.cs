namespace Civ2Like.Core;

// value in [-1, +1] on each axis-pair, where +1 = right endpoint above
public sealed class IdeologyProfile
{
    // Store only the four pairs as a scalar each in [-1, +1]
    public double EgalitarianVsAuthority { get; set; }   // −1 = Egalitarian, +1 = Authoritarian
    public double MaterialistVsSpiritual { get; set; }   // −1 = Materialist, +1 = Spiritualist
    public double PacifistVsMilitarist { get; set; }   // −1 = Pacifist,   +1 = Militarist
    public double XenophileVsXenophobe { get; set; }   // −1 = Xenophile,  +1 = Xenophobe

    public double DistanceTo(IdeologyProfile other)
    {
        // L1 is cheap & gamey; switch to L2 if you prefer smoothness
        return Math.Abs(EgalitarianVsAuthority - other.EgalitarianVsAuthority)
             + Math.Abs(MaterialistVsSpiritual - other.MaterialistVsSpiritual)
             + Math.Abs(PacifistVsMilitarist - other.PacifistVsMilitarist)
             + Math.Abs(XenophileVsXenophobe - other.XenophileVsXenophobe);
    }

    // +1 per axis if same side, −1 if opposite, 0 near center; return in [-1, 1]
    public int CompatibilityScore(IdeologyProfile other, double deadzone = 0.2f)
    {
        int s(double a, double b)
        {
            if (Math.Abs(a) < deadzone || Math.Abs(b) < deadzone)
            {
                return 0;
            }

            return Math.Sign(a) == Math.Sign(b) ? +1 : -1;
        }

        return (s(EgalitarianVsAuthority, other.EgalitarianVsAuthority)
             + s(MaterialistVsSpiritual, other.MaterialistVsSpiritual)
             + s(PacifistVsMilitarist, other.PacifistVsMilitarist)
             + s(XenophileVsXenophobe, other.XenophileVsXenophobe)) / 4;
    }

    public override string ToString()
    {
        return $"EgalitarianVsAuthority: {EgalitarianVsAuthority:0.00}, " +
               $"MaterialistVsSpiritual: {MaterialistVsSpiritual:0.00}, " +
               $"PacifistVsMilitarist: {PacifistVsMilitarist:0.00}, " +
               $"XenophileVsXenophobe: {XenophileVsXenophobe:0.00}";
    }
}

