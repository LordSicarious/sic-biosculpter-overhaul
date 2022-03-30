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
    [StaticConstructorOnStartup]
    public static class ApplyHarmonyPatches {
		// Reference to this class for patches
        static ApplyHarmonyPatches() {
			// Instantiate Harmony
			var harmony = new Harmony("sic.biosculpteroverhaul.thisisanid");
			Type patchType;
			MethodInfo original;
			string modified;

			//Postfix to [RimWorld.FloatMenuMakerMap.AddHumanlikeOrders]
			patchType = typeof(Harmony_AddHumanlikeOrders);
            original = AccessTools.Method(typeof(FloatMenuMakerMap), name: "AddHumanlikeOrders");
            modified = nameof(Harmony_AddHumanlikeOrders.AddHumanlikeOrders_Postfix);
            harmony.Patch(original, null, new HarmonyMethod(patchType, modified));

			//Postfix to [RimWorld.TargetingParameters.ForCarryToBiosculpterPod]
			patchType = typeof(Harmony_ForCarryToBiosculpterPod);
            original = AccessTools.Method(typeof(TargetingParameters), name: "ForCarryToBiosculpterPod");
            modified = nameof(Harmony_ForCarryToBiosculpterPod.ForCarryToBiosculpterPod_Postfix);
            harmony.Patch(original, null, new HarmonyMethod(patchType, modified));
        }
    }
}