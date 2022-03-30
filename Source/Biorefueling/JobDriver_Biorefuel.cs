// RimWorld.JobDriver_Biorefuel
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiosculpterOverhaul {
	public class JobDriver_Biorefuel : JobDriver {
		private const TargetIndex BiorefuelableInd = TargetIndex.A;

		private const TargetIndex BiofuelInd = TargetIndex.B;

		public const int BiorefuelingDuration = 240;

		protected Thing Biorefuelable => job.GetTarget(TargetIndex.A).Thing;

		protected CompBiorefuelable BiorefuelableComp => Biorefuelable.TryGetComp<CompBiorefuelable>();

		protected Thing Biofuel => job.GetTarget(TargetIndex.B).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed) {
			if (pawn.Reserve(Biorefuelable, job, 1, -1, null, errorOnFailed)) {
				return pawn.Reserve(Biofuel, job, 1, -1, null, errorOnFailed);
			}
			return false;
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			AddEndCondition(() => (!BiorefuelableComp.IsFull) ? JobCondition.Ongoing : JobCondition.Succeeded);
			AddFailCondition(() => !job.playerForced && !BiorefuelableComp.ShouldAutoBiorefuelNowIgnoringBiofuelPct);
			AddFailCondition(() => !BiorefuelableComp.allowAutoBiorefuel && !job.playerForced);
			yield return Toils_General.DoAtomic(delegate {job.count = BiorefuelableComp.GetBiofuelCountToFullyRefuel();});
			Toil reserveBiofuel = Toils_Reserve.Reserve(TargetIndex.B);
			yield return reserveBiofuel;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveBiofuel, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
				.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
				.WithProgressBarToilDelay(TargetIndex.A);
			yield return Toils_Biorefuel.FinalizeBiorefueling(TargetIndex.A, TargetIndex.B);
		}
	}
}