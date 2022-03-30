// BiosculpterOverhaul.MiscTypesAndMethods
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using HarmonyLib;

namespace BiosculpterOverhaul {
	public static class MiscTypesAndMethods {
		public struct HeadInfo {
			public string path;
			public Gender gender;
			public CrownType crownType;
			public string jawType;
		}

		public static HeadInfo parseHeadPath (string path) {
			HeadInfo tmpHeadInfo;
			tmpHeadInfo.path = path;
			string[] array = Path.GetFileNameWithoutExtension(path).Split('_');
			try {
				tmpHeadInfo.gender = ParseHelper.FromString<Gender>(array[0]);
				tmpHeadInfo.crownType = ParseHelper.FromString<CrownType>(array[1]);
				tmpHeadInfo.jawType = array[2];
			} catch (Exception ex) {
				Log.Error("Parse error with head graphic at " + tmpHeadInfo.path + ": " + ex.Message);
				tmpHeadInfo.gender = Gender.None;
				tmpHeadInfo.crownType = CrownType.Undefined;
				tmpHeadInfo.jawType = "error";
			}
			return tmpHeadInfo;
		}
	}
}