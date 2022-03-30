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
    public static class Harmony_ForCarryToBiosculpterPod {
        // Define who can potentially be targeted to carry to Biosculpter Pods
		public static void ForCarryToBiosculpterPod_Postfix(ref TargetingParameters __result) {
			__result = new TargetingParameters {
				canTargetPawns = true,
				canTargetHumans = true,
				canTargetAnimals = false,
				canTargetMechs = false,
				canTargetBuildings = false,
				mapObjectTargetsMustBeAutoAttackable = false
			};
		}
    }
}