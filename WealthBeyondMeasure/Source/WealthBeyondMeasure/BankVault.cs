
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public class Building_BankVault : Building_Storage
    {
        public override void PostMake()
        {
            base.PostMake();
            MakeBankSilverOnly();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MakeBankSilverOnly();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                MakeBankSilverOnly();
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager == null || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            MakeBankSilverOnly();
            EjectNonCurrencyContents();
            RemoveFireFromVault();
        }


        public override string GetInspectString()
        {
            List<string> lines = new List<string>();
            string baseInspect = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseInspect))
            {
                lines.Add(baseInspect);
            }

            lines.Add("WBM_BankVaultInspect".Translate());
            lines.Add("WBM_BankVaultCurrentSilver".Translate(BankVaultUtility.TotalBankSilver(Map)));
            lines.Add("WBM_BankVaultProjectedPayout".Translate(WealthGameComponent.Instance?.GetProjectedPaydayPayoutForMap(Map) ?? 0));
            lines.Add("WBM_BankVaultNextPayday".Translate(PaydayUtility.PaydayLabel, PaydayUtility.DaysUntilNextPayday()));
            if (WealthGameComponent.Instance != null)
            {
                lines.Add(WealthGameComponent.Instance.GetBankerStatusLabel(Map));
            }

            return string.Join("\n", lines.Where(line => !string.IsNullOrEmpty(line)));
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            MakeBankSilverOnly();
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action requestBanker = new Command_Action
            {
                defaultLabel = "WBM_RequestBankerLabel".Translate(),
                defaultDesc = "WBM_RequestBankerDescription".Translate(),
                action = delegate
                {
                    IntVec3 requestCell = InteractionCell.IsValid ? InteractionCell : Position;
                    WealthGameComponent.Instance?.TryRequestBanker(Map, requestCell);
                }
            };

            string disableReason = null;
            bool canRequest = WealthGameComponent.Instance != null && WealthGameComponent.Instance.CanRequestBanker(Map, out disableReason);
            if (!canRequest)
            {
                requestBanker.Disable(disableReason ?? "WBM_BankerUnavailableNoMap".Translate());
            }

            yield return requestBanker;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.Vanish)
            {
                return;
            }

            base.Destroy(mode);
        }


        public CellRect GetVaultRect()
        {
            return GenAdj.OccupiedRect(Position, Rotation, def.Size);
        }

        private void RemoveFireFromVault()
        {
            if (Map == null)
            {
                return;
            }

            foreach (IntVec3 cell in GetVaultRect().Cells)
            {
                List<Thing> things = Map.thingGrid?.ThingsListAtFast(cell);
                if (things == null)
                {
                    continue;
                }

                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Fire fire && !fire.Destroyed)
                    {
                        fire.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private void EjectNonCurrencyContents()
        {
            if (Map == null)
            {
                return;
            }

            List<Thing> toMove = new List<Thing>();
            foreach (IntVec3 cell in GetVaultRect().Cells)
            {
                List<Thing> things = Map.thingGrid?.ThingsListAtFast(cell);
                if (things == null)
                {
                    continue;
                }

                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing == this || thing.Destroyed || thing.def == ThingDefOf.Silver || thing.def.category != ThingCategory.Item)
                    {
                        continue;
                    }

                    toMove.Add(thing);
                }
            }

            if (toMove.Count == 0)
            {
                return;
            }

            IntVec3 dropCell = InteractionCell.IsValid ? InteractionCell : Position;
            for (int i = 0; i < toMove.Count; i++)
            {
                Thing thing = toMove[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                Thing movedThing = thing;
                if (thing.Spawned)
                {
                    thing.DeSpawn();
                }

                GenPlace.TryPlaceThing(movedThing, dropCell, Map, ThingPlaceMode.Near);
            }
        }

        private void MakeBankSilverOnly()
        {
            StorageSettings storeSettings = GetStoreSettings();
            if (storeSettings?.filter == null)
            {
                return;
            }

            storeSettings.filter.SetDisallowAll();
            storeSettings.filter.SetAllow(ThingDefOf.Silver, true);
            storeSettings.Priority = StoragePriority.Critical;
        }
    }

    public static class BankVaultUtility
    {
        public static bool IsBankSilver(Thing thing)
        {
            return thing != null && thing.def == ThingDefOf.Silver && thing.MapHeld != null && GetBankVaultAt(thing.MapHeld, thing.PositionHeld) != null;
        }

        public static bool IsInSharedNonBankStockpile(Thing thing)
        {
            if (thing?.MapHeld == null || !thing.PositionHeld.IsValid)
            {
                return false;
            }

            if (GetBankVaultAt(thing.MapHeld, thing.PositionHeld) != null)
            {
                return false;
            }

            Zone zone = thing.MapHeld.zoneManager?.ZoneAt(thing.PositionHeld);
            return zone is Zone_Stockpile;
        }

        public static Building_BankVault GetBankVaultAt(Map map, IntVec3 cell)
        {
            if (map?.thingGrid == null || !cell.IsValid || !cell.InBounds(map))
            {
                return null;
            }

            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            if (things == null)
            {
                return null;
            }

            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building_BankVault vault)
                {
                    return vault;
                }
            }

            return null;
        }

        public static IEnumerable<Thing> AllBankSilver(Map map)
        {
            if (map?.listerThings == null)
            {
                yield break;
            }

            List<Thing> allSilver = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            if (allSilver == null)
            {
                yield break;
            }

            for (int i = 0; i < allSilver.Count; i++)
            {
                Thing thing = allSilver[i];
                if (IsBankSilver(thing))
                {
                    yield return thing;
                }
            }
        }

        public static IEnumerable<Building_BankVault> AllBankVaults(Map map)
        {
            if (map?.listerThings?.AllThings == null)
            {
                yield break;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                if (allThings[i] is Building_BankVault vault)
                {
                    yield return vault;
                }
            }
        }

        public static int TotalBankSilver(Map map)
        {
            int total = 0;
            foreach (Thing silver in AllBankSilver(map))
            {
                total += silver.stackCount;
            }

            return total;
        }

        public static int DepositSilverToBank(Map map, int amount, IntVec3 preferredCell)
        {
            if (map == null || amount <= 0)
            {
                return 0;
            }

            Building_BankVault vault = FindClosestBankVault(map, preferredCell) ?? AllBankVaults(map).FirstOrDefault();
            if (vault == null)
            {
                return 0;
            }

            IntVec3 targetCell = FindBestBankCell(vault, preferredCell);
            if (!targetCell.IsValid)
            {
                return 0;
            }

            int deposited = 0;
            int remaining = amount;
            while (remaining > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = Math.Min(remaining, ThingDefOf.Silver.stackLimit);
                if (!GenPlace.TryPlaceThing(silver, targetCell, map, ThingPlaceMode.Direct) && !GenPlace.TryPlaceThing(silver, targetCell, map, ThingPlaceMode.Near))
                {
                    silver.Destroy();
                    break;
                }

                deposited += silver.stackCount;
                remaining -= silver.stackCount;
            }

            return deposited;
        }

        public static void EnsureInfiniteBankSilverStock(Map map)
        {
            if (!(WealthBeyondMeasureMod.Settings?.infiniteBankSilver ?? false) || map == null)
            {
                return;
            }

            if (TotalBankSilver(map) >= 1000)
            {
                return;
            }

            Building_BankVault vault = AllBankVaults(map).FirstOrDefault();
            if (vault == null)
            {
                return;
            }

            IntVec3 cell = FindBestBankCell(vault, vault.Position);
            if (!cell.IsValid)
            {
                return;
            }

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = ThingDefOf.Silver.stackLimit;
            GenPlace.TryPlaceThing(silver, cell, map, ThingPlaceMode.Direct);
        }

        private static Building_BankVault FindClosestBankVault(Map map, IntVec3 preferredCell)
        {
            Building_BankVault best = null;
            int bestDistance = int.MaxValue;
            foreach (Building_BankVault vault in AllBankVaults(map))
            {
                if (vault == null)
                {
                    continue;
                }

                int distance = preferredCell.IsValid ? (vault.Position - preferredCell).LengthHorizontalSquared : 0;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = vault;
                }
            }

            return best;
        }

        private static IntVec3 FindBestBankCell(Building_BankVault vault, IntVec3 preferredCell)
        {
            if (vault == null)
            {
                return preferredCell;
            }

            List<IntVec3> cells = vault.GetVaultRect().Cells.ToList();
            if (cells.Count == 0)
            {
                return vault.Position;
            }

            if (!preferredCell.IsValid)
            {
                return cells[0];
            }

            return cells.OrderBy(cell => (cell - preferredCell).LengthHorizontalSquared).First();
        }
    }
}
