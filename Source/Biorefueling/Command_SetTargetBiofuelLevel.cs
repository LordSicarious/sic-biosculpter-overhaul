// RimWorld.Command_SetTargetBiofuelLevel
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BiosculpterOverhaul {
    [StaticConstructorOnStartup]
    public class Command_SetTargetBiofuelLevel : Command {
        public CompBiorefuelable biorefuelable;

        private List<CompBiorefuelable> biorefuelables;

        public override void ProcessInput(Event ev) {
            base.ProcessInput(ev);
            if (biorefuelables == null) {
                biorefuelables = new List<CompBiorefuelable>();
            }
            if (!biorefuelables.Contains(biorefuelable)) {
                biorefuelables.Add(biorefuelable);
            }
            int num = int.MaxValue;
            for (int i = 0; i < biorefuelables.Count; i++) {
                if ((int)biorefuelables[i].Props.biofuelCapacity < num) {
                    num = (int)biorefuelables[i].Props.biofuelCapacity;
                }
            }
            int startingValue = num / 2;
            for (int j = 0; j < biorefuelables.Count; j++) {
                if ((int)biorefuelables[j].TargetBiofuelLevel <= num) {
                    startingValue = (int)biorefuelables[j].TargetBiofuelLevel;
                    break;
                }
            }
            Func<int, string> textGetter = (Func<int, string>)((int x) => "SetTargetBiofuelLevel".Translate(x));
            Dialog_Slider window = new Dialog_Slider(textGetter, 0, num, delegate(int value) {
                for (int k = 0; k < biorefuelables.Count; k++) {
                    biorefuelables[k].TargetBiofuelLevel = value;
                }
            }, startingValue);
            Find.WindowStack.Add(window);
        }

        public override bool InheritInteractionsFrom(Gizmo other) {
            if (biorefuelables == null) {
                biorefuelables = new List<CompBiorefuelable>();
            }
            biorefuelables.Add(((Command_SetTargetBiofuelLevel)other).biorefuelable);
            return false;
        }
    }
}