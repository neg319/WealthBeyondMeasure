using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public class WealthSettings : ModSettings
    {
        public int paydayDayIndex = 4;
        public bool showOverlay = true;
        public bool infiniteBankSilver = false;
        public int currencyStyleIndex = 0;
        public List<WorkTypePayRateEntry> workTypePayRates = new List<WorkTypePayRateEntry>();
        public List<ConsumablePriceEntry> consumablePrices = new List<ConsumablePriceEntry>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref paydayDayIndex, "paydayDayIndex", 4);
            Scribe_Values.Look(ref showOverlay, "showOverlay", true);
            Scribe_Values.Look(ref infiniteBankSilver, "infiniteBankSilver", false);
            Scribe_Values.Look(ref currencyStyleIndex, "currencyStyleIndex", 0);
            Scribe_Collections.Look(ref workTypePayRates, "workTypePayRates", LookMode.Deep);
            Scribe_Collections.Look(ref consumablePrices, "consumablePrices", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                workTypePayRates ??= new List<WorkTypePayRateEntry>();
                consumablePrices ??= new List<ConsumablePriceEntry>();
            }
        }

        public void EnsureDefaultsLoaded()
        {
            workTypePayRates ??= new List<WorkTypePayRateEntry>();
            consumablePrices ??= new List<ConsumablePriceEntry>();

            foreach (WorkTypeDef workType in WageUtility.AllVisibleWorkTypes())
            {
                if (workType == null)
                {
                    continue;
                }

                if (!workTypePayRates.Any(entry => entry != null && entry.workTypeDefName == workType.defName))
                {
                    workTypePayRates.Add(new WorkTypePayRateEntry(workType.defName, WageUtility.GetDefaultSilverPerHour(workType)));
                }
            }

            foreach (ThingDef thingDef in WageUtility.AllConsumablesForPricing())
            {
                if (thingDef == null)
                {
                    continue;
                }

                if (!consumablePrices.Any(entry => entry != null && entry.thingDefName == thingDef.defName))
                {
                    consumablePrices.Add(new ConsumablePriceEntry(thingDef.defName, WageUtility.GetDefaultConsumablePrice(thingDef)));
                }
            }

            workTypePayRates = workTypePayRates
                .Where(entry => entry != null && !string.IsNullOrEmpty(entry.workTypeDefName))
                .GroupBy(entry => entry.workTypeDefName)
                .Select(group => group.First())
                .OrderBy(entry => DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.workTypeDefName)?.naturalPriority ?? 9999)
                .ThenBy(entry => DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.workTypeDefName)?.label ?? entry.workTypeDefName)
                .ToList();

            consumablePrices = consumablePrices
                .Where(entry => entry != null && !string.IsNullOrEmpty(entry.thingDefName))
                .GroupBy(entry => entry.thingDefName)
                .Select(group => group.First())
                .OrderBy(entry => DefDatabase<ThingDef>.GetNamedSilentFail(entry.thingDefName)?.IsMedicine == true ? 1 : 0)
                .ThenBy(entry => DefDatabase<ThingDef>.GetNamedSilentFail(entry.thingDefName)?.label ?? entry.thingDefName)
                .ToList();
        }

        public float GetSilverPerHour(string workTypeDefName)
        {
            EnsureDefaultsLoaded();
            WorkTypePayRateEntry entry = workTypePayRates.FirstOrDefault(x => x != null && x.workTypeDefName == workTypeDefName);
            if (entry != null)
            {
                return entry.silverPerHour;
            }

            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
            return WageUtility.GetDefaultSilverPerHour(workType);
        }

        public float GetConsumableCost(string thingDefName)
        {
            EnsureDefaultsLoaded();
            ConsumablePriceEntry entry = consumablePrices.FirstOrDefault(x => x != null && x.thingDefName == thingDefName);
            if (entry != null)
            {
                return entry.silverCost;
            }

            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            return WageUtility.GetDefaultConsumablePrice(thingDef);
        }
    }
}
