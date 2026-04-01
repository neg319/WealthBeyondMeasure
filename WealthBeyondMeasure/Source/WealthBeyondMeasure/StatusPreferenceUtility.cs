using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public static class StatusPreferenceUtility
    {
        private static readonly float[] DesiredWeaponValues = { 25f, 35f, 50f, 70f, 95f, 130f, 180f, 260f, 380f, 550f };
        private static readonly int[] DesiredFoodPreferability = { 4, 4, 5, 5, 6, 6, 7, 7, 8, 9 };

        public static float ScoreApparelForPawn(Pawn pawn, Apparel apparel, int wealthRank)
        {
            if (apparel == null)
            {
                return -100f;
            }

            int rank = TraitPreferenceUtility.GetPreferenceRank(pawn, wealthRank);
            float score = 0f;
            float marketValue = Mathf.Max(0f, apparel.MarketValue);
            float desiredValue = WealthRankUtility.GetDesiredApparelValue(rank);
            float valueDelta = marketValue - desiredValue;
            score -= Mathf.Abs(valueDelta) / Mathf.Max(12f, desiredValue * 0.35f);

            float hitPointPct = GetHitPointPct(apparel);
            int qualityIndex = GetQualityIndex(apparel);
            bool tainted = apparel.WornByCorpse;

            if (rank <= 2)
            {
                if (tainted) score += 2.2f;
                score += Mathf.Clamp01((0.65f - hitPointPct) / 0.65f) * 2.0f;
                score += qualityIndex <= (int)QualityCategory.Poor ? 1.8f : 0f;
                score -= qualityIndex >= (int)QualityCategory.Good ? 1.8f : 0f;
                score -= Mathf.Max(0f, valueDelta) / 30f;
            }
            else if (rank <= 6)
            {
                if (tainted) score -= 2.5f;
                score += Mathf.Clamp01((hitPointPct - 0.65f) / 0.35f) * 1.4f;
                score += 1.2f - Mathf.Abs(qualityIndex - (int)QualityCategory.Normal) * 0.5f;
            }
            else
            {
                if (tainted) score -= 6f;
                score += Mathf.Clamp01((hitPointPct - 0.75f) / 0.25f) * 2.5f;
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Good + 1) * 0.9f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 1.2f;
                score += Mathf.Max(0f, valueDelta) / 45f;
            }

            score += TraitPreferenceUtility.GetApparelTraitBias(pawn, apparel);
            return score;
        }

        public static float ScoreWeaponForPawn(Pawn pawn, ThingWithComps weapon, int wealthRank)
        {
            if (weapon == null || weapon.def == null || !weapon.def.IsWeapon)
            {
                return 0f;
            }

            int rank = TraitPreferenceUtility.GetPreferenceRank(pawn, wealthRank);
            float score = 0f;
            float marketValue = Mathf.Max(0f, weapon.MarketValue);
            float desiredValue = GetDesiredWeaponValue(rank);
            float valueDelta = marketValue - desiredValue;
            score -= Mathf.Abs(valueDelta) / Mathf.Max(15f, desiredValue * 0.4f);

            float hitPointPct = GetHitPointPct(weapon);
            int qualityIndex = GetQualityIndex(weapon);

            if (rank <= 2)
            {
                score += Mathf.Clamp01((0.7f - hitPointPct) / 0.7f) * 1.7f;
                score += qualityIndex <= (int)QualityCategory.Poor ? 1.4f : 0f;
                score -= qualityIndex >= (int)QualityCategory.Good ? 1.4f : 0f;
                score -= Mathf.Max(0f, valueDelta) / 35f;
            }
            else if (rank <= 6)
            {
                score += Mathf.Clamp01((hitPointPct - 0.6f) / 0.4f) * 1.2f;
                score += 1.0f - Mathf.Abs(qualityIndex - (int)QualityCategory.Normal) * 0.45f;
            }
            else
            {
                score += Mathf.Clamp01((hitPointPct - 0.75f) / 0.25f) * 2.0f;
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Good + 1) * 0.85f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 1.0f;
                score += Mathf.Max(0f, valueDelta) / 55f;
            }

            score += ScoreWeaponRoleFit(weapon, rank);
            score += TraitPreferenceUtility.GetWeaponTraitBias(pawn, weapon);
            return score;
        }

        public static float ScoreFoodForPawn(Pawn pawn, Thing foodSource, ThingDef foodDef, int wealthRank)
        {
            if (foodDef?.ingestible == null)
            {
                return 0f;
            }

            int rank = TraitPreferenceUtility.GetPreferenceRank(pawn, wealthRank);
            float score = 0f;
            int preferability = (int)foodDef.ingestible.preferability;
            int desiredPreferability = GetDesiredFoodPreferability(rank);
            score -= Mathf.Abs(preferability - desiredPreferability) * 0.8f;

            float marketValue = Mathf.Max(0f, foodSource?.MarketValue ?? foodDef.GetStatValueAbstract(StatDefOf.MarketValue));
            float nutrition = Mathf.Max(0.05f, foodDef.ingestible.CachedNutrition);
            float valuePerNutrition = marketValue / nutrition;
            float desiredValuePerNutrition = rank <= 2 ? 6f : rank <= 6 ? 12f : 22f;
            score -= Mathf.Abs(valuePerNutrition - desiredValuePerNutrition) / Mathf.Max(3f, desiredValuePerNutrition * 0.35f);

            int qualityIndex = GetQualityIndex(foodSource);
            if (rank <= 2)
            {
                score += qualityIndex <= (int)QualityCategory.Poor ? 1.5f : 0f;
                score -= qualityIndex >= (int)QualityCategory.Good ? 1.0f : 0f;
            }
            else if (rank >= 7)
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.7f;
                score -= Mathf.Max(0f, (int)QualityCategory.Good - qualityIndex) * 0.8f;
            }

            if (foodSource != null)
            {
                score += GetHitPointPct(foodSource) - 0.5f;
            }

            score += TraitPreferenceUtility.GetFoodTraitBias(pawn, foodSource);
            return score;
        }

        public static float GetCurrentLifestyleAlignmentScore(Pawn pawn, int wealthRank)
        {
            if (pawn == null)
            {
                return 0f;
            }

            float score = 0f;
            int apparelCount = 0;
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    score += Mathf.Clamp(ScoreApparelForPawn(pawn, apparel, wealthRank), -4f, 4f);
                    apparelCount++;
                }
            }

            if (apparelCount > 0)
            {
                score /= apparelCount;
            }

            ThingWithComps primary = pawn.equipment?.Primary;
            if (primary != null)
            {
                score += Mathf.Clamp(ScoreWeaponForPawn(pawn, primary, wealthRank), -4f, 4f) * 0.8f;
            }

            Building_Bed bed = pawn.ownership?.OwnedBed;
            if (bed != null)
            {
                score += ScoreBedForPawn(pawn, bed, wealthRank) * 0.7f;
            }

            return score;
        }

        public static string DescribeLifestyle(Pawn pawn, int wealthRank)
        {
            float score = GetCurrentLifestyleAlignmentScore(pawn, wealthRank);
            if (score >= 3.5f)
            {
                return "WBM_Lifestyle_Excellent".Translate();
            }

            if (score >= 1.5f)
            {
                return "WBM_Lifestyle_Good".Translate();
            }

            if (score <= -3.5f)
            {
                return "WBM_Lifestyle_Poor".Translate();
            }

            if (score <= -1.5f)
            {
                return "WBM_Lifestyle_Bad".Translate();
            }

            return "WBM_Lifestyle_Neutral".Translate();
        }

        public static float ScoreBedForPawn(Pawn pawn, Building_Bed bed, int wealthRank)
        {
            if (bed == null)
            {
                return 0f;
            }

            int rank = TraitPreferenceUtility.GetPreferenceRank(pawn, wealthRank);
            float comfort = bed.GetStatValue(StatDefOf.Comfort, true);
            float restEffectiveness = bed.GetStatValue(StatDefOf.BedRestEffectiveness, true);
            float total = comfort + restEffectiveness;
            float desired = rank <= 2 ? 1.0f : rank <= 6 ? 1.35f : 1.7f;
            float score = 1.2f - Mathf.Abs(total - desired) * 2.2f;

            int qualityIndex = GetQualityIndex(bed);
            if (rank <= 2)
            {
                score += qualityIndex <= (int)QualityCategory.Poor ? 0.8f : 0f;
            }
            else if (rank >= 7)
            {
                score += Mathf.Max(0f, qualityIndex - (int)QualityCategory.Normal) * 0.4f;
                score -= Mathf.Max(0f, (int)QualityCategory.Normal - qualityIndex) * 0.6f;
            }

            score += TraitPreferenceUtility.GetBedTraitBias(pawn, bed);
            return score;
        }

        public static float GetDesiredWeaponValue(int rank)
        {
            rank = Mathf.Clamp(rank, 0, DesiredWeaponValues.Length - 1);
            return DesiredWeaponValues[rank];
        }

        public static int GetDesiredFoodPreferability(int rank)
        {
            rank = Mathf.Clamp(rank, 0, DesiredFoodPreferability.Length - 1);
            return DesiredFoodPreferability[rank];
        }

        public static float GetHitPointPct(Thing thing)
        {
            if (thing == null)
            {
                return 1f;
            }

            return Mathf.Clamp01(thing.HitPoints / (float)Math.Max(1, thing.MaxHitPoints));
        }

        public static int GetQualityIndex(Thing thing)
        {
            if (thing != null && thing.TryGetQuality(out QualityCategory quality))
            {
                return (int)quality;
            }

            return (int)QualityCategory.Normal;
        }

        public static bool IsStrongFoodMatch(Pawn pawn, Thing foodSource, ThingDef foodDef)
        {
            int wealthRank = WealthGameComponent.Instance?.GetWealthRankIndex(pawn) ?? 0;
            return ScoreFoodForPawn(pawn, foodSource, foodDef, wealthRank) >= 1.2f;
        }

        public static bool IsStrongFoodMismatch(Pawn pawn, Thing foodSource, ThingDef foodDef)
        {
            int wealthRank = WealthGameComponent.Instance?.GetWealthRankIndex(pawn) ?? 0;
            return ScoreFoodForPawn(pawn, foodSource, foodDef, wealthRank) <= -1.2f;
        }

        private static float ScoreWeaponRoleFit(ThingWithComps weapon, int rank)
        {
            if (weapon?.def?.Verbs == null || weapon.def.Verbs.Count == 0)
            {
                return 0f;
            }

            bool ranged = weapon.def.Verbs.Any(verb => verb?.range > 1.42f);
            if (!ranged)
            {
                return 0f;
            }

            return rank >= 7 ? 0.4f : 0f;
        }
    }
}
