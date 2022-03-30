// RimWorld.CompProperties_Biorefuelable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BiosculpterOverhaul {
	public class CompProperties_Biorefuelable : CompProperties {
		public float biofuelConsumptionRate = 1f;
		public float biofuelCapacity = 2f;
		public float initialBiofuelPercent;
		public float autoRefuelPercent = 0.3f;
		public float biofuelConsumptionPerTickInRain;
		public ThingFilter biofuelFilter;
		public bool destroyOnNoBiofuel;
		public bool consumeBiofuelOnlyWhenUsed;
		public bool consumeBiofuelOnlyWhenPowered;
		public bool showBiofuelGizmo;
		public bool initialAllowAutoRefuel = true;
		public bool showAllowAutoRefuelToggle;
		public bool allowBiorefuelIfNotEmpty = true;
		public bool targetBiofuelLevelConfigurable;
		public float initialConfigurableTargetBiofuelLevel;
		public bool drawOutOfBiofuelOverlay = true;
		public float minimumFueledThreshold;
		public bool drawBiofuelGaugeInMap;
		private float biofuelMultiplier = 1f;
		public bool factorByDifficulty = false;
		public string biofuelLabel;
		public string biofuelGizmoLabel;
		public string outOfBiofuelMessage;
		public string biofuelIconPath;
		private Texture2D biofuelIcon;
		public string BiofuelLabel {
			get {
				if (biofuelLabel.NullOrEmpty()) {
					return "Biofuel".TranslateSimple();
				}
				return biofuelLabel;
			}
		}

		public string BiofuelGizmoLabel {
			get {
				if (biofuelGizmoLabel.NullOrEmpty()) {
					return "Biofuel".TranslateSimple();
				}
				return biofuelGizmoLabel;
			}
		}

		public Texture2D BiofuelIcon {
			get {
				if (biofuelIcon == null) {
					if (!biofuelIconPath.NullOrEmpty()) {
						biofuelIcon = ContentFinder<Texture2D>.Get(biofuelIconPath);
					}
					else {
						ThingDef thingDef = ((biofuelFilter.AnyAllowedDef == null) ? ThingDefOf.Chemfuel : biofuelFilter.AnyAllowedDef);
						biofuelIcon = thingDef.uiIcon;
					}
				}
				return biofuelIcon;
			}
		}

		public float BiofuelMultiplierCurrentDifficulty {
			get {
				if (factorByDifficulty && Find.Storyteller?.difficulty != null) {
					return biofuelMultiplier / Find.Storyteller.difficulty.maintenanceCostFactor;
				}
				return biofuelMultiplier;
			}
		}

		public CompProperties_Biorefuelable() {
			compClass = typeof(CompBiorefuelable);
		}

		public override void ResolveReferences(ThingDef parentDef) {
			base.ResolveReferences(parentDef);
			biofuelFilter.ResolveReferences();
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef) {
			foreach (string item in base.ConfigErrors(parentDef)) {
				yield return item;
			}
			if (destroyOnNoBiofuel && initialBiofuelPercent <= 0f) {
				yield return "Biorefuelable component has destroyOnNoBiofuel, but initialBiofuelPercent <= 0";
			}
			if ((!consumeBiofuelOnlyWhenUsed || biofuelConsumptionPerTickInRain > 0f) && parentDef.tickerType != TickerType.Normal) {
				yield return $"Biorefuelable component set to consume biofuel per tick, but parent tickertype is {parentDef.tickerType} instead of {TickerType.Normal}";
			}
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req) {
			foreach (StatDrawEntry item in base.SpecialDisplayStats(req)) {
				yield return item;
			}
			if (((ThingDef)req.Def).building.IsTurret) {
				TaggedString taggedString = "RearmCostExplanation".Translate();
				if (factorByDifficulty) {
					taggedString += " (" + "RearmCostExplanationDifficulty".Translate() + ")";
				}
				taggedString += ".";
				yield return new StatDrawEntry(StatCategoryDefOf.Building, "RearmCost".Translate(), GenLabel.ThingLabel(biofuelFilter.AnyAllowedDef, null, (int)(biofuelCapacity / BiofuelMultiplierCurrentDifficulty)).CapitalizeFirst(), taggedString, 3171);
				yield return new StatDrawEntry(StatCategoryDefOf.Building, "ShotsBeforeRearm".Translate(), ((int)biofuelCapacity).ToString(), "ShotsBeforeRearmExplanation".Translate(), 3171);
			}
		}
	}
}