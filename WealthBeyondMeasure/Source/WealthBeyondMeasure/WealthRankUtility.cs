using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public static class WealthRankUtility
    {
        private static readonly int[] Thresholds = { 0, 50, 150, 300, 600, 1000, 1600, 2500, 4000, 6500 };
        private static readonly float[] DesiredApparelValues = { 15f, 22f, 35f, 50f, 70f, 100f, 140f, 200f, 300f, 450f };

        public static int RankCount => Thresholds.Length;

        public static int GetRankIndex(int carriedSilver)
        {
            int rank = 0;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (carriedSilver >= Thresholds[i])
                {
                    rank = i;
                }
            }

            return Mathf.Clamp(rank, 0, Thresholds.Length - 1);
        }

        public static string GetRankLabel(int carriedSilver)
        {
            return GetRankLabelByIndex(GetRankIndex(carriedSilver));
        }

        public static string GetRankLabelByIndex(int index)
        {
            index = Mathf.Clamp(index, 0, Thresholds.Length - 1);
            return ("WBM_Rank_" + index).Translate();
        }

        public static float GetDesiredApparelValue(int rankIndex)
        {
            rankIndex = Mathf.Clamp(rankIndex, 0, DesiredApparelValues.Length - 1);
            return DesiredApparelValues[rankIndex];
        }

        public static float GetCommunityAssistanceMultiplier(int rankIndex)
        {
            rankIndex = Mathf.Clamp(rankIndex, 0, RankCount - 1);
            return 0.1f + (0.1f * rankIndex);
        }

        public static IEnumerable<int> GetThresholds()
        {
            return Thresholds;
        }
    }
}
