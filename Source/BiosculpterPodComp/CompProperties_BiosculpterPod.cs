// RimWorld.CompProperties_BiosculpterPod
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BiosculpterOverhaul {

    public class CompProperties_BiosculpterPod : CompProperties {

        public EffecterDef operatingEffecter;
        public EffecterDef readyEffecter;

        public Color standbyColor;
        public Color occupiedColor;
        public Color operatingColor;

        public float powerConsumptionStandby;

        public CompProperties_BiosculpterPod() {
            compClass = typeof(CompBiosculpterPod);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef)) {
                yield return item;
            }
            if (parentDef.tickerType != TickerType.Normal) {
                yield return GetType().Name + " requires parent ticker type Normal";
            }
        }
    }
}