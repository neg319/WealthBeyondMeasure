using Verse;

namespace WealthBeyondMeasure
{
    public class WorkTypePayRateEntry : IExposable
    {
        public string workTypeDefName;
        public float silverPerHour;

        public WorkTypePayRateEntry()
        {
        }

        public WorkTypePayRateEntry(string workTypeDefName, float silverPerHour)
        {
            this.workTypeDefName = workTypeDefName;
            this.silverPerHour = silverPerHour;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Values.Look(ref silverPerHour, "silverPerHour", 0f);
        }
    }
}
