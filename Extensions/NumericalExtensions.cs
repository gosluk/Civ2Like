namespace Civ2Like.Extensions;

public static class NumericalExtensions
{
    public static ulong Sum(this IEnumerable<ulong> source)
    {
        ulong sum = 0;
        foreach (var item in source)
        {
            sum += item;
        }
        return sum;
    }

    public static decimal NextDecimal(this Random rand) => (decimal)rand.NextDouble();
}
