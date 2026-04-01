using Verse;

namespace WealthBeyondMeasure
{
    public class ConsumablePriceEntry : IExposable
    {
        public string thingDefName;
        public float silverCost;

        public ConsumablePriceEntry()
        {
        }

        public ConsumablePriceEntry(string thingDefName, float silverCost)
        {
            this.thingDefName = thingDefName;
            this.silverCost = silverCost;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref silverCost, "silverCost", 0f);
        }
    }
}
