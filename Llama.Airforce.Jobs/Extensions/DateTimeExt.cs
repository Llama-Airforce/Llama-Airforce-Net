namespace Llama.Airforce.Jobs.Extensions;

public static class DateTimeExt
{
    public static long ToUnixTimeSeconds(this DateTime x) => ((DateTimeOffset)x).ToUnixTimeSeconds();
    public static DateTime FromUnixTimeSeconds(long x) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(x);
}