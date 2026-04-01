using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WealthBeyondMeasure
{
    public class WealthGameComponent : GameComponent
    {
        private Dictionary<int, PawnAccountRecord> pawnAccounts = new Dictionary<int, PawnAccountRecord>();
        private List<BankerVisitRecord> bankerVisits = new List<BankerVisitRecord>();
        private int lastObservedDay = -1;

        public WealthGameComponent(Game game)
        {
        }

        public static WealthGameComponent Instance => Current.Game?.GetComponent<WealthGameComponent>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref pawnAccounts, "pawnAccounts", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref bankerVisits, "bankerVisits", LookMode.Deep);
            Scribe_Values.Look(ref lastObservedDay, "lastObservedDay", -1);

            if (pawnAccounts == null)
            {
                pawnAccounts = new Dictionary<int, PawnAccountRecord>();
            }

            if (bankerVisits == null)
            {
                bankerVisits = new List<BankerVisitRecord>();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            TickActiveWork();
            TickBankers();

            int currentDay = PaydayUtility.CurrentDay;
            if (currentDay != lastObservedDay)
            {
                lastObservedDay = currentDay;
                if (PaydayUtility.IsPaydayToday(currentDay) && Find.AnyPlayerHomeMap != null)
                {
                    PrepareAllPawnsForPayday(currentDay);
                    Messages.Message("WBM_PaydayMessage".Translate(), MessageTypeDefOf.PositiveEvent, historical: false);
                }
            }

            if (Find.TickManager.TicksGame % 250 == 0)
            {
                SyncWalletsToInventories();
            }

            if (Find.TickManager.TicksGame % 9000 == 0)
            {
                RunWeaponPreferencePass();
            }

            if (Find.TickManager.TicksGame % 15000 == 0)
            {
                RunBedUpgradePass();
            }
        }

        public BankerVisitRecord GetBankerVisit(Map map)
        {
            if (map == null)
            {
                return null;
            }

            bankerVisits ??= new List<BankerVisitRecord>();
            BankerVisitRecord visit = bankerVisits.FirstOrDefault(x => x != null && x.mapId == map.uniqueID);
            if (visit == null)
            {
                visit = new BankerVisitRecord { mapId = map.uniqueID };
                bankerVisits.Add(visit);
            }

            return visit;
        }

        public string GetBankerStatusLabel(Map map)
        {
            BankerVisitRecord visit = GetBankerVisit(map);
            if (visit == null)
            {
                return string.Empty;
            }

            if (visit.ActiveNow)
            {
                int hoursLeft = Mathf.Max(0, Mathf.CeilToInt((visit.departureTick - Find.TickManager.TicksGame) / 2500f));
                return "WBM_BankVaultBankerActive".Translate(hoursLeft);
            }

            int cooldownTicks = Math.Max(0, visit.requestCooldownUntilTick - Find.TickManager.TicksGame);
            if (cooldownTicks > 0)
            {
                int hours = Mathf.CeilToInt(cooldownTicks / 2500f);
                return "WBM_BankVaultBankerCooldown".Translate(hours);
            }

            return "WBM_BankVaultBankerReady".Translate();
        }

        public bool CanRequestBanker(Map map, out string disableReason)
        {
            disableReason = null;
            if (map == null)
            {
                disableReason = "WBM_BankerUnavailableNoMap".Translate();
                return false;
            }

            if (!BankVaultUtility.AllBankVaults(map).Any())
            {
                disableReason = "WBM_BankerUnavailableNoBank".Translate();
                return false;
            }

            BankerVisitRecord visit = GetBankerVisit(map);
            if (visit != null && visit.ActiveNow)
            {
                disableReason = "WBM_BankerUnavailableActive".Translate();
                return false;
            }

            if (visit != null && Find.TickManager.TicksGame < visit.requestCooldownUntilTick)
            {
                int hours = Mathf.CeilToInt((visit.requestCooldownUntilTick - Find.TickManager.TicksGame) / 2500f);
                disableReason = "WBM_BankerUnavailableCooldown".Translate(hours);
                return false;
            }

            return true;
        }

        public bool TryRequestBanker(Map map, IntVec3 preferredCell)
        {
            if (!CanRequestBanker(map, out string disableReason))
            {
                if (!disableReason.NullOrEmpty())
                {
                    Messages.Message(disableReason, MessageTypeDefOf.RejectInput, historical: false);
                }

                return false;
            }

            BankerVisitRecord visit = GetBankerVisit(map);
            if (visit == null)
            {
                return false;
            }

            if (!BankerUtility.TrySpawnBankerParty(map, preferredCell, out Pawn bankerPawn, out Pawn guardPawn))
            {
                Messages.Message("WBM_BankerSpawnFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            visit.bankerPawn = bankerPawn;
            visit.guardPawn = guardPawn;
            visit.departureTick = Find.TickManager.TicksGame + BankerUtility.BankerStayTicks;
            visit.requestCooldownUntilTick = visit.departureTick + BankerUtility.BankerCooldownTicks;
            visit.bankerSilverBuffer = BankerUtility.BankerSilverReserveTarget;
            Messages.Message("WBM_BankerRequested".Translate(), bankerPawn, MessageTypeDefOf.PositiveEvent, historical: false);
            return true;
        }

        public PawnAccountRecord GetAccount(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (!pawnAccounts.TryGetValue(pawn.thingIDNumber, out PawnAccountRecord record) || record == null)
            {
                record = new PawnAccountRecord();
                record.expectedWalletSilver = WalletUtility.CountSilverInInventory(pawn);
                pawnAccounts[pawn.thingIDNumber] = record;
            }

            return record;
        }

        public int GetOutstandingPaydayPayout(Pawn pawn)
        {
            return GetAccount(pawn)?.currentPaydayPayoutSilver ?? 0;
        }

        public int GetProjectedPaydayPayout(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            PawnAccountRecord record = GetAccount(pawn);
            if (record == null)
            {
                return 0;
            }

            int currentDay = PaydayUtility.CurrentDay;
            if (PaydayUtility.IsPaydayToday(currentDay) && record.lastSettlementDay == currentDay)
            {
                return Math.Max(0, record.currentPaydayPayoutSilver);
            }

            int grossWhole = Mathf.CeilToInt((float)record.cycleGrossSilver);
            int remainingExpenseWhole = Math.Max(0, Mathf.CeilToInt((float)record.cycleExpenseSilver) - record.cycleExpensePaidSilverWhole);
            int expenseWhole = remainingExpenseWhole + Math.Max(0, record.rolloverDebtSilver) + Math.Max(0, record.currentPaydayDeductionSilver);
            int projectedNet = grossWhole - expenseWhole;
            return Math.Max(0, record.currentPaydayPayoutSilver + projectedNet);
        }

        public int GetProjectedPaydayPayoutForMap(Map map)
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            int total = 0;
            HashSet<int> counted = new HashSet<int>();
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (!WageUtility.IsPawnEligible(pawn) || !counted.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                total += GetProjectedPaydayPayout(pawn);
            }

            return total;
        }

        public int GetWalletSilver(Pawn pawn)
        {
            float balance = GetEffectiveWealthBalance(pawn);
            return balance >= 0f ? Mathf.FloorToInt(balance) : Mathf.CeilToInt(balance);
        }

        public float GetEffectiveWealthBalance(Pawn pawn)
        {
            return GetHouseholdCarriedSilver(pawn) - GetHouseholdOutstandingLiability(pawn);
        }

        public int GetHouseholdCarriedSilver(Pawn pawn)
        {
            return GetFinancialProviders(pawn).Sum(member => WalletUtility.CountSilverInInventory(member));
        }

        public float GetHouseholdOutstandingLiability(Pawn pawn)
        {
            float total = 0f;
            foreach (Pawn member in GetFinancialProviders(pawn))
            {
                PawnAccountRecord record = GetAccount(member);
                if (record == null)
                {
                    continue;
                }

                total += Math.Max(0f, (float)record.cycleExpenseSilver - record.cycleExpensePaidSilverWhole);
                total += Math.Max(0, record.rolloverDebtSilver);
                total += Math.Max(0, record.currentPaydayDeductionSilver);
            }

            return total;
        }

        public float GetCommunityAssistanceMultiplier(Pawn pawn)
        {
            return WealthRankUtility.GetCommunityAssistanceMultiplier(GetWealthRankIndex(pawn));
        }

        public string GetCommunityAssistanceLabel(Pawn pawn)
        {
            return (GetCommunityAssistanceMultiplier(pawn) * 100f).ToString("0") + "%";
        }

        public float GetCurrentGrossCycleSilver(Pawn pawn)
        {
            return (float)(GetAccount(pawn)?.cycleGrossSilver ?? 0d);
        }

        public float GetCurrentExpenseCycleSilver(Pawn pawn)
        {
            return (float)(GetAccount(pawn)?.cycleExpenseSilver ?? 0d);
        }

        public float GetCurrentNetCycleSilver(Pawn pawn)
        {
            PawnAccountRecord record = GetAccount(pawn);
            if (record == null)
            {
                return 0f;
            }

            return (float)(record.cycleGrossSilver - record.cycleExpenseSilver);
        }

        public float GetLifetimeEarnedSilver(Pawn pawn)
        {
            return (float)(GetAccount(pawn)?.lifetimeGrossSilver ?? 0d);
        }

        public float GetLifetimeSpentSilver(Pawn pawn)
        {
            return (float)(GetAccount(pawn)?.lifetimeExpenseSilver ?? 0d);
        }

        public int GetWealthRankIndex(Pawn pawn)
        {
            return WealthRankUtility.GetRankIndex(GetWalletSilver(pawn));
        }

        public string GetWealthRankLabel(Pawn pawn)
        {
            return WealthRankUtility.GetRankLabel(GetWalletSilver(pawn));
        }

        public int GetSocialWealthOpinionOffset(Pawn pawn)
        {
            return SocialWealthUtility.GetWealthPrestigeOpinionOffset(pawn);
        }

        public string GetLifestyleStatus(Pawn pawn)
        {
            if (pawn == null)
            {
                return "WBM_Lifestyle_Neutral".Translate();
            }

            return StatusPreferenceUtility.DescribeLifestyle(pawn, GetWealthRankIndex(pawn));
        }

        public string GetWorkBreakdownSummary(Pawn pawn, int maxLines = 8)
        {
            PawnAccountRecord record = GetAccount(pawn);
            if (record == null || record.cycleWorkedTicksByType == null || record.cycleWorkedTicksByType.Count == 0)
            {
                return "WBM_NoPaidWorkYet".Translate();
            }

            List<string> lines = new List<string>();
            foreach (var pair in record.cycleWorkedTicksByType.OrderByDescending(x => x.Value).Take(maxLines))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key);
                string label = workType?.labelShort?.CapitalizeFirst() ?? workType?.label?.CapitalizeFirst() ?? pair.Key;
                float hours = WageUtility.TicksToHours(pair.Value);
                lines.Add(label + ": " + hours.ToString("0.0") + "h");
            }

            return string.Join("
", lines);
        }

        public void AddWorkedTick(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn == null || workType == null)
            {
                return;
            }

            PawnAccountRecord record = GetAccount(pawn);
            float silverPerTick = WageUtility.GetSilverPerTick(workType);
            if (silverPerTick <= 0f)
            {
                return;
            }

            record.AddWorkedTick(workType.defName, silverPerTick);
        }

        public float GetAdjustedConsumableCost(Pawn pawn, ThingDef thingDef, int count = 1)
        {
            if (thingDef == null || count <= 0)
            {
                return 0f;
            }

            float baseUnitCost = WealthBeyondMeasureMod.Settings?.GetConsumableCost(thingDef.defName) ?? 0f;
            if (baseUnitCost <= 0f)
            {
                return 0f;
            }

            return baseUnitCost * GetCommunityAssistanceMultiplier(pawn) * count;
        }

        public void RegisterConsumableTakenToInventory(Pawn pawn, Thing consumable)
        {
            if (pawn == null || consumable?.def == null || consumable.stackCount <= 0)
            {
                return;
            }

            float totalCost = GetAdjustedConsumableCost(pawn, consumable.def, consumable.stackCount);
            if (totalCost <= 0f)
            {
                return;
            }

            AddExpense(pawn, totalCost, consumable.def.defName, consumable.stackCount);
        }

        public void RegisterConsumableDirectUse(Pawn pawn, ThingDef thingDef, int count = 1)
        {
            if (pawn == null || thingDef == null || count <= 0)
            {
                return;
            }

            float totalCost = GetAdjustedConsumableCost(pawn, thingDef, count);
            if (totalCost <= 0f)
            {
                return;
            }

            AddExpense(pawn, totalCost);
        }

        public bool TryUsePrepaidConsumable(Pawn pawn, ThingDef thingDef, int count = 1)
        {
            PawnAccountRecord record = GetAccount(pawn);
            return record?.TryConsumePrepaidConsumable(thingDef?.defName, count) ?? false;
        }

        public void AddExpense(Pawn pawn, float silverCost, string prepaidConsumableDefName = null, int prepaidCount = 0)
        {
            if (pawn == null || silverCost <= 0f)
            {
                return;
            }

            PawnAccountRecord record = GetAccount(pawn);
            record.cycleExpenseSilver += silverCost;
            record.lifetimeExpenseSilver += silverCost;

            if (!string.IsNullOrEmpty(prepaidConsumableDefName) && prepaidCount > 0)
            {
                record.AddPrepaidConsumables(prepaidConsumableDefName, prepaidCount);
            }

            ChargeWholeSilverExpenseNow(pawn, record);
        }

        public void ResolveCollectedPaydaySilver(Pawn pawn, int amountCollected, int currentDay)
        {
            if (pawn == null || amountCollected <= 0)
            {
                return;
            }

            PawnAccountRecord record = GetAccount(pawn);
            record.currentPaydayPayoutSilver = Math.Max(0, record.currentPaydayPayoutSilver - amountCollected);
            record.expectedWalletSilver += amountCollected;
            GainPaydayThought(pawn, currentDay);
        }

        public void PrepareAllPawnsForPayday(int currentDay)
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    PreparePawnForPayday(pawn, currentDay);
                }
            }
        }

        public void PreparePawnForPayday(Pawn pawn, int currentDay)
        {
            if (!WageUtility.IsPawnEligible(pawn))
            {
                return;
            }

            PawnAccountRecord record = GetAccount(pawn);
            if (record == null || record.lastSettlementDay == currentDay)
            {
                return;
            }

            int grossWhole = Mathf.CeilToInt((float)record.cycleGrossSilver);
            int remainingExpenseWhole = Math.Max(0, Mathf.CeilToInt((float)record.cycleExpenseSilver) - record.cycleExpensePaidSilverWhole);
            int expenseWhole = remainingExpenseWhole + record.rolloverDebtSilver;
            int net = grossWhole - expenseWhole;

            record.currentPaydayPayoutSilver += Math.Max(0, net);
            record.currentPaydayDeductionSilver = Math.Max(0, -net);
            record.rolloverDebtSilver = 0;
            record.lastSettlementDay = currentDay;
            record.cycleGrossSilver = 0d;
            record.cycleExpenseSilver = 0d;
            record.cycleExpensePaidSilverWhole = 0;
            record.cycleWorkedTicksByType.Clear();

            if (record.currentPaydayDeductionSilver > 0)
            {
                ApplyPaydayDeduction(pawn, currentDay);
            }
        }

        public void ApplyPaydayDeduction(Pawn pawn, int currentDay)
        {
            PawnAccountRecord record = GetAccount(pawn);
            if (pawn == null || record == null || record.currentPaydayDeductionSilver <= 0)
            {
                return;
            }

            int removed = RemoveSilverFromHousehold(pawn, record.currentPaydayDeductionSilver);
            if (removed > 0 && pawn.Map != null)
            {
                BankVaultUtility.DepositSilverToBank(pawn.Map, removed, pawn.Position);
            }

            int remaining = Math.Max(0, record.currentPaydayDeductionSilver - removed);
            record.rolloverDebtSilver += remaining;
            record.currentPaydayDeductionSilver = 0;
            GainLossThought(pawn, currentDay);
        }

        public void SyncWalletsToInventories()
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                if (WealthBeyondMeasureMod.Settings?.infiniteBankSilver ?? false)
                {
                    BankVaultUtility.EnsureInfiniteBankSilverStock(map);
                }

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    SyncWalletToInventory(pawn);
                }
            }
        }

        public void SyncWalletToInventory(Pawn pawn)
        {
            if (!WageUtility.IsPawnEligible(pawn))
            {
                return;
            }

            PawnAccountRecord record = GetAccount(pawn);
            int actual = WalletUtility.CountSilverInInventory(pawn);
            if (actual > record.expectedWalletSilver)
            {
                record.expectedWalletSilver = actual;
                return;
            }

            int missing = record.expectedWalletSilver - actual;
            if (missing > 0)
            {
                WalletUtility.RestoreWalletSilverFromMapOrMint(pawn, missing);
            }
        }

        private void TickActiveWork()
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (WageUtility.TryGetActivePaidWorkType(pawn, out WorkTypeDef workType))
                    {
                        AddWorkedTick(pawn, workType);
                    }
                }
            }
        }

        private void TickBankers()
        {
            if (bankerVisits == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = bankerVisits.Count - 1; i >= 0; i--)
            {
                BankerVisitRecord visit = bankerVisits[i];
                if (visit == null)
                {
                    bankerVisits.RemoveAt(i);
                    continue;
                }

                Pawn banker = visit.bankerPawn;
                Pawn guard = visit.guardPawn;
                bool bankerActive = banker != null && !banker.Destroyed;
                bool guardActive = guard != null && !guard.Destroyed;
                if (!bankerActive && !guardActive)
                {
                    visit.bankerPawn = null;
                    visit.guardPawn = null;
                    continue;
                }

                Map map = banker?.MapHeld ?? guard?.MapHeld;
                if (bankerActive && banker.MapHeld != null)
                {
                    BankerUtility.RefillSilverReserve(banker, visit.bankerSilverBuffer);
                }

                if (visit.departureTick > 0 && now >= visit.departureTick)
                {
                    if (map != null)
                    {
                        BankerUtility.BeginDeparture(banker, map);
                        BankerUtility.BeginDeparture(guard, map);
                    }

                    if (now >= visit.departureTick + 2500 || map == null)
                    {
                        if (bankerActive)
                        {
                            banker.Destroy(DestroyMode.Vanish);
                        }

                        if (guardActive)
                        {
                            guard.Destroy(DestroyMode.Vanish);
                        }

                        visit.bankerPawn = null;
                        visit.guardPawn = null;
                        Messages.Message("WBM_BankerDeparted".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
                    }
                }
            }
        }

        private void ChargeWholeSilverExpenseNow(Pawn pawn, PawnAccountRecord record)
        {
            if (pawn == null || record == null)
            {
                return;
            }

            int shouldBePaidWhole = Mathf.FloorToInt((float)record.cycleExpenseSilver);
            int dueNow = Math.Max(0, shouldBePaidWhole - record.cycleExpensePaidSilverWhole);
            if (dueNow <= 0)
            {
                return;
            }

            int removed = RemoveSilverFromHousehold(pawn, dueNow);
            if (removed <= 0)
            {
                return;
            }

            record.cycleExpensePaidSilverWhole += removed;
            if (pawn.Map != null)
            {
                BankVaultUtility.DepositSilverToBank(pawn.Map, removed, pawn.Position);
            }
        }

        private int RemoveSilverFromHousehold(Pawn pawn, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int removed = 0;
            foreach (Pawn member in GetFinancialProviders(pawn))
            {
                if (removed >= amount)
                {
                    break;
                }

                int fromMember = WalletUtility.RemoveSilverFromInventory(member, amount - removed);
                if (fromMember <= 0)
                {
                    continue;
                }

                removed += fromMember;
                PawnAccountRecord memberRecord = GetAccount(member);
                if (memberRecord != null)
                {
                    memberRecord.expectedWalletSilver = Math.Max(0, memberRecord.expectedWalletSilver - fromMember);
                }
            }

            return removed;
        }

        public IEnumerable<Pawn> GetFinancialProviders(Pawn pawn)
        {
            if (pawn == null)
            {
                yield break;
            }

            HashSet<int> yielded = new HashSet<int>();

            if (!IsAdultForWealth(pawn))
            {
                bool foundParentProvider = false;
                foreach (Pawn parent in GetParents(pawn))
                {
                    if (parent == null)
                    {
                        continue;
                    }

                    foundParentProvider = true;
                    foreach (Pawn provider in GetAdultHouseholdMembers(parent))
                    {
                        if (provider != null && yielded.Add(provider.thingIDNumber))
                        {
                            yield return provider;
                        }
                    }
                }

                if (foundParentProvider)
                {
                    yield break;
                }
            }

            foreach (Pawn member in GetAdultHouseholdMembers(pawn))
            {
                if (member != null && yielded.Add(member.thingIDNumber))
                {
                    yield return member;
                }
            }
        }

        public IEnumerable<Pawn> GetAdultHouseholdMembers(Pawn pawn)
        {
            if (pawn == null)
            {
                yield break;
            }

            yield return pawn;

            Pawn spouse = GetMarriedPartner(pawn);
            if (spouse != null && spouse != pawn)
            {
                yield return spouse;
            }
        }

        public IEnumerable<Pawn> GetParents(Pawn pawn)
        {
            if (pawn?.relations?.DirectRelations == null)
            {
                yield break;
            }

            HashSet<int> yielded = new HashSet<int>();
            foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
            {
                string defName = relation?.def?.defName ?? string.Empty;
                if (string.IsNullOrEmpty(defName))
                {
                    continue;
                }

                if (defName.IndexOf("Parent", StringComparison.OrdinalIgnoreCase) < 0 &&
                    !defName.Equals("Mother", StringComparison.OrdinalIgnoreCase) &&
                    !defName.Equals("Father", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Pawn parent = relation.otherPawn;
                if (!WageUtility.IsPawnEligible(parent))
                {
                    continue;
                }

                if (yielded.Add(parent.thingIDNumber))
                {
                    yield return parent;
                }
            }
        }

        public bool IsAdultForWealth(Pawn pawn)
        {
            if (pawn?.ageTracker == null)
            {
                return true;
            }

            return pawn.ageTracker.AgeBiologicalYears >= 18;
        }

        public Pawn GetMarriedPartner(Pawn pawn)
        {
            if (pawn?.relations?.DirectRelations == null)
            {
                return null;
            }

            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel == null || rel.def != PawnRelationDefOf.Spouse)
                {
                    continue;
                }

                Pawn spouse = rel.otherPawn;
                if (WageUtility.IsPawnEligible(spouse))
                {
                    return spouse;
                }
            }

            return null;
        }

        private void GainPaydayThought(Pawn pawn, int currentDay)
        {
            PawnAccountRecord record = GetAccount(pawn);
            if (pawn?.needs?.mood?.thoughts?.memories == null || record == null || record.lastPositiveMoodDay == currentDay)
            {
                return;
            }

            pawn.needs.mood.thoughts.memories.TryGainMemory(WBM_DefOf.WBM_GotPaidThought);
            record.lastPositiveMoodDay = currentDay;
        }

        private void GainLossThought(Pawn pawn, int currentDay)
        {
            PawnAccountRecord record = GetAccount(pawn);
            if (pawn?.needs?.mood?.thoughts?.memories == null || record == null || record.lastNegativeMoodDay == currentDay)
            {
                return;
            }

            pawn.needs.mood.thoughts.memories.TryGainMemory(WBM_DefOf.WBM_LostMoneyThought);
            record.lastNegativeMoodDay = currentDay;
        }

        private void RunWeaponPreferencePass()
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    TryUpgradeWeaponForPawn(pawn);
                }
            }
        }

        private void TryUpgradeWeaponForPawn(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.equipment == null || pawn.jobs == null)
            {
                return;
            }

            if (pawn.Drafted || pawn.Downed || pawn.InMentalState || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return;
            }

            int rank = GetWealthRankIndex(pawn);
            ThingWithComps currentWeapon = pawn.equipment.Primary;
            float currentScore = StatusPreferenceUtility.ScoreWeaponForPawn(pawn, currentWeapon, rank);
            ThingWithComps bestWeapon = currentWeapon;
            float bestScore = currentScore;

            foreach (Thing thing in pawn.Map.listerThings.AllThings)
            {
                if (!(thing is ThingWithComps candidate) || candidate == currentWeapon || candidate.def == null || !candidate.def.IsWeapon)
                {
                    continue;
                }

                if (!candidate.Spawned || candidate.IsForbidden(pawn) || !pawn.CanReserveAndReach(candidate, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }

                float score = StatusPreferenceUtility.ScoreWeaponForPawn(pawn, candidate, rank);
                if (score > bestScore + 1.1f)
                {
                    bestScore = score;
                    bestWeapon = candidate;
                }
            }

            if (bestWeapon != null && bestWeapon != currentWeapon)
            {
                Job job = JobMaker.MakeJob(JobDefOf.Equip, bestWeapon);
                pawn.jobs.TryTakeOrderedJob(job);
            }
        }

        private void RunBedUpgradePass()
        {
            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    TryUpgradeBedForPawn(pawn);
                }
            }
        }

        private void TryUpgradeBedForPawn(Pawn pawn)
        {
            if (pawn?.ownership == null || pawn.Map == null)
            {
                return;
            }

            int wealthRank = GetWealthRankIndex(pawn);
            int preferenceRank = TraitPreferenceUtility.GetPreferenceRank(pawn, wealthRank);
            if (preferenceRank < 2)
            {
                return;
            }

            Building_Bed currentBed = pawn.ownership.OwnedBed;
            float currentScore = ScoreBed(pawn, currentBed, wealthRank);
            float requiredUpgrade = 0.1f + preferenceRank * 0.08f;
            Building_Bed bestBed = currentBed;
            float bestScore = currentScore;

            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (!(building is Building_Bed bed))
                {
                    continue;
                }

                if (!bed.Spawned || bed.Medical || bed.ForPrisoners || bed.SleepingSlotsCount <= 0)
                {
                    continue;
                }

                if (!bed.def.building.bed_humanlike)
                {
                    continue;
                }

                if (bed.OwnersForReading != null && bed.OwnersForReading.Any(owner => owner != pawn) && bed.OwnersForReading.Count >= bed.SleepingSlotsCount)
                {
                    continue;
                }

                float score = ScoreBed(pawn, bed, wealthRank);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestBed = bed;
                }
            }

            if (bestBed != null && bestBed != currentBed && bestScore > currentScore + requiredUpgrade)
            {
                pawn.ownership.ClaimBedIfNonMedical(bestBed);
            }
        }

        private static float ScoreBed(Pawn pawn, Building_Bed bed, int rank)
        {
            return StatusPreferenceUtility.ScoreBedForPawn(pawn, bed, rank);
        }
    }
}
