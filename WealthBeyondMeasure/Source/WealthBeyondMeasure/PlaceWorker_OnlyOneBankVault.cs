using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public class PlaceWorker_OnlyOneBankVault : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (map == null)
            {
                return true;
            }

            foreach (Building_BankVault vault in BankVaultUtility.AllBankVaults(map))
            {
                if (vault == null || vault.Destroyed)
                {
                    continue;
                }

                if (thingToIgnore != null && vault == thingToIgnore)
                {
                    continue;
                }

                return "WBM_OnlyOneBankVault".Translate();
            }

            return true;
        }
    }
}
