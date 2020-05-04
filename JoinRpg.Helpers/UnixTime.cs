using System;

namespace JoinRpg.Helpers
{
    public static class UnixTime
    {
        public static DateTime ToDateTime(long unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);
        }
    }
}
