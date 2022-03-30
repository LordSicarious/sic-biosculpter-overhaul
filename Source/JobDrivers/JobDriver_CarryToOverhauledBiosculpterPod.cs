// BiosculpterOverhaul.JobDriver_CarryToOverhauledBiosculpterPod
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiosculpterOverhaul {
public class JobDriver_CarryToOverhauledBiosculpterPod : JobDriver {
	private const TargetIndex TakeeInd = TargetIndex.A;

	private const TargetIndex PodInd = TargetIndex.B;

	protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	protected Building_BiosculpterPod Pod => (Building_BiosculpterPod)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed) {
		if (pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed)) {
			return pawn.Reserve(Pod, job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils() {
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnDestroyedOrNull(TargetIndex.B);
		this.FailOnAggroMentalState(TargetIndex.A);
		this.FailOn(() => !Pod.Accepts(Takee));
		Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
			.FailOn(() => Pod.GetDirectlyHeldThings().Count > 0)
			.FailOn(() => !(Takee.Downed || Takee.IsPrisoner))
			.FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))
			.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
		Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
		yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
		yield return goToTakee;
		yield return startCarryingTakee;
		yield return goToThing;
		Toil toil = Toils_General.Wait(500, TargetIndex.B);
		toil.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
		toil.WithProgressBarToilDelay(TargetIndex.B);
		yield return toil;
		Toil toil2 = new Toil();
		toil2.initAction = delegate {
			Pod.TryAcceptThing(Takee);
		};
		toil2.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil2;
	}

	public override object[] TaleParameters() {
		return new object[2] { pawn, Takee };
	}
}
}