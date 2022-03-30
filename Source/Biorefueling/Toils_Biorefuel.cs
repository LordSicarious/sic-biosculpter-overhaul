// RimWorld.Toils_Biorefuel
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiosculpterOverhaul {
	public class Toils_Biorefuel {
		public static Toil FinalizeBiorefueling(TargetIndex refuelableInd, TargetIndex fuelInd) {
			Toil toil = new Toil();
			toil.initAction = delegate {
				Job curJob = toil.actor.CurJob;
				Thing thing = curJob.GetTarget(refuelableInd).Thing;
				if (toil.actor.CurJob.placedThings.NullOrEmpty()) {
					thing.TryGetComp<CompBiorefuelable>().Biorefuel(new List<Thing> { curJob.GetTarget(fuelInd).Thing });
				}
				else {
					thing.TryGetComp<CompBiorefuelable>().Biorefuel(toil.actor.CurJob.placedThings.Select((ThingCountClass p) => p.thing).ToList());
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}