using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WealthBeyondMeasure
{
    public static class WageUtility
    {
        public const float TicksPerHour = 2500f;

        public static bool IsPawnEligible(Pawn pawn)
        {
            return pawn != null &&
                   pawn.IsColonistPlayerControlled &&
                   pawn.RaceProps != null &&
                   pawn.RaceProps.Humanlike &&
                   !pawn.IsPrisoner &&
                   !pawn.Downed &&
                   !pawn.Dead;
        }

        public static bool TryGetActivePaidWorkType(Pawn pawn, out WorkTypeDef workType)
        {
            workType = null;
            if (!IsPawnEligible(pawn) || pawn.Drafted || pawn.InMentalState)
            {
                return false;
            }

            Job job = pawn.CurJob;
            if (job == null || job.def == null)
            {
                return false;
            }

            if (!IsBillableJob(job))
            {
                return false;
            }

            if (IsPawnMoving(pawn) || IsPawnOnBreakLikeActivity(pawn, job))
            {
                return false;
            }

            workType = GetWorkTypeForJob(pawn, job);
            return workType != null;
        }

        public static bool IsBillableJob(Job job)
        {
            if (job?.def == null)
            {
                return false;
            }

            if (job.def == WBM_DefOf.WBM_CollectPaycheck)
            {
                return false;
            }

            if (job.def.joyKind != null)
            {
                return false;
            }

            string defName = job.def.defName ?? string.Empty;
            if (defName.StartsWith("Goto", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Wait", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("LayDown", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Ingest", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Lovin", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("SocialRelax", StringComparison.OrdinalIgnoreCase) ||
                defName.Contains("Idle") ||
                defName.Contains("Flee") ||
                defName.Contains("Break") ||
                defName.Contains("UnloadInventory") ||
                defName.Contains("TakeInventory") ||
                defName.Contains("CarryToCryptosleepCasket") ||
                defName.Contains("Meditate") ||
                defName.Contains("Play") ||
                defName.Contains("Follow"))
            {
                return false;
            }

            return true;
        }

        public static float GetSilverPerTick(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return 0f;
            }

            return WealthBeyondMeasureMod.Settings?.GetSilverPerHour(workType.defName) / TicksPerHour ?? 0f;
        }

        public static float TicksToHours(int ticks)
        {
            return ticks / TicksPerHour;
        }

        public static IEnumerable<WorkTypeDef> AllVisibleWorkTypes()
        {
            return DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(def => def != null)
                .OrderBy(def => def.naturalPriority)
                .ThenBy(def => def.label ?? def.defName);
        }

        public static float GetDefaultSilverPerHour(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return 0f;
            }

            string key = (workType.defName ?? string.Empty).ToLowerInvariant();
            string label = (workType.labelShort ?? workType.label ?? string.Empty).ToLowerInvariant();
            string combined = key + " " + label;

            if (combined.Contains("research")) return 12f;
            if (combined.Contains("doctor") || combined.Contains("surgery")) return 10f;
            if (combined.Contains("construct")) return 8f;
            if (combined.Contains("smith") || combined.Contains("tailor") || combined.Contains("craft") || combined.Contains("art")) return 7f;
            if (combined.Contains("cook")) return 6f;
            if (combined.Contains("grow") || combined.Contains("plant") || combined.Contains("hunt") || combined.Contains("handle") || combined.Contains("warden")) return 5f;
            if (combined.Contains("haul") || combined.Contains("clean") || combined.Contains("basic")) return 3f;
            if (combined.Contains("fire")) return 2f;
            if (combined.Contains("patient")) return 0f;
            return 5f;
        }

        public static float GetDefaultConsumablePrice(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return 0f;
            }

            float marketValue = Math.Max(0f, thingDef.GetStatValueAbstract(StatDefOf.MarketValue));
            if (thingDef.IsMedicine)
            {
                return Math.Max(0.1f, marketValue * 0.5f);
            }

            if (thingDef.IsNutritionGivingIngestible)
            {
                return Math.Max(0.1f, marketValue * 0.35f);
            }

            return Math.Max(0.1f, marketValue * 0.25f);
        }

        public static IEnumerable<ThingDef> AllConsumablesForPricing()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def != null && (def.IsNutritionGivingIngestible || def.IsMedicine))
                .OrderBy(def => def.IsMedicine ? 1 : 0)
                .ThenBy(def => def.label ?? def.defName);
        }

        public static bool IsPricedConsumable(ThingDef thingDef)
        {
            return thingDef != null &&
                   (thingDef.IsNutritionGivingIngestible || thingDef.IsMedicine) &&
                   (WealthBeyondMeasureMod.Settings?.GetConsumableCost(thingDef.defName) ?? 0f) > 0f;
        }

        public static bool ShouldChargeForConsumableInventoryTake(Pawn pawn, Thing item)
        {
            if (!IsPawnEligible(pawn) || item?.def == null || !IsPricedConsumable(item.def))
            {
                return false;
            }

            if (!BankVaultUtility.IsInSharedNonBankStockpile(item))
            {
                return false;
            }

            Job job = pawn.CurJob;
            string defName = job?.def?.defName ?? string.Empty;
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }

            if (IsConsumableJobExemptFromPersonalCharge(defName))
            {
                return false;
            }

            return defName.IndexOf("TakeInventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   defName.IndexOf("Ingest", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsConsumableJobExemptFromPersonalCharge(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }

            string[] exemptTerms =
            {
                "Tend", "Doctor", "Feed", "DeliverFood", "CarryFood", "BringFood", "FoodFeed",
                "Cook", "DoBill", "Make", "Prepare", "Operate", "Surgery", "Rescue", "CarryToBed",
                "Haul", "Hauling", "Deliver", "Carry", "Transfer", "Unload", "LoadTransporter",
                "Refill", "Refuel", "Store", "Stockpile"
            };

            for (int i = 0; i < exemptTerms.Length; i++)
            {
                if (defName.IndexOf(exemptTerms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPawnMoving(Pawn pawn)
        {
            if (pawn?.pather == null)
            {
                return false;
            }

            try
            {
                Type type = pawn.pather.GetType();
                var prop = AccessTools.Property(type, "MovingNow") ?? AccessTools.Property(type, "Moving");
                if (prop != null && prop.GetValue(pawn.pather, null) is bool moving)
                {
                    return moving;
                }

                var field = AccessTools.Field(type, "moving") ?? AccessTools.Field(type, "movingNow");
                if (field != null && field.GetValue(pawn.pather) is bool movingField)
                {
                    return movingField;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool IsPawnOnBreakLikeActivity(Pawn pawn, Job job)
        {
            if (pawn == null || job?.def == null)
            {
                return true;
            }

            if (pawn.needs?.rest != null && pawn.needs.rest.CurLevelPercentage < 0.12f)
            {
                return true;
            }

            if (pawn.needs?.food != null && pawn.needs.food.CurLevelPercentage < 0.08f)
            {
                return true;
            }

            if (pawn.CurJobDef == JobDefOf.Ingest || pawn.CurJobDef == JobDefOf.LayDown)
            {
                return true;
            }

            return false;
        }

        public static WorkTypeDef GetWorkTypeForJob(Pawn pawn, Job job)
        {
            if (pawn?.workSettings?.WorkGiversInOrderNormal == null || job?.def == null)
            {
                return null;
            }

            WorkGiverDef bestMatch = null;
            int bestPriority = int.MaxValue;
            foreach (WorkGiverDef def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (def?.workType == null || def.priorityInType < 0)
                {
                    continue;
                }

                if (def.workerClass == null)
                {
                    continue;
                }

                if (def.giverClass != null && !typeof(WorkGiver).IsAssignableFrom(def.giverClass))
                {
                    continue;
                }

                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count > 0 && job.targetA.Thing != null && !def.fixedBillGiverDefs.Contains(job.targetA.Thing.def))
                {
                    continue;
                }

                if (def.workType == WorkTypeDefOf.Firefighter && job.def.defName.IndexOf("BeatFire", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool match = false;
                if (def.emergency && job.def.defName.IndexOf("Rescue", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }

                if (job.def.defName.IndexOf(def.defName ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }

                if (!match && def.verb != null && job.def.defName.IndexOf(def.verb, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }

                if (!match && def.workType == WorkTypeDefOf.Doctor &&
                    (job.def.defName.IndexOf("Tend", StringComparison.OrdinalIgnoreCase) >= 0 || job.def.defName.IndexOf("Doctor", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    match = true;
                }

                if (!match && def.workType == WorkTypeDefOf.Cook &&
                    (job.def.defName.IndexOf("Cook", StringComparison.OrdinalIgnoreCase) >= 0 || job.def.defName.IndexOf("Prepare", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    match = true;
                }

                if (!match && def.workType == WorkTypeDefOf.Research && job.def.defName.IndexOf("Research", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }

                if (!match)
                {
                    continue;
                }

                if (def.priorityInType < bestPriority)
                {
                    bestPriority = def.priorityInType;
                    bestMatch = def;
                }
            }

            if (bestMatch != null)
            {
                return bestMatch.workType;
            }

            return pawn.workSettings.WorkGiversInOrderNormal
                .Select(giver => giver?.def?.workType)
                .FirstOrDefault(type => type != null && job.def.defName.IndexOf(type.defName ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
