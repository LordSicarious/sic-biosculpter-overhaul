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
    public static class Harmony_Includes {
        // Include Biorefueling in Refuelable request group
		public static void Includes_Postfix(this ThingRequestGroup group, ThingDef def, ref bool __result) {
			if (group == ThingRequestGroup.Refuelable && __result != true) {
                __result = def.HasComp(typeof(CompBiorefuelable));
            }
		}
    }
}