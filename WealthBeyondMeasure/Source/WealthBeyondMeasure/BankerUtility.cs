using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace WealthBeyondMeasure
{
    public static class BankerUtility
    {
        public const string BankerTraderKindDefName = "WBM_BankerTrader";
        public const string BankerKindDefName = "WBM_Banker";
        public const string BankerGuardKindDefName = "WBM_BankerGuard";
        public const int BankerStayTicks = 30000;
        public const int BankerCooldownTicks = 30000;
        public const int BankerSilverReserveTarget = 200000;
        public const int BankerSilverReserveRefillFloor = 50000;

        public static bool IsBankerTrader(Pawn pawn)
        {
            return pawn?.kindDef?.trader != null && pawn.kindDef.trader.defName == BankerTraderKindDefName;
        }

        public static void DressAsBanker(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            TryWearNamed(pawn, "Apparel_TopHat");
            if (!pawn.apparel.WornApparel.Any(app => MatchesAny(app.def, "top hat", "tophat", "formal hat")))
            {
                TryWearBySearch(pawn, def => MatchesAny(def, "top hat", "tophat", "formal hat"));
            }

            TryWearNamed(pawn, "Apparel_BusinessSuit");
            if (!pawn.apparel.WornApparel.Any(app => MatchesAny(app.def, "business suit", "suit jacket", "formal jacket", "vest")))
            {
                TryWearBySearch(pawn, def => MatchesAny(def, "business suit", "suit jacket", "vest", "formal jacket"));
            }

            TryWearNamed(pawn, "Apparel_ButtonDownShirt");
            TryWearBySearch(pawn, def => MatchesAny(def, "button-down shirt", "button down shirt", "dress shirt", "shirt"));
            TryWearNamed(pawn, "Apparel_Pants");
        }

        public static void DressAsGuard(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            TryWearNamed(pawn, "Apparel_CataphractArmor");
            TryWearNamed(pawn, "Apparel_MarineArmor");
            if (!pawn.apparel.WornApparel.Any(app => MatchesAny(app.def, "cataphract armor", "marine armor", "recon armor", "plate armor", "flak vest")))
            {
                TryWearBySearch(pawn, def => MatchesAny(def, "cataphract armor", "marine armor", "recon armor", "plate armor", "flak vest"));
            }

            TryWearNamed(pawn, "Apparel_CataphractHelmet");
            TryWearNamed(pawn, "Apparel_MarineHelmet");
            if (!pawn.apparel.WornApparel.Any(app => MatchesAny(app.def, "cataphract helmet", "marine helmet", "recon helmet", "simple helmet")))
            {
                TryWearBySearch(pawn, def => MatchesAny(def, "cataphract helmet", "marine helmet", "recon helmet", "simple helmet"));
            }

            TryWearNamed(pawn, "Apparel_FlakVest");
            TryWearNamed(pawn, "Apparel_Pants");
            TryWearNamed(pawn, "Apparel_ButtonDownShirt");
        }

        public static void PrepareGuard(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            DressAsGuard(pawn);
            EnsureMarksmanLoadout(pawn);
            EnsureCombatSkills(pawn);
        }

        public static void AddSilverReserve(Pawn pawn, int totalSilver)
        {
            if (pawn?.inventory?.innerContainer == null || totalSilver <= 0)
            {
                return;
            }

            int remaining = totalSilver;
            while (remaining > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = Math.Min(remaining, ThingDefOf.Silver.stackLimit);
                pawn.inventory.innerContainer.TryAdd(silver);
                remaining -= silver.stackCount;
            }
        }

        public static void RefillSilverReserve(Pawn pawn, int targetSilver)
        {
            if (!IsBankerTrader(pawn) || targetSilver <= 0)
            {
                return;
            }

            int currentSilver = WalletUtility.CountSilverInInventory(pawn);
            if (currentSilver >= BankerSilverReserveRefillFloor)
            {
                return;
            }

            AddSilverReserve(pawn, Math.Max(0, targetSilver - currentSilver));
        }

        public static bool TrySpawnBankerParty(Map map, IntVec3 bankCell, out Pawn bankerPawn, out Pawn guardPawn)
        {
            bankerPawn = null;
            guardPawn = null;
            if (map == null)
            {
                return false;
            }

            PawnKindDef bankerKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(BankerKindDefName);
            PawnKindDef guardKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(BankerGuardKindDefName);
            if (bankerKind == null || guardKind == null)
            {
                return false;
            }

            Faction faction = FindBestHostFaction();
            bankerPawn = PawnGenerator.GeneratePawn(bankerKind, faction);
            guardPawn = PawnGenerator.GeneratePawn(guardKind, faction);
            if (bankerPawn == null || guardPawn == null)
            {
                bankerPawn = null;
                guardPawn = null;
                return false;
            }

            DressAsBanker(bankerPawn);
            PrepareGuard(guardPawn);
            AddSilverReserve(bankerPawn, BankerSilverReserveTarget);
            bankerPawn.mindState.wantsToTradeWithColony = true;

            IntVec3 spawnCell = TryFindSpawnCell(map, bankCell);
            GenSpawn.Spawn(bankerPawn, spawnCell, map, WipeMode.Vanish);

            IntVec3 guardCell = TryFindEscortSpawnCell(map, spawnCell);
            GenSpawn.Spawn(guardPawn, guardCell, map, WipeMode.Vanish);

            bankerPawn.mindState.wantsToTradeWithColony = true;
            TryAssignTradeLord(new List<Pawn> { bankerPawn, guardPawn }, map, bankCell, faction);
            return true;
        }

        public static void BeginDeparture(Pawn pawn, Map map)
        {
            if (pawn == null || pawn.Destroyed || map == null)
            {
                return;
            }

            pawn.mindState.wantsToTradeWithColony = false;

            IntVec3 exitCell = CellFinder.RandomEdgeCell(map);
            if (pawn.jobs != null)
            {
                Job leaveJob = null;
                if (DefDatabase<JobDef>.GetNamedSilentFail("LeaveMap") is JobDef leaveMap)
                {
                    leaveJob = JobMaker.MakeJob(leaveMap, exitCell);
                }
                else if (JobDefOf.Goto != null)
                {
                    leaveJob = JobMaker.MakeJob(JobDefOf.Goto, exitCell);
                }

                if (leaveJob != null)
                {
                    pawn.jobs.StartJob(leaveJob, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
                }
            }
        }

        private static void EnsureCombatSkills(Pawn pawn)
        {
            if (pawn?.skills == null)
            {
                return;
            }

            SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
            if (shooting != null)
            {
                shooting.levelInt = Math.Max(shooting.levelInt, 16);
                shooting.passion = Passion.Major;
            }

            SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
            if (melee != null)
            {
                melee.levelInt = Math.Max(melee.levelInt, 8);
            }
        }

        private static void EnsureMarksmanLoadout(Pawn pawn)
        {
            if (pawn?.equipment == null || pawn.equipment.Primary != null)
            {
                return;
            }

            ThingDef weaponDef = FindMarksmanWeaponDef();
            if (weaponDef == null)
            {
                return;
            }

            ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
            if (weapon != null)
            {
                pawn.equipment.AddEquipment(weapon);
            }
        }

        private static ThingDef FindMarksmanWeaponDef()
        {
            List<ThingDef> weaponDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def != null && def.IsWeapon)
                .ToList();

            ThingDef exact = weaponDefs.FirstOrDefault(def => MatchesAny(def, "sniper rifle", "charge lance"));
            if (exact != null)
            {
                return exact;
            }

            ThingDef fallback = weaponDefs.FirstOrDefault(def => MatchesAny(def, "bolt-action rifle", "assault rifle", "charge rifle", "heavy smg"));
            if (fallback != null)
            {
                return fallback;
            }

            return weaponDefs.FirstOrDefault();
        }

        private static Faction FindBestHostFaction()
        {
            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction != null)
            {
                Faction candidate = Find.FactionManager.AllFactionsListForReading
                    .FirstOrDefault(f => f != null && !f.IsPlayer && !f.HostileTo(playerFaction) && (f.def?.humanlikeFaction ?? false));
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f != null && !f.IsPlayer) ?? playerFaction;
        }

        private static IntVec3 TryFindSpawnCell(Map map, IntVec3 bankCell)
        {
            return CellFinder.RandomEdgeCell(map);
        }

        private static IntVec3 TryFindEscortSpawnCell(Map map, IntVec3 nearCell)
        {
            if (map == null)
            {
                return nearCell;
            }

            if (CellFinder.TryFindRandomCellNear(nearCell, map, 3, c => c.Standable(map), out IntVec3 found))
            {
                return found;
            }

            return nearCell;
        }

        private static void TryAssignTradeLord(List<Pawn> pawns, Map map, IntVec3 bankCell, Faction faction)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            try
            {
                Type lordType = typeof(LordJob_TradeWithColony);
                ConstructorInfo[] ctors = lordType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (ConstructorInfo ctor in ctors.OrderBy(c => c.GetParameters().Length))
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    object[] args = new object[parameters.Length];
                    bool valid = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType == typeof(Faction))
                        {
                            args[i] = faction;
                        }
                        else if (parameterType == typeof(IntVec3))
                        {
                            args[i] = bankCell;
                        }
                        else if (parameterType == typeof(bool))
                        {
                            args[i] = false;
                        }
                        else if (parameterType == typeof(int))
                        {
                            args[i] = 0;
                        }
                        else if (parameterType == typeof(float))
                        {
                            args[i] = 0f;
                        }
                        else
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        continue;
                    }

                    LordJob lordJob = ctor.Invoke(args) as LordJob;
                    if (lordJob != null)
                    {
                        LordMaker.MakeNewLord(faction, lordJob, map, pawns);
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        private static void TryWearNamed(Pawn pawn, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null || pawn.apparel.WornApparel.Any(app => app.def == def))
            {
                return;
            }

            Apparel apparel = ThingMaker.MakeThing(def) as Apparel;
            if (apparel == null)
            {
                return;
            }

            pawn.apparel.Wear(apparel, false);
        }

        private static void TryWearBySearch(Pawn pawn, Func<ThingDef, bool> predicate)
        {
            if (predicate == null)
            {
                return;
            }

            ThingDef found = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def != null && def.IsApparel && def.apparel != null)
                .FirstOrDefault(predicate);
            if (found != null && !pawn.apparel.WornApparel.Any(app => app.def == found))
            {
                Apparel apparel = ThingMaker.MakeThing(found) as Apparel;
                if (apparel != null)
                {
                    pawn.apparel.Wear(apparel, false);
                }
            }
        }

        private static bool MatchesAny(ThingDef def, params string[] needles)
        {
            if (def == null)
            {
                return false;
            }

            string text = ((def.defName ?? string.Empty) + " " + (def.label ?? string.Empty)).ToLowerInvariant();
            for (int i = 0; i < needles.Length; i++)
            {
                if (text.Contains(needles[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
