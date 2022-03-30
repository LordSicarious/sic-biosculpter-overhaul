using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BiosculpterOverhaul {

public class Bill_Biosculpter : Bill
{
	private BodyPartRecord part;
	public int temp_partIndexToSetLater;
	public override bool CheckIngredientsIfSociallyProper => false;
	protected override bool CanCopy => false;
	public override bool CompletableEver {
		get {
			if (recipe.targetsBodyPart && !recipe.Worker.GetPartsToApplyOn(patient, recipe).Contains(part)) {
				return false;
			}
			return true;
		}
	}
	public bool IsCurrentOperation => false;
	public float workLeft;
	public BodyPartRecord Part {
		get {
			return part;
		}
		set {
			if (billStack == null && part != null) {
				Log.Error("Can only set Bill_Biosculpter.Part after the bill has been added to the pod's bill stack.");
			}
			else {
				part = value;
			}
		}
	}
	public Building_BiosculpterPod pod => billStack.billGiver as Building_BiosculpterPod;
	public Pawn patient => pod.ContainedThing as Pawn;
	public override string Label {
		get {
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(recipe.Worker.GetLabelWhenUsedOn(patient, part));
			if (Part != null && !recipe.hideBodyPartNames) {
				stringBuilder.Append(" (" + Part.Label + ")");
			}
			return stringBuilder.ToString();
		}
	}
	public Bill_Biosculpter() {
	}
	public Bill_Biosculpter(RecipeDef recipe) : base(recipe) {
		workLeft = recipe.workAmount;
	}
	public override bool ShouldDoNow() {
		if (suspended) {
			return false;
		}
		return true;
	}
	public override void Notify_IterationCompleted(Pawn doctor, List<Thing> ingredients)
	{
		base.Notify_IterationCompleted(doctor, ingredients);
		if (CompletableEver) {
			recipe.Worker.ApplyOnPawn(patient, Part, doctor, ingredients, this);
			if (patient.RaceProps.IsFlesh) {
				patient.records.Increment(RecordDefOf.OperationsReceived);
				doctor.records.Increment(RecordDefOf.OperationsPerformed);
			}
		}
		billStack.Delete(this);
	}
	public override bool PawnAllowedToStartAnew(Pawn pawn) {
		if (!base.PawnAllowedToStartAnew(pawn))	{
			return false;
		}
		return true;
	}
	public override void ExposeData() {
		base.ExposeData();
		Scribe_BodyParts.Look(ref part, "part");
		BackCompatibility.PostExposeData(this);
	}
	public override Bill Clone() {
		Bill_Biosculpter obj = (Bill_Biosculpter)base.Clone();
		obj.part = part;
		return obj;
	}
}
}