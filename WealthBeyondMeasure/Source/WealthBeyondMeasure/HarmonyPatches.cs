using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    [StaticConstructorOnStartup]
    public static class HarmonyBootstrap
    {
        static HarmonyBootstrap()
        {
            Harmony harmony = new Harmony("vyberware.wealthbeyondmeasure");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_Pawn_GetInspectString
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            if (__instance == null || !__instance.IsColonistPlayerControlled || WealthGameComponent.Instance == null)
            {
                return;
            }

            List<string> lines = new List<string>
            {
                "WBM_InspectWallet".Translate(WealthGameComponent.Instance.GetWalletSilver(__instance)),
                "WBM_InspectCommunityAssistance".Translate(WealthGameComponent.Instance.GetCommunityAssistanceLabel(__instance)),
                "WBM_InspectCurrentGross".Translate(WealthGameComponent.Instance.GetCurrentGrossCycleSilver(__instance).ToString("0.0")),
                "WBM_InspectCurrentExpenses".Translate(WealthGameComponent.Instance.GetCurrentExpenseCycleSilver(__instance).ToString("0.0")),
                "WBM_InspectCurrentNet".Translate(WealthGameComponent.Instance.GetCurrentNetCycleSilver(__instance).ToString("0.0")),
                "WBM_InspectRank".Translate(WealthGameComponent.Instance.GetWealthRankLabel(__instance)),
                "WBM_InspectSocialStanding".Translate(SocialWealthUtility.GetSignedOpinionString(WealthGameComponent.Instance.GetSocialWealthOpinionOffset(__instance))),
                "WBM_InspectLifestyle".Translate(WealthGameComponent.Instance.GetLifestyleStatus(__instance)),
                "WBM_InspectPayday".Translate(PaydayUtility.PaydayLabel),
                "WBM_InspectWorkBreakdown".Translate(),
                WealthGameComponent.Instance.GetWorkBreakdownSummary(__instance, 6)
            };

            string extra = "\n" + string.Join("\n", lines);
            __result = string.IsNullOrEmpty(__result) ? extra.TrimStart('\n') : __result + extra;
        }
    }

    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs))]
    public static class Patch_MapInterface_MapInterfaceOnGUI_AfterMainTabs
    {
        private const float WindowWidth = 360f;
        private const float WindowHeight = 350f;
        private const float FixedInfoHeight = 182f;

        public static Vector2 ScrollPosition = Vector2.zero;

        public static void Postfix()
        {
            if (!(WealthBeyondMeasureMod.Settings?.showOverlay ?? true))
            {
                return;
            }

            if (Current.ProgramState != ProgramState.Playing || Find.Selector == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            if (!(Find.Selector.SingleSelectedThing is Pawn pawn) || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            Rect rect = new Rect(UI.screenWidth - 390f, 70f, WindowWidth, WindowHeight);
            Find.WindowStack.ImmediateWindow(0x5B178E11, rect, WindowLayer.GameUI, delegate
            {
                Rect inner = rect.AtZero().ContractedBy(8f);
                Widgets.DrawMenuSection(inner);
                Rect content = inner.ContractedBy(10f);
                Text.Font = GameFont.Small;

                Widgets.Label(new Rect(content.x, content.y, content.width, 24f), "WBM_OverlayTitle".Translate());

                Rect fixedInfo = new Rect(content.x, content.y + 24f, content.width, FixedInfoHeight);
                List<string> infoLines = new List<string>
                {
                    "WBM_OverlayWallet".Translate(WealthGameComponent.Instance.GetWalletSilver(pawn)),
                    "WBM_OverlayCommunityAssistance".Translate(WealthGameComponent.Instance.GetCommunityAssistanceLabel(pawn)),
                    "WBM_OverlayCurrentGross".Translate(WealthGameComponent.Instance.GetCurrentGrossCycleSilver(pawn).ToString("0.0")),
                    "WBM_OverlayCurrentExpenses".Translate(WealthGameComponent.Instance.GetCurrentExpenseCycleSilver(pawn).ToString("0.0")),
                    "WBM_OverlayCurrentNet".Translate(WealthGameComponent.Instance.GetCurrentNetCycleSilver(pawn).ToString("0.0")),
                    "WBM_OverlayLifetime".Translate(WealthGameComponent.Instance.GetLifetimeEarnedSilver(pawn).ToString("0.0"), WealthGameComponent.Instance.GetLifetimeSpentSilver(pawn).ToString("0.0")),
                    "WBM_OverlayRank".Translate(WealthGameComponent.Instance.GetWealthRankLabel(pawn)),
                    "WBM_OverlaySocialStanding".Translate(SocialWealthUtility.GetSignedOpinionString(WealthGameComponent.Instance.GetSocialWealthOpinionOffset(pawn))),
                    "WBM_OverlayLifestyle".Translate(WealthGameComponent.Instance.GetLifestyleStatus(pawn)),
                    "WBM_OverlayPayday".Translate(PaydayUtility.PaydayLabel)
                };
                Widgets.Label(fixedInfo, string.Join("\n", infoLines));

                Rect breakdownLabel = new Rect(content.x, content.y + 214f, content.width, 24f);
                Widgets.Label(breakdownLabel, "WBM_OverlayWorkBreakdown".Translate());

                Rect outRect = new Rect(content.x, content.y + 238f, content.width, 96f);
                string breakdownText = WealthGameComponent.Instance.GetWorkBreakdownSummary(pawn, 20);
                float height = Text.CalcHeight(breakdownText, outRect.width - 16f);
                Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, height));
                Widgets.BeginScrollView(outRect, ref ScrollPosition, viewRect);
                Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), breakdownText);
                Widgets.EndScrollView();
            }, false, false, 1f);
        }
    }

    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw")]
    public static class Patch_JobGiver_OptimizeApparel_ApparelScoreRaw
    {
        public static void Postfix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (pawn == null || ap == null || WealthGameComponent.Instance == null || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            int rank = WealthGameComponent.Instance.GetWealthRankIndex(pawn);
            float statusScore = StatusPreferenceUtility.ScoreApparelForPawn(pawn, ap, rank);
            __result += statusScore * 1.75f;
        }
    }

    [HarmonyPatch(typeof(FoodUtility), "FoodOptimality")]
    public static class Patch_FoodUtility_FoodOptimality
    {
        public static void Postfix(ref float __result, Pawn eater, Thing foodSource, ThingDef foodDef, float dist, bool takingToInventory = false)
        {
            if (eater == null || foodDef == null || WealthGameComponent.Instance == null || !eater.IsColonistPlayerControlled)
            {
                return;
            }

            int rank = WealthGameComponent.Instance.GetWealthRankIndex(eater);
            float statusScore = StatusPreferenceUtility.ScoreFoodForPawn(eater, foodSource, foodDef, rank);
            __result += statusScore * 8f;
        }
    }

    [HarmonyPatch(typeof(Pawn_InventoryTracker), "TryAddItemNotForSale")]
    public static class Patch_Pawn_InventoryTracker_TryAddItemNotForSale
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_InventoryTracker), "pawn");

        public static void Prefix(Pawn_InventoryTracker __instance, Thing item, ref bool __state)
        {
            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            __state = WageUtility.ShouldChargeForConsumableInventoryTake(pawn, item);
        }

        public static void Postfix(Pawn_InventoryTracker __instance, Thing item, bool __result, bool __state)
        {
            if (!__result || !__state || item?.def == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (!WageUtility.IsPawnEligible(pawn))
            {
                return;
            }

            WealthGameComponent.Instance.RegisterConsumableTakenToInventory(pawn, item);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Thing_Ingested
    {
        public static void Prefix(Thing __instance, Pawn ingester, ref bool __state)
        {
            __state = false;
            if (__instance?.def == null || ingester == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            if (!WageUtility.IsPawnEligible(ingester))
            {
                return;
            }

            bool fromOwnInventory = ingester.inventory?.innerContainer != null &&
                (__instance.ParentHolder == ingester.inventory.innerContainer || ingester.inventory.innerContainer.Contains(__instance));
            if (fromOwnInventory)
            {
                __state = WealthGameComponent.Instance.TryUsePrepaidConsumable(ingester, __instance.def, 1);
            }
        }

        public static void Postfix(Thing __instance, Pawn ingester, bool __state)
        {
            if (__instance?.def == null || ingester == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            if (!WageUtility.IsPawnEligible(ingester))
            {
                return;
            }

            if (!__instance.def.IsNutritionGivingIngestible)
            {
                return;
            }

            if (!__state)
            {
                WealthGameComponent.Instance.RegisterConsumableDirectUse(ingester, __instance.def, 1);
            }

            if (ingester.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            if (StatusPreferenceUtility.IsStrongFoodMatch(ingester, __instance, __instance.def))
            {
                ingester.needs.mood.thoughts.memories.TryGainMemory(WBM_DefOf.WBM_AteStatusMealThought);
            }
            else if (StatusPreferenceUtility.IsStrongFoodMismatch(ingester, __instance, __instance.def))
            {
                ingester.needs.mood.thoughts.memories.TryGainMemory(WBM_DefOf.WBM_AteBadStatusMealThought);
            }
        }
    }

    [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
    public static class Patch_TendUtility_DoTend
    {
        public static void Postfix(Pawn patient, object medicine)
        {
            if (patient == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            if (!WageUtility.IsPawnEligible(patient))
            {
                return;
            }

            Thing medicineThing = medicine as Thing;
            if (medicineThing?.def == null)
            {
                return;
            }

            WealthGameComponent.Instance.RegisterConsumableDirectUse(patient, medicineThing.def, 1);
        }
    }

    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf))]
    public static class Patch_Pawn_RelationsTracker_OpinionOf
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn");

        public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, ref int __result)
        {
            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || other == null || WealthGameComponent.Instance == null)
            {
                return;
            }

            __result += SocialWealthUtility.GetOpinionOffsetTowardTarget(pawn, other);
        }
    }

    [HarmonyPatch(typeof(TraderKindDef), "WillTrade")]
    public static class Patch_TraderKindDef_WillTrade
    {
        public static void Postfix(TraderKindDef __instance, ThingDef td, ref bool __result)
        {
            if (__instance?.defName == BankerUtility.BankerTraderKindDefName && td != null)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_Thing_TakeDamage_BankProtection
    {
        public static bool Prefix(Thing __instance, ref DamageWorker.DamageResult __result)
        {
            if (__instance == null)
            {
                return true;
            }

            if (__instance is Building_BankVault || BankVaultUtility.IsBankSilver(__instance))
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }

            return true;
        }
    }
}
