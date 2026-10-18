using System;

namespace InkLine
{
    // 按本机时间算的日期、周期号。每日残局、每日功课、无尽周词缀共用这一份，
    // 云函数按北京时间算同一套，两边对得上才收成绩。
    public static class DayClock
    {
        // 周一起算。2024-01-01 是周一，周号是从那天起过了几个整周。
        static readonly DateTime WeekZero = new DateTime(2024, 1, 1);

        public static int Today => DayKey(DateTime.Now);

        public static int DayKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

        public static int Week => WeekOf(DateTime.Now);

        public static int WeekOf(DateTime d) => (int)Math.Floor((d.Date - WeekZero).TotalDays / 7.0);

        // 离这周结束（下周一零点）还有几天，今天算一天。
        public static int DaysLeftInWeek
        {
            get
            {
                int dow = ((int)DateTime.Now.DayOfWeek + 6) % 7;
                return 7 - dow;
            }
        }

        public static int SecondsToTomorrow
        {
            get
            {
                DateTime now = DateTime.Now;
                return Math.Max(0, (int)(now.Date.AddDays(1) - now).TotalSeconds);
            }
        }

        // 同一个日期每次算出来都一样的打散值，用来给全服挑同一套东西。
        public static int Hash(int key, int salt)
        {
            unchecked
            {
                uint h = (uint)key * 2654435761u ^ (uint)salt * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (int)(h & 0x7fffffff);
            }
        }
    }
}
