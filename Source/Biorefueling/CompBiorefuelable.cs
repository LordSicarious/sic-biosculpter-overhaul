// BiosculpterOverhaul.CompBiorefuelable
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BiosculpterOverhaul {
	[StaticConstructorOnStartup]
	public class CompBiorefuelable : ThingComp
	{
		private float fuel;

		private float configuredTargetBiofuelLevel = -1f;

		public bool allowAutoBiorefuel = true;

		private CompFlickable flickComp;

		public const string BiorefueledSignal = "Biorefueled";

		public const string RanOutOfFuelSignal = "RanOutOfFuel";

		private static readonly Texture2D SetTargetBiofuelLevelCommand = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel");

		private static readonly Vector2 BiofuelBarSize = new Vector2(1f, 0.2f);

		private static readonly Material BiofuelBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.6f, 0.56f, 0.13f));

		private static readonly Material BiofuelBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

		public float TargetBiofuelLevel
		{
			get
			{
				if (configuredTargetBiofuelLevel >= 0f)
				{
					return configuredTargetBiofuelLevel;
				}
				if (Props.targetBiofuelLevelConfigurable)
				{
					return Props.initialConfigurableTargetBiofuelLevel;
				}
				return Props.biofuelCapacity;
			}
			set
			{
				configuredTargetBiofuelLevel = Mathf.Clamp(value, 0f, Props.biofuelCapacity);
			}
		}

		public CompProperties_Biorefuelable Props => (CompProperties_Biorefuelable)props;

		public float Biofuel => fuel;

		public float BiofuelPercentOfTarget => fuel / TargetBiofuelLevel;

		public float BiofuelPercentOfMax => fuel / Props.biofuelCapacity;

		public bool IsFull => TargetBiofuelLevel - fuel < 1f;

		public bool HasBiofuel
		{
			get
			{
				if (fuel > 0f)
				{
					return fuel >= Props.minimumFueledThreshold;
				}
				return false;
			}
		}

		private float ConsumptionRatePerTick => Props.biofuelConsumptionRate / 60000f;

		public bool ShouldAutoBiorefuelNow
		{
			get
			{
				if (BiofuelPercentOfTarget <= Props.autoRefuelPercent && !IsFull && TargetBiofuelLevel > 0f)
				{
					return ShouldAutoBiorefuelNowIgnoringBiofuelPct;
				}
				return false;
			}
		}

		public bool ShouldAutoBiorefuelNowIgnoringBiofuelPct
		{
			get
			{
				if (!parent.IsBurning() && (flickComp == null || flickComp.SwitchIsOn) && parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.Flick) == null)
				{
					return parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.Deconstruct) == null;
				}
				return false;
			}
		}

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			allowAutoBiorefuel = Props.initialAllowAutoRefuel;
			fuel = Props.biofuelCapacity * Props.initialBiofuelPercent;
			flickComp = parent.GetComp<CompFlickable>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref fuel, "fuel", 0f);
			Scribe_Values.Look(ref configuredTargetBiofuelLevel, "configuredTargetBiofuelLevel", -1f);
			Scribe_Values.Look(ref allowAutoBiorefuel, "allowAutoBiorefuel", defaultValue: false);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && !Props.showAllowAutoRefuelToggle)
			{
				allowAutoBiorefuel = Props.initialAllowAutoRefuel;
			}
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if (!allowAutoBiorefuel)
			{
				parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.ForbiddenRefuel);
			}
			else if (!HasBiofuel && Props.drawOutOfBiofuelOverlay)
			{
				parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.OutOfFuel);
			}
			if (Props.drawBiofuelGaugeInMap)
			{
				GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
				r.center = parent.DrawPos + Vector3.up * 0.1f;
				r.size = BiofuelBarSize;
				r.fillPercent = BiofuelPercentOfMax;
				r.filledMat = BiofuelBarFilledMat;
				r.unfilledMat = BiofuelBarUnfilledMat;
				r.margin = 0.15f;
				Rot4 rotation = parent.Rotation;
				rotation.Rotate(RotationDirection.Clockwise);
				r.rotation = rotation;
				GenDraw.DrawFillableBar(r);
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap) {
			base.PostDestroy(mode, previousMap);
			if (previousMap != null && Props.biofuelFilter.AllowedDefCount == 1 && Props.initialBiofuelPercent == 0f) {
				ThingDef thingDef = Props.biofuelFilter.AllowedThingDefs.First();
				int num = GenMath.RoundRandom(1f * fuel);
				while (num > 0) {
					Thing thing = ThingMaker.MakeThing(thingDef);
					thing.stackCount = Mathf.Min(num, thingDef.stackLimit);
					num -= thing.stackCount;
					GenPlace.TryPlaceThing(thing, parent.Position, previousMap, ThingPlaceMode.Near);
				}
			}
		}

		public override string CompInspectStringExtra() {
			string text = Props.BiofuelLabel + ": " + fuel.ToStringDecimalIfSmall() + " / " + Props.biofuelCapacity.ToStringDecimalIfSmall();
			if (!Props.consumeBiofuelOnlyWhenUsed && HasBiofuel) {
				int numTicks = (int)(fuel / Props.biofuelConsumptionRate * 60000f);
				text = text + " (" + numTicks.ToStringTicksToPeriod() + ")";
			}
			if (!HasBiofuel && !Props.outOfBiofuelMessage.NullOrEmpty()) {
				text += $"\n{Props.outOfBiofuelMessage} ({GetBiofuelCountToFullyRefuel()}x {Props.biofuelFilter.AnyAllowedDef.label})";
			}
			if (Props.targetBiofuelLevelConfigurable) {
				text += "\n" + "ConfiguredTargetBiofuelLevel".Translate(TargetBiofuelLevel.ToStringDecimalIfSmall());
			}
			return text;
		}

		public override void CompTick() {
			base.CompTick();
			CompPowerTrader comp = parent.GetComp<CompPowerTrader>();
			if (!Props.consumeBiofuelOnlyWhenUsed && (flickComp == null || flickComp.SwitchIsOn) && (!Props.consumeBiofuelOnlyWhenPowered || (comp != null && comp.PowerOn))) {
				ConsumeBiofuel(ConsumptionRatePerTick);
			}
			if (Props.biofuelConsumptionPerTickInRain > 0f && parent.Spawned && parent.Map.weatherManager.RainRate > 0.4f && !parent.Map.roofGrid.Roofed(parent.Position)) {
				ConsumeBiofuel(Props.biofuelConsumptionPerTickInRain);
			}
		}

		public void ConsumeBiofuel(float amount) {
			if (fuel <= 0f) {
				return;
			}
			fuel -= amount;
			if (fuel <= 0f) {
				fuel = 0f;
				if (Props.destroyOnNoBiofuel) {
					parent.Destroy();
				}
				parent.BroadcastCompSignal("RanOutOfFuel");
			}
		}

		public void Biorefuel(List<Thing> fuelThings) {
			int num = GetBiofuelCountToFullyRefuel();
			while (num > 0 && fuelThings.Count > 0)	{
				Thing thing = fuelThings.Pop();
				if (!thing.def.IsIngestible) {
					Log.ErrorOnce("Error refueling; biofuel should be ingestible", 216584841);
					return;
				}
				int num2 = Mathf.Min(num, thing.stackCount);
				Biorefuel(num2 * thing.def.ingestible.CachedNutrition);
				thing.SplitOff(num2).Destroy();
				num -= num2;
			}
		}

		public void Biorefuel(float amount) {
			fuel += amount * Props.BiofuelMultiplierCurrentDifficulty;
			if (fuel > Props.biofuelCapacity) {
				fuel = Props.biofuelCapacity;
			}
			parent.BroadcastCompSignal("Biorefueled");
		}

		public void Notify_UsedThisTick() {
			ConsumeBiofuel(ConsumptionRatePerTick);
		}

		public int GetBiofuelCountToFullyRefuel() {
			return Mathf.Max(Mathf.CeilToInt((TargetBiofuelLevel - fuel) / Props.BiofuelMultiplierCurrentDifficulty), 1);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()	{
			if (Props.targetBiofuelLevelConfigurable) {
				Command_SetTargetBiofuelLevel command_SetTargetBiofuelLevel = new Command_SetTargetBiofuelLevel();
				command_SetTargetBiofuelLevel.biorefuelable = this;
				command_SetTargetBiofuelLevel.defaultLabel = "CommandSetTargetBiofuelLevel".Translate();
				command_SetTargetBiofuelLevel.defaultDesc = "CommandSetTargetBiofuelLevelDesc".Translate();
				command_SetTargetBiofuelLevel.icon = SetTargetBiofuelLevelCommand;
				yield return command_SetTargetBiofuelLevel;
			}
			if (Props.showBiofuelGizmo && Find.Selector.SingleSelectedThing == parent) {
				Gizmo_BiorefuelableFuelStatus gizmo_BiorefuelableFuelStatus = new Gizmo_BiorefuelableFuelStatus();
				gizmo_BiorefuelableFuelStatus.biorefuelable = this;
				yield return gizmo_BiorefuelableFuelStatus;
			}
			if (Props.showAllowAutoRefuelToggle) {
				Command_Toggle command_Toggle = new Command_Toggle();
				command_Toggle.defaultLabel = "CommandToggleAllowAutoBiorefuel".Translate();
				command_Toggle.defaultDesc = "CommandToggleAllowAutoBiorefuelDesc".Translate();
				command_Toggle.hotKey = KeyBindingDefOf.Command_ItemForbid;
				command_Toggle.icon = (allowAutoBiorefuel ? TexCommand.ForbidOff : TexCommand.ForbidOn);
				command_Toggle.isActive = () => allowAutoBiorefuel;
				command_Toggle.toggleAction = delegate {allowAutoBiorefuel = !allowAutoBiorefuel;};
				yield return command_Toggle;
			}
			if (Prefs.DevMode) {
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "Debug: Set fuel to 0";
				command_Action.action = delegate {
					fuel = 0f;
					parent.BroadcastCompSignal("Biorefueled");
				};
				yield return command_Action;
				Command_Action command_Action2 = new Command_Action();
				command_Action2.defaultLabel = "Debug: Set fuel to 0.1";
				command_Action2.action = delegate {fuel = 0.1f; parent.BroadcastCompSignal("Biorefueled");};
				yield return command_Action2;
				Command_Action command_Action3 = new Command_Action();
				command_Action3.defaultLabel = "Debug: Set fuel to max";
				command_Action3.action = delegate {fuel = Props.biofuelCapacity; parent.BroadcastCompSignal("Biorefueled");};
				yield return command_Action3;
			}
		}
	}
}