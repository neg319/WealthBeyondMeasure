using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public static class TraitPreferenceUtility
    {
        private const int HighMaintenanceRankOffset = 3;
        private const int FrugalRankOffset = -3;

        public static bool HasHighMaintenance(Pawn pawn)
        {
            return pawn?.story?.traits?.HasTrait(WBM_DefOf.WBM_HighMaintenance) ?? false;
        }

        public static bool HasFrugal(Pawn pawn)
        {
            return pawn?.story?.traits?.HasTrait(WBM_DefOf.WBM_Frugal) ?? false;
        }

        public static int GetPreferenceRank(Pawn pawn, int wealthRank)
        {
            int adjusted = wealthRank;

            if (HasHighMaintenance(pawn))
            {
                adjusted += HighMaintenanceRankOffset;
            }

            if (HasFrugal(pawn))
            {
                adjusted += FrugalRankOffset;
            }

            return Mathf.Clamp(adjusted, 0, WealthRankUtility.RankCount - 1);
        }

        public static float GetApparelTraitBias(Pawn pawn, Apparel apparel)
        {
            if (apparel == null)
            {
                return 0f;
            }

            float score = 0f;
            int qualityIndex = StatusPreferenceUtility.GetQualityIndex(apparel);
            float hitPointPct = StatusPreferenceUtility.GetHitPointPct(apparel);
            bool tainted = apparel.WornByCorpse;

            if (HasHighMaintenance(pawn))
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.9f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 1.2f;
                score += Mathf.Clamp01((hitPointPct - 0.8f) / 0.2f) * 1.4f;
                if (tainted)
                {
                    score -= 3.5f;
                }
            }

            if (HasFrugal(pawn))
            {
                score += Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 0.9f;
                score -= Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.8f;
                score += Mathf.Clamp01((0.8f - hitPointPct) / 0.8f) * 1.1f;
                if (tainted)
                {
                    score += 1.6f;
                }
            }

            return score;
        }

        public static float GetWeaponTraitBias(Pawn pawn, ThingWithComps weapon)
        {
            if (weapon == null)
            {
                return 0f;
            }

            float score = 0f;
            int qualityIndex = StatusPreferenceUtility.GetQualityIndex(weapon);
            float hitPointPct = StatusPreferenceUtility.GetHitPointPct(weapon);

            if (HasHighMaintenance(pawn))
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.8f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 1.0f;
                score += Mathf.Clamp01((hitPointPct - 0.8f) / 0.2f) * 1.2f;
            }

            if (HasFrugal(pawn))
            {
                score += Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 0.75f;
                score -= Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.7f;
                score += Mathf.Clamp01((0.8f - hitPointPct) / 0.8f) * 0.9f;
            }

            return score;
        }

        public static float GetFoodTraitBias(Pawn pawn, Thing foodSource)
        {
            float score = 0f;
            int qualityIndex = StatusPreferenceUtility.GetQualityIndex(foodSource);

            if (HasHighMaintenance(pawn))
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.7f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 0.85f;
            }

            if (HasFrugal(pawn))
            {
                score += Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 0.65f;
                score -= Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.55f;
            }

            return score;
        }

        public static float GetBedTraitBias(Pawn pawn, Building_Bed bed)
        {
            if (bed == null)
            {
                return 0f;
            }

            float score = 0f;
            int qualityIndex = StatusPreferenceUtility.GetQualityIndex(bed);
            float comfort = bed.GetStatValue(StatDefOf.Comfort, true);

            if (HasHighMaintenance(pawn))
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.5f;
                score -= Mathf.Max(0f, (int)QualityCategory.Normal - qualityIndex) * 0.9f;
                score += Mathf.Clamp01((comfort - 0.7f) / 0.3f) * 0.8f;
            }

            if (HasFrugal(pawn))
            {
                score += Mathf.Max(0f, (int)QualityCategory.Normal - qualityIndex) * 0.55f;
                score -= Mathf.Max(0f, qualityIndex - (int)QualityCategory.Good) * 0.5f;
            }

            return score;
        }
    }
}
