namespace Civ2Like.Hexagon;

public static class HexLayout
{
    private static readonly double SQRT3 = Math.Sqrt(3);

    public static (double x, double y) HexToPixel(Hex h, double sizeX, double sizeY)
    {
        double x = sizeX * (SQRT3 * h.Q + SQRT3 / 2 * h.R);
        double y = sizeY * (3.0 / 2.0 * h.R);
        return (x, y);
    }

    public static Hex PixelToHex(double x, double y, double sizeX, double sizeY)
    {
        if (sizeX == 0.0) throw new ArgumentOutOfRangeException(nameof(sizeX));
        if (sizeY == 0.0) throw new ArgumentOutOfRangeException(nameof(sizeY));

        // Normalize pixel coords by axis scales
        double nx = x / sizeX;
        double ny = y / sizeY;

        // Inverse of:
        // x = sizeX * (√3*q + √3/2*r)
        // y = sizeY * (3/2*r)
        double q = SQRT3 / 3.0 * nx - 1.0 / 3.0 * ny;
        double r = 2.0 / 3.0 * ny;

        return CubeRound(q, r);
    }

    private static Hex CubeRound(double q, double r)
    {
        double s = -q - r;
        int rq = (int)Math.Round(q);
        int rr = (int)Math.Round(r);
        int rs = (int)Math.Round(s);

        double qDiff = Math.Abs(rq - q);
        double rDiff = Math.Abs(rr - r);
        double sDiff = Math.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff) rq = -rr - rs;
        else if (rDiff > sDiff) rr = -rq - rs;

        return new Hex(rq, rr);
    }

    public static (double x, double y) CornerOffset(int i, double sizeX, double sizeY)
    {
        double angle = Math.PI / 180.0 * (60 * i - 30);
        return (sizeX * Math.Cos(angle), sizeY * Math.Sin(angle));
    }
}
