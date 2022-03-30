    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using HarmonyLib;
    using RimWorld;
    using UnityEngine;
    using Verse;
    using Verse.AI;
    using Verse.Grammar;


namespace BiosculpterOverhaul {
    public static class Harmony_AddHumanlikeOrders {
		// Add "Carry to Biosculpter Pod" option for downed humans
		public static void AddHumanlikeOrders_Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts) {
			foreach (LocalTargetInfo item14 in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarryToBiosculpterPod(pawn), thingsOnly: true)) {
				LocalTargetInfo localTargetInfo = item14;
				Pawn victim2 = (Pawn)localTargetInfo.Thing;
				if (!pawn.CanReserveAndReach(victim2, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true)
				|| Building_BiosculpterPod.FindBiosculpterPodFor(victim2, pawn, ignoreOtherReservations: true) == null
				|| !(victim2.IsPrisoner || victim2.Downed)){
					continue;
				}
				string text2 = "CarryToOverhauledBiosculpterPod".Translate(localTargetInfo.Thing.LabelCap, localTargetInfo.Thing);
				JobDef jDef = BiosculpterOverhaulDefOf.CarryToBiosculpterPod;
				Building_BiosculpterPod pod = Building_BiosculpterPod.FindBiosculpterPodFor(victim2, pawn);
				Action action2 = delegate {
					if (pod == null) {
						pod = Building_BiosculpterPod.FindBiosculpterPodFor(victim2, pawn, ignoreOtherReservations: true);
					}
					if (pod == null) {
						Messages.Message("CannotCarryToOverhauledBiosculpterPod".Translate() + ": " + "NoOverhauledBiosculpterPod".Translate(), victim2, MessageTypeDefOf.RejectInput, historical: false);
					}
					else {
						Job job19 = JobMaker.MakeJob(jDef, victim2, pod);
						job19.count = 1;
						pawn.jobs.TryTakeOrderedJob(job19, JobTag.Misc);
					}
				};
				if (victim2.IsQuestLodger()) {
					text2 += " (" + "OverhauledBiosculpterPodGuestsNotAllowed".Translate() + ")";
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
				else if (victim2.GetExtraHostFaction() != null) {
					text2 += " (" + "OverhauledBiosculpterPodGuestPrisonersNotAllowed".Translate() + ")";
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
				else if (!pod.IsPowered) {
					text2 += " (" + "OverhauledBiosculpterPodUnpowered".Translate() + ")";
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
				else {
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, action2, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
			}
		}
    }
}