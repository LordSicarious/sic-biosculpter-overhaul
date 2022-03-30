// RimWorld.WorkGiver_Biorefuel
using RimWorld;
using Verse;
using Verse.AI;

namespace BiosculpterOverhaul {
	public class WorkGiver_Biorefuel : WorkGiver_Scanner {
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Refuelable);
		public override PathEndMode PathEndMode => PathEndMode.Touch;
		public virtual JobDef biorefuel => BiosculpterOverhaulDefOf.Biorefuel;
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			return BiorefuelWorkGiverUtility.CanBiorefuel(pawn, t, forced);
		}
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			Thing thing = BiorefuelWorkGiverUtility.FindBestBiofuel(pawn, t);
			return JobMaker.MakeJob(biorefuel, t, thing);
		}
	}
}