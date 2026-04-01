using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace WealthBeyondMeasure
{
    public class JobDriver_CollectPaycheck : JobDriver
    {
        private const TargetIndex SilverIndex = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(SilverIndex), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(SilverIndex);
            this.FailOn(() => !PaydayUtility.IsPaydayToday());
            this.FailOn(() => WealthGameComponent.Instance == null || WealthGameComponent.Instance.GetOutstandingPaydayPayout(pawn) <= 0);

            yield return Toils_Goto.GotoThing(SilverIndex, PathEndMode.ClosestTouch);

            Toil takeSilver = new Toil();
            takeSilver.initAction = delegate
            {
                Thing thing = job.GetTarget(SilverIndex).Thing;
                if (thing == null || thing.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int pending = WealthGameComponent.Instance.GetOutstandingPaydayPayout(pawn);
                if (pending <= 0)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                if (WealthBeyondMeasureMod.Settings?.infiniteBankSilver ?? false)
                {
                    BankVaultUtility.EnsureInfiniteBankSilverStock(pawn.Map);
                    if (thing.stackCount < pending)
                    {
                        thing.stackCount = System.Math.Max(thing.stackCount, pending);
                    }
                }

                int count = job.count > 0 ? System.Math.Min(job.count, pending) : pending;
                count = System.Math.Min(count, thing.stackCount);
                if (count <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing taken = thing.SplitOff(count);
                if (!pawn.inventory.innerContainer.TryAdd(taken))
                {
                    GenPlace.TryPlaceThing(taken, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                WealthGameComponent.Instance.ResolveCollectedPaydaySilver(pawn, count, PaydayUtility.CurrentDay);
            };
            takeSilver.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return takeSilver;
        }
    }
}
