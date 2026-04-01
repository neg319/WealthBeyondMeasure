using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public static class SocialWealthUtility
    {
        public static int GetOpinionOffsetTowardTarget(Pawn observer, Pawn target)
        {
            if (!ShouldApply(observer, target))
            {
                return 0;
            }

            int observerRank = WealthGameComponent.Instance.GetWealthRankIndex(observer);
            int targetRank = WealthGameComponent.Instance.GetWealthRankIndex(target);

            int prestige = GetWealthPrestigeOpinionOffset(target);
            int affinity = GetSharedWealthAffinityOffset(observerRank, targetRank);
            int gapPenalty = GetWealthGapPenalty(observerRank, targetRank);

            return Mathf.Clamp(prestige + affinity - gapPenalty, -12, 12);
        }

        public static int GetWealthPrestigeOpinionOffset(Pawn target)
        {
            if (target == null || WealthGameComponent.Instance == null || !target.IsColonistPlayerControlled)
            {
                return 0;
            }

            int wealthRank = WealthGameComponent.Instance.GetWealthRankIndex(target);
            float centeredRank = wealthRank - ((WealthRankUtility.RankCount - 1) / 2f);
            int offset = Mathf.RoundToInt(centeredRank * 1.25f);

            return Mathf.Clamp(offset, -6, 6);
        }

        public static string GetSignedOpinionString(int offset)
        {
            return offset.ToString("+0;-0;0");
        }

        private static int GetSharedWealthAffinityOffset(int observerRank, int targetRank)
        {
            int rankCount = WealthRankUtility.RankCount;
            int poorCeiling = Mathf.FloorToInt((rankCount - 1) * 0.33f);
            int richFloor = Mathf.CeilToInt((rankCount - 1) * 0.66f);
            int diff = Mathf.Abs(observerRank - targetRank);

            int offset = 0;

            if (observerRank <= poorCeiling && targetRank <= poorCeiling)
            {
                float observerPoorProgress = 1f - (observerRank / Mathf.Max(1f, poorCeiling));
                float targetPoorProgress = 1f - (targetRank / Mathf.Max(1f, poorCeiling));
                offset += 2 + Mathf.RoundToInt(((observerPoorProgress + targetPoorProgress) * 0.5f) * 3f);
            }
            else if (observerRank >= richFloor && targetRank >= richFloor)
            {
                float observerRichProgress = (observerRank - richFloor) / Mathf.Max(1f, (rankCount - 1) - richFloor);
                float targetRichProgress = (targetRank - richFloor) / Mathf.Max(1f, (rankCount - 1) - richFloor);
                offset += 2 + Mathf.RoundToInt(((observerRichProgress + targetRichProgress) * 0.5f) * 3f);
            }

            if (diff == 0)
            {
                offset += 4;
            }
            else if (diff == 1)
            {
                offset += 2;
            }
            else if (diff == 2)
            {
                offset += 1;
            }

            return offset;
        }

        private static int GetWealthGapPenalty(int observerRank, int targetRank)
        {
            int rankCount = WealthRankUtility.RankCount;
            int medianFloor = Mathf.FloorToInt((rankCount - 1) * 0.5f);
            int diff = Mathf.Abs(observerRank - targetRank);
            int penalty = Mathf.RoundToInt(diff * 1.5f);

            bool observerPoorSide = observerRank < medianFloor;
            bool targetPoorSide = targetRank < medianFloor;
            if (observerPoorSide != targetPoorSide && diff >= 4)
            {
                penalty += 2;
            }

            return penalty;
        }

        private static bool ShouldApply(Pawn observer, Pawn target)
        {
            if (observer == null || target == null || observer == target || WealthGameComponent.Instance == null)
            {
                return false;
            }

            if (!observer.IsColonistPlayerControlled || !target.IsColonistPlayerControlled)
            {
                return false;
            }

            if (observer.RaceProps == null || target.RaceProps == null || !observer.RaceProps.Humanlike || !target.RaceProps.Humanlike)
            {
                return false;
            }

            return true;
        }
    }
}
