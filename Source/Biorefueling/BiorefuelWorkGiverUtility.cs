// RimWorld.BiorefuelWorkGiverUtility
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiosculpterOverhaul {
	public static class BiorefuelWorkGiverUtility {
		public static bool CanBiorefuel(Pawn pawn, Thing t, bool forced = false) {
			CompBiorefuelable compBiorefuelable = t.TryGetComp<CompBiorefuelable>();
			if (compBiorefuelable == null || compBiorefuelable.IsFull || (!forced && !compBiorefuelable.allowAutoBiorefuel)) {
				return false;
			}
			if (compBiorefuelable.BiofuelPercentOfMax > 0f && !compBiorefuelable.Props.allowBiorefuelIfNotEmpty) {
				return false;
			}
			if (!forced && !compBiorefuelable.ShouldAutoBiorefuelNow) {
				return false;
			}
			if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced)) {
				return false;
			}
			if (t.Faction != pawn.Faction) {
				return false;
			}
			if (FindBestBiofuel(pawn, t) == null) {
				ThingFilter biofuelFilter = t.TryGetComp<CompBiorefuelable>().Props.biofuelFilter;
				JobFailReason.Is("NoFuelToBiorefuel".Translate(biofuelFilter.Summary));
				return false;
			}
			return true;
		}

		public static Thing FindBestBiofuel(Pawn pawn, Thing refuelable) {
			ThingFilter filter = refuelable.TryGetComp<CompBiorefuelable>().Props.biofuelFilter;
			Predicate<Thing> validator = delegate(Thing x) {
				if (x.IsForbidden(pawn) || !pawn.CanReserve(x))	{
					return false;
				}
				return filter.Allows(x) ? true : false;
			};
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, filter.BestThingRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
		}

		public static List<Thing> FindAllBiofuel(Pawn pawn, Thing refuelable) {
			int fuelCountToFullyBiorefuel = refuelable.TryGetComp<CompBiorefuelable>().GetBiofuelCountToFullyRefuel();
			ThingFilter filter = refuelable.TryGetComp<CompBiorefuelable>().Props.biofuelFilter;
			return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, refuelable.Position, new IntRange(fuelCountToFullyBiorefuel, fuelCountToFullyBiorefuel), (Thing t) => filter.Allows(t));
		}
	}
}