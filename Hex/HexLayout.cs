using System;

namespace Civ2Like
{
    public static class HexLayout
    {
        private static readonly double SQRT3 = Math.Sqrt(3);

        public static (double x, double y) HexToPixel(Hex h, double size)
        {
            double x = size * (SQRT3 * h.Q + SQRT3 / 2 * h.R);
            double y = size * (3.0 / 2.0 * h.R);
            return (x, y);
        }

        public static Hex PixelToHex(double x, double y, double size)
        {
            double q = (SQRT3 / 3 * x - 1.0 / 3.0 * y) / size;
            double r = (2.0 / 3.0 * y) / size;
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

        public static (double x, double y) CornerOffset(int i, double size)
        {
            double angle = Math.PI / 180.0 * (60 * i - 30);
            return (size * Math.Cos(angle), size * Math.Sin(angle));
        }
    }
}
