using System.Linq;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public static class WalletUtility
    {
        public static int CountSilverInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return 0;
            }

            int total = 0;
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing.def == ThingDefOf.Silver)
                {
                    total += thing.stackCount;
                }
            }

            return total;
        }

        public static int RemoveSilverFromInventory(Pawn pawn, int amount)
        {
            if (pawn?.inventory?.innerContainer == null || amount <= 0)
            {
                return 0;
            }

            int removed = 0;
            List<Thing> things = pawn.inventory.innerContainer.ToList();
            for (int i = things.Count - 1; i >= 0 && removed < amount; i--)
            {
                Thing thing = things[i];
                if (thing.def != ThingDefOf.Silver)
                {
                    continue;
                }

                int take = Math.Min(amount - removed, thing.stackCount);
                Thing split = thing.SplitOff(take);
                split.Destroy();
                removed += take;
            }

            return removed;
        }

        public static int AddSilverToInventory(Pawn pawn, int amount)
        {
            if (pawn?.inventory?.innerContainer == null || amount <= 0)
            {
                return 0;
            }

            int remaining = amount;
            while (remaining > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = Math.Min(remaining, ThingDefOf.Silver.stackLimit);
                if (!pawn.inventory.innerContainer.TryAdd(silver))
                {
                    GenPlace.TryPlaceThing(silver, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    break;
                }

                remaining -= silver.stackCount;
            }

            return amount - remaining;
        }

        public static int RestoreWalletSilverFromMapOrMint(Pawn pawn, int needed)
        {
            if (pawn?.Map == null || needed <= 0)
            {
                return 0;
            }

            int restored = PullSilverFromMap(pawn, needed);
            int remaining = needed - restored;
            if (remaining > 0)
            {
                restored += AddSilverToInventory(pawn, remaining);
            }

            return restored;
        }

        private static int PullSilverFromMap(Pawn pawn, int needed)
        {
            int restored = 0;
            List<Thing> allSilver = pawn.Map.listerThings?.ThingsOfDef(ThingDefOf.Silver);
            if (allSilver == null)
            {
                return 0;
            }

            for (int i = allSilver.Count - 1; i >= 0 && restored < needed; i--)
            {
                Thing thing = allSilver[i];
                if (thing == null || thing.Destroyed || thing.ParentHolder == pawn.inventory.innerContainer)
                {
                    continue;
                }

                int take = Math.Min(needed - restored, thing.stackCount);
                Thing split = thing.SplitOff(take);
                int added = AddSilverThingToInventory(pawn, split);
                if (added < take)
                {
                    int leftover = take - added;
                    Thing refund = ThingMaker.MakeThing(ThingDefOf.Silver);
                    refund.stackCount = leftover;
                    GenPlace.TryPlaceThing(refund, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

                restored += added;
            }

            return restored;
        }

        private static int AddSilverThingToInventory(Pawn pawn, Thing silverThing)
        {
            if (pawn?.inventory?.innerContainer == null || silverThing == null || silverThing.def != ThingDefOf.Silver)
            {
                return 0;
            }

            int total = silverThing.stackCount;
            if (pawn.inventory.innerContainer.TryAdd(silverThing))
            {
                return total;
            }

            int remaining = silverThing.stackCount;
            while (remaining > 0)
            {
                Thing partial = ThingMaker.MakeThing(ThingDefOf.Silver);
                partial.stackCount = Math.Min(remaining, ThingDefOf.Silver.stackLimit);
                if (!pawn.inventory.innerContainer.TryAdd(partial))
                {
                    partial.Destroy();
                    break;
                }

                remaining -= partial.stackCount;
            }

            silverThing.Destroy();
            return total - remaining;
        }
    }
}
