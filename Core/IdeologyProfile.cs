namespace Civ2Like.Core;

// value in [-1, +1] on each axis-pair, where +1 = right endpoint above
public sealed class IdeologyProfile
{
    // Store only the four pairs as a scalar each in [-1, +1]
    public float Authority { get; set; }   // −1 = Egalitarian, +1 = Authoritarian
    public float Spiritual { get; set; }   // −1 = Materialist, +1 = Spiritualist
    public float Militarism { get; set; }   // −1 = Pacifist,   +1 = Militarist
    public float Xenology { get; set; }   // −1 = Xenophile,  +1 = Xenophobe

    public float DistanceTo(IdeologyProfile other)
    {
        // L1 is cheap & gamey; switch to L2 if you prefer smoothness
        return Math.Abs(Authority - other.Authority)
             + Math.Abs(Spiritual - other.Spiritual)
             + Math.Abs(Militarism - other.Militarism)
             + Math.Abs(Xenology - other.Xenology);
    }

    // +1 per axis if same side, −1 if opposite, 0 near center; return in [-1, 1]
    public int CompatibilityScore(IdeologyProfile other, float deadzone = 0.2f)
    {
        int s(float a, float b)
        {
            if (Math.Abs(a) < deadzone || Math.Abs(b) < deadzone)
            {
                return 0;
            }

            return Math.Sign(a) == Math.Sign(b) ? +1 : -1;
        }

        return (s(Authority, other.Authority)
             + s(Spiritual, other.Spiritual)
             + s(Militarism, other.Militarism)
             + s(Xenology, other.Xenology)) / 4;
    }
}

