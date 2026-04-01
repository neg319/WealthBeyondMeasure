using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public class ThoughtWorker_WBM_WealthLifestyle : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!WageUtility.IsPawnEligible(p) || WealthGameComponent.Instance == null)
            {
                return ThoughtState.Inactive;
            }

            int rank = WealthGameComponent.Instance.GetWealthRankIndex(p);
            float score = StatusPreferenceUtility.GetCurrentLifestyleAlignmentScore(p, rank);

            if (score <= -3.5f)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            if (score <= -1.5f)
            {
                return ThoughtState.ActiveAtStage(1);
            }

            if (score >= 3.5f)
            {
                return ThoughtState.ActiveAtStage(3);
            }

            if (score >= 1.5f)
            {
                return ThoughtState.ActiveAtStage(2);
            }

            return ThoughtState.Inactive;
        }
    }
}
