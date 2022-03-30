// BiosculpterOverhaul.Building_BiosculpterPod
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;


namespace BiosculpterOverhaul {
    public class Building_BiosculpterPod : Building_Casket, ISuspendableThingHolder, IThingHolderWithDrawnPawn , IBillGiver, IBillGiverWithTickAction {

        public BillStack billStack;
        public BillStack BillStack => billStack;

        public IntVec3 BillInteractionCell => InteractionCell;

        public IEnumerable<IntVec3> IngredientStackCells => GenAdj.CellsOccupiedBy(this);
		private const int EjectAfterTicksWithoutPower = 4000;
        private int TicksWithoutPower = 0;
        public bool IsContentsSuspended {
            get {return IsOccupied && IsPowered;}
        }
        public Pawn Occupant => ContainedThing as Pawn;
        public bool IsWorking => false;
        public bool IsOccupied => Occupant != null;
		public bool IsPowered => powerTraderComp.PowerOn;
        public bool IsBiofuelled => biorefuelableComp.HasBiofuel;
		public float HeldPawnDrawPos_Y => DrawPos.y - 3f / 74f;
		public float HeldPawnBodyAngle => Rotation.Opposite.AsAngle;
		public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public CompPowerTrader powerTraderComp => GetComp<CompPowerTrader>();
        public CompBiorefuelable biorefuelableComp => GetComp<CompBiorefuelable>();

        public float powerConsumptionActive => GetComp<CompPowerTrader>().Props.basePowerConsumption;
        public float powerConsumptionStandby => GetComp<CompBiosculpterPod>().Props.powerConsumptionStandby;

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true) {
            if (base.TryAcceptThing(thing, allowSpecialEffects)) {
                if (allowSpecialEffects) {
                    BiosculpterOverhaulDefOf.BiosculpterPod_Enter.PlayOneShot(new TargetInfo(base.Position, base.Map));
                }
                return true;
            }
            return false;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn) {
            if (myPawn.IsQuestLodger()) {
                yield return new FloatMenuOption("CannotUseReason".Translate("OverhauledBiosculpterPodGuestsNotAllowed".Translate()), null);
                yield break;
            }
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(myPawn)) {
                yield return floatMenuOption;
            }
            if (innerContainer.Count != 0) {
                yield break;
            }
            if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly)) {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
                yield break;
            }
            if (!IsPowered) {
                yield return new FloatMenuOption("CannotUseReason".Translate("OverhauledBiosculpterPodUnpowered".Translate()), null);
                yield break;
            }
            JobDef jobDef = BiosculpterOverhaulDefOf.EnterBiosculpterPod;
            string label = "EnterOverhauledBiosculpterPod".Translate();
            Action action = delegate {
                Job job = JobMaker.MakeJob(jobDef, this);
                myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), myPawn, this);
        }

        public override IEnumerable<Gizmo> GetGizmos() {
            foreach (Gizmo gizmo in base.GetGizmos()) {
                yield return gizmo;
            }
            // Button to open Biosculpting UI Dialog
            Command_Action command_Action = new Command_Action();
            command_Action.action = new System.Action(delegate {Find.WindowStack.Add(new Dialog_Biosculpting(this));});
            command_Action.defaultLabel = "CommandSculptPawn".Translate();
            command_Action.defaultDesc = "CommandSculptPawnDescEmpty".Translate();
            if (innerContainer.Count == 0) {
                command_Action.Disable("CommandSculptPawnFailEmpty".Translate());
            }
            command_Action.hotKey = KeyBindingDefOf.Misc8;
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");
            yield return command_Action;
    }

        public override void EjectContents() {
            EjectContents(interrupted: false);
        }
        public void EjectContents(bool interrupted) {
            foreach (Thing item in (IEnumerable<Thing>)innerContainer) {
                Pawn pawn = item as Pawn;
                if (pawn != null) {
                    PawnComponentsUtility.AddComponentsForSpawn(pawn);
                    pawn.needs?.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SoakingWet);
                    pawn.filth.GainFilth(ThingDefOf.Filth_PodSlime);
                    FilthMaker.TryMakeFilth(InteractionCell, Map, ThingDefOf.Filth_PodSlime, new IntRange(3, 6).RandomInRange);
                    if(interrupted) {
				        pawn.health?.AddHediff(HediffDefOf.BiosculptingSickness);
                    }
                }
            }
            if (!base.Destroyed) {
                BiosculpterOverhaulDefOf.BiosculpterPod_Exit.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map)));
            }
            base.EjectContents();
        }

        public override void Tick() {
            base.Tick();
            innerContainer.ThingOwnerTick();
            if (!ModLister.CheckIdeology("Biosculpting")) {
                return;
            }
            if (IsPowered || !IsOccupied) {
                if (TicksWithoutPower > 100) {
                    Messages.Message("BiosculpterPowerRestoredMessage".Translate(Occupant.LabelCap, Occupant), Occupant, MessageTypeDefOf.PositiveEvent, historical: true);
                }
                TicksWithoutPower = 0;
            } else {
                if (TicksWithoutPower == 100) {
                    Messages.Message("BiosculpterNoPowerWillEjectMessage".Translate(Occupant.LabelCap, Occupant), Occupant, MessageTypeDefOf.NegativeEvent, historical: true);
                }
                TicksWithoutPower++;
                if (TicksWithoutPower >= EjectAfterTicksWithoutPower && IsOccupied) {
                    Messages.Message("BiosculpterNoPowerEjectedMessage".Translate(Occupant.Named("PAWN")), Occupant, MessageTypeDefOf.NegativeEvent, historical: true);
                    TicksWithoutPower = 0;
                    EjectContents(interrupted: true);
                }
            }
            SetPower();
        }

		private void SetPower()	{
			if (powerTraderComp == null) {
				//powerTraderComp = GetComp<CompPowerTrader>();
			}
			if (IsOccupied) {
				powerTraderComp.PowerOutput = -1f * powerConsumptionActive;
			} else {
				powerTraderComp.PowerOutput = -1f * powerConsumptionStandby;
			}
		}

        public static Building_BiosculpterPod FindBiosculpterPodFor(Pawn p, Pawn traveler, bool ignoreOtherReservations = false) {
            foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => typeof(Building_BiosculpterPod).IsAssignableFrom(def.thingClass)))
            // Could replace lambda with `=> def.IsBiosculpterPod` and save the bool as an extra variable on ThingDef. Probably not worth it.
            {
                Building_BiosculpterPod building_BiosculpterPod = (Building_BiosculpterPod)GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForDef(item), PathEndMode.InteractionCell, TraverseParms.For(traveler), 9999f, (Thing x) => !((Building_BiosculpterPod)x).HasAnyContents && traveler.CanReserve(x, 1, -1, null, ignoreOtherReservations));
                if (building_BiosculpterPod != null)
                {
                    return building_BiosculpterPod;
                }
            }
            return null;
        }
        public virtual void UsedThisTick() {
            biorefuelableComp.Notify_UsedThisTick();
        }
        public bool CurrentlyUsableForBills() {
            return IsOccupied && UsableForBillsAfterFueling();
        }
        public bool UsableForBillsAfterFueling() {
            return IsPowered && IsBiofuelled;
        }
    }
}