using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BiosculpterOverhaul {
    [DefOf]
    public static class BiosculpterOverhaulDefOf {
        public static RoomStatDef BiosculpterMishapChance;
        public static StatDef BiosculpterOperatingSkill;
        public static SoundDef BiosculpterPod_Enter;
        public static SoundDef BiosculpterPod_Exit;
        public static JobDef EnterBiosculpterPod;
        public static JobDef Biorefuel;
        public static JobDef CarryToBiosculpterPod;
        static BiosculpterOverhaulDefOf() {
		    DefOfHelper.EnsureInitializedInCtor(typeof(BiosculpterOverhaulDefOf));
	    }
    }
    [DefOf]
    public static class BiosculpterOverhaulDefOf2 {
        public static WorkGiverDef Biorefuel;
        static BiosculpterOverhaulDefOf2() {
		    DefOfHelper.EnsureInitializedInCtor(typeof(BiosculpterOverhaulDefOf2));
	    }
    }
}