using RimWorld;
using Verse;

namespace WealthBeyondMeasure
{
    public class BankerVisitRecord : IExposable
    {
        public int mapId = -1;
        public int departureTick = -1;
        public int requestCooldownUntilTick = -1;
        public int bankerSilverBuffer = 200000;
        public Pawn bankerPawn;
        public Pawn guardPawn;

        public bool ActiveNow =>
            (bankerPawn != null && !bankerPawn.Destroyed) ||
            (guardPawn != null && !guardPawn.Destroyed);

        public void ExposeData()
        {
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref departureTick, "departureTick", -1);
            Scribe_Values.Look(ref requestCooldownUntilTick, "requestCooldownUntilTick", -1);
            Scribe_Values.Look(ref bankerSilverBuffer, "bankerSilverBuffer", 200000);
            Scribe_References.Look(ref bankerPawn, "bankerPawn");
            Scribe_References.Look(ref guardPawn, "guardPawn");
        }
    }
}
