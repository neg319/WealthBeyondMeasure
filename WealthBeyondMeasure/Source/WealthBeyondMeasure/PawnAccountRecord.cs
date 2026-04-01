using System.Collections.Generic;
using Verse;

namespace WealthBeyondMeasure
{
    public class PawnAccountRecord : IExposable
    {
        public int expectedWalletSilver;
        public double cycleGrossSilver;
        public double cycleExpenseSilver;
        public double lifetimeGrossSilver;
        public double lifetimeExpenseSilver;
        public int cycleExpensePaidSilverWhole;
        public int currentPaydayPayoutSilver;
        public int currentPaydayDeductionSilver;
        public int rolloverDebtSilver;
        public int lastSettlementDay = -1;
        public int lastPositiveMoodDay = -1;
        public int lastNegativeMoodDay = -1;
        public Dictionary<string, int> cycleWorkedTicksByType = new Dictionary<string, int>();
        public Dictionary<string, int> lifetimeWorkedTicksByType = new Dictionary<string, int>();
        public Dictionary<string, int> prepaidConsumablesByDef = new Dictionary<string, int>();

        public void AddWorkedTick(string workTypeDefName, float silverPerTick)
        {
            if (string.IsNullOrEmpty(workTypeDefName))
            {
                return;
            }

            cycleGrossSilver += silverPerTick;
            lifetimeGrossSilver += silverPerTick;

            if (!cycleWorkedTicksByType.ContainsKey(workTypeDefName))
            {
                cycleWorkedTicksByType[workTypeDefName] = 0;
            }

            if (!lifetimeWorkedTicksByType.ContainsKey(workTypeDefName))
            {
                lifetimeWorkedTicksByType[workTypeDefName] = 0;
            }

            cycleWorkedTicksByType[workTypeDefName] += 1;
            lifetimeWorkedTicksByType[workTypeDefName] += 1;
        }

        public void AddPrepaidConsumables(string thingDefName, int count)
        {
            if (string.IsNullOrEmpty(thingDefName) || count <= 0)
            {
                return;
            }

            if (!prepaidConsumablesByDef.ContainsKey(thingDefName))
            {
                prepaidConsumablesByDef[thingDefName] = 0;
            }

            prepaidConsumablesByDef[thingDefName] += count;
        }

        public bool TryConsumePrepaidConsumable(string thingDefName, int count = 1)
        {
            if (string.IsNullOrEmpty(thingDefName) || count <= 0)
            {
                return false;
            }

            if (!prepaidConsumablesByDef.TryGetValue(thingDefName, out int available) || available < count)
            {
                return false;
            }

            available -= count;
            if (available <= 0)
            {
                prepaidConsumablesByDef.Remove(thingDefName);
            }
            else
            {
                prepaidConsumablesByDef[thingDefName] = available;
            }

            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref expectedWalletSilver, "expectedWalletSilver", 0);
            Scribe_Values.Look(ref cycleGrossSilver, "cycleGrossSilver", 0d);
            Scribe_Values.Look(ref cycleExpenseSilver, "cycleExpenseSilver", 0d);
            Scribe_Values.Look(ref lifetimeGrossSilver, "lifetimeGrossSilver", 0d);
            Scribe_Values.Look(ref lifetimeExpenseSilver, "lifetimeExpenseSilver", 0d);
            Scribe_Values.Look(ref cycleExpensePaidSilverWhole, "cycleExpensePaidSilverWhole", 0);
            Scribe_Values.Look(ref currentPaydayPayoutSilver, "currentPaydayPayoutSilver", 0);
            Scribe_Values.Look(ref currentPaydayDeductionSilver, "currentPaydayDeductionSilver", 0);
            Scribe_Values.Look(ref rolloverDebtSilver, "rolloverDebtSilver", 0);
            Scribe_Values.Look(ref lastSettlementDay, "lastSettlementDay", -1);
            Scribe_Values.Look(ref lastPositiveMoodDay, "lastPositiveMoodDay", -1);
            Scribe_Values.Look(ref lastNegativeMoodDay, "lastNegativeMoodDay", -1);
            Scribe_Collections.Look(ref cycleWorkedTicksByType, "cycleWorkedTicksByType", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lifetimeWorkedTicksByType, "lifetimeWorkedTicksByType", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref prepaidConsumablesByDef, "prepaidConsumablesByDef", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cycleWorkedTicksByType ??= new Dictionary<string, int>();
                lifetimeWorkedTicksByType ??= new Dictionary<string, int>();
                prepaidConsumablesByDef ??= new Dictionary<string, int>();
            }
        }
    }
}
