using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    [DefOf]
    public static class WBM_DefOf
    {
        public static JobDef WBM_CollectPaycheck;
        public static ThoughtDef WBM_GotPaidThought;
        public static ThoughtDef WBM_LostMoneyThought;
        public static ThoughtDef WBM_WealthLifestyleThought;
        public static ThoughtDef WBM_AteStatusMealThought;
        public static ThoughtDef WBM_AteBadStatusMealThought;
        public static TraitDef WBM_HighMaintenance;
        public static TraitDef WBM_Frugal;
        public static ThingDef WBM_BankVault;

        static WBM_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WBM_DefOf));
        }
    }
}
