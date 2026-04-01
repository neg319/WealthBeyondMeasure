using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public static class PaydayUtility
    {
        public const int TicksPerDay = 60000;
        public const int DaysPerWeek = 7;

        private static readonly string[] DayKeys =
        {
            "WBM_Day_Monday",
            "WBM_Day_Tuesday",
            "WBM_Day_Wednesday",
            "WBM_Day_Thursday",
            "WBM_Day_Friday",
            "WBM_Day_Saturday",
            "WBM_Day_Sunday"
        };

        public static List<string> WeekdayLabels
        {
            get
            {
                List<string> labels = new List<string>();
                for (int i = 0; i < DayKeys.Length; i++)
                {
                    labels.Add(DayKeys[i].Translate());
                }

                return labels;
            }
        }

        public static int CurrentDay => Find.TickManager == null ? 0 : Find.TickManager.TicksGame / TicksPerDay;

        public static int CurrentWeekdayIndex => PositiveMod(CurrentDay, DaysPerWeek);

        public static float HourOfDayPrecise
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0f;
                }

                int dayTicks = Find.TickManager.TicksGame % TicksPerDay;
                return dayTicks / WageUtility.TicksPerHour;
            }
        }

        public static bool IsPaydayToday(int? absoluteDay = null)
        {
            int day = absoluteDay ?? CurrentDay;
            int payday = Mathf.Clamp(WealthBeyondMeasureMod.Settings?.paydayDayIndex ?? 4, 0, DaysPerWeek - 1);
            return PositiveMod(day, DaysPerWeek) == payday;
        }

        public static string PaydayLabel => WeekdayLabels[Mathf.Clamp(WealthBeyondMeasureMod.Settings?.paydayDayIndex ?? 4, 0, DaysPerWeek - 1)];

        public static int DaysUntilNextPayday()
        {
            int payday = Mathf.Clamp(WealthBeyondMeasureMod.Settings?.paydayDayIndex ?? 4, 0, DaysPerWeek - 1);
            int diff = payday - CurrentWeekdayIndex;
            if (diff < 0)
            {
                diff += DaysPerWeek;
            }

            return diff;
        }

        private static int PositiveMod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
