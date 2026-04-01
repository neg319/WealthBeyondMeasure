using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public class WorkGiver_CollectPaycheck : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDefOf.Silver);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map == null || ShouldSkip(pawn, false))
            {
                yield break;
            }

            BankVaultUtility.EnsureInfiniteBankSilverStock(pawn.Map);
            foreach (Thing silver in BankVaultUtility.AllBankSilver(pawn.Map))
            {
                yield return silver;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == null || pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.IsPrisoner)
            {
                return true;
            }

            if (!pawn.IsColonistPlayerControlled || !PaydayUtility.IsPaydayToday())
            {
                return true;
            }

            return WealthGameComponent.Instance == null || WealthGameComponent.Instance.GetOutstandingPaydayPayout(pawn) <= 0;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || !BankVaultUtility.IsBankSilver(t))
            {
                return false;
            }

            if (WealthGameComponent.Instance == null || WealthGameComponent.Instance.GetOutstandingPaydayPayout(pawn) <= 0)
            {
                return false;
            }

            return pawn.CanReserve(t, ignoreOtherReservations: forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Job job = JobMaker.MakeJob(WBM_DefOf.WBM_CollectPaycheck, t);
            job.count = WealthGameComponent.Instance.GetOutstandingPaydayPayout(pawn);
            return job;
        }
    }
}
