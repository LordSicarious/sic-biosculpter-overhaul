// BiosculpterOverhaul.Dialog_Biosculpting
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

using static BiosculpterOverhaul.MiscTypesAndMethods;

// STYLING STATION INTERFACE AS TEMPLATE

namespace BiosculpterOverhaul {
[StaticConstructorOnStartup]
public class Dialog_Biosculpting : Window {

	//  Harmony shit needed to access private fields
	private static AccessTools.FieldRef<object, string> field_headGraphicPath = AccessTools.FieldRefAccess<string>(typeof(Pawn_StoryTracker), "headGraphicPath");
	private static AccessTools.FieldRef<object, Color> field_MenuSectionBGBorderColor = AccessTools.FieldRefAccess<Color>(typeof(Widgets), "MenuSectionBGBorderColor");
	private static AccessTools.FieldRef<object, BodyTypeDef> field_bodyTypeMaleResolved = AccessTools.FieldRefAccess<BodyTypeDef>(typeof(Backstory), "bodyTypeMaleResolved");
	private static AccessTools.FieldRef<object, BodyTypeDef> field_bodyTypeFemaleResolved = AccessTools.FieldRefAccess<BodyTypeDef>(typeof(Backstory), "bodyTypeFemaleResolved");

	private ref string headGraphicPostop => ref field_headGraphicPath.Invoke(pawnPostop.story);
	private ref Color menuBorderColor => ref field_MenuSectionBGBorderColor.Invoke();

	private enum BiosculptingTab {
		Treatments, // Healing stuff associated with Hediffs
		Adjustments, // Changes to body shape, head shape, jawline, skintone, sex and age
		Enhancements, // Changes related to traits
	}

	private enum FeatureType {
		Sex,
		Crown,
		Face,
		Jawline,
		Body,
		Skin,
		Age,
	}

	// UI Variables
	private List<TabRecord> tabs = new List<TabRecord>();
	private BiosculptingTab mainTab;
	private bool BiosynthesisResearched;
	private bool naturalSkin, anyGenderHead = false, anyGenderBody = false;
	private Gender desiredGender => pawnPreop.gender; // Assume cis for now
	private Dictionary<FeatureType,string> adjusted = new Dictionary<FeatureType,string>();
	private HashSet<FeatureType> wrongGender = new HashSet<FeatureType>();

	private static Texture2D anyGenderIcon = ContentFinder<Texture2D>.Get("UI/Icons/Gender/Genders");

	// Things
	private Building_BiosculpterPod Pod;
	private Pawn pawnPreop, pawnPostop;

	// Pawn Window Rotations
	private Rot4 overviewPortraitRot = new Rot4(2);
	private Rot4 adjustmentsPortraitRot = new Rot4(2);

    public static readonly Regex regexMelanin = new Regex("^[0-9]{1,2}$|^[1][0][0]$");
    public static readonly Regex regexColorByte = new Regex(("^[1]?[0-9]{1,2}$|^[2][0-4][0-9]$|^[2][5][0-6]$"));

	// Skintone Menu Constants
	public static readonly float skinStyleButton = 45f;
	public static readonly float sliderSpacing = 40f;
	public static readonly float elementHeight = 24f;
    public static readonly float elementMargin = 4f;
    public static readonly float elementMarginSlider = elementHeight / 12f;

	// Head and Body Menu Constants

	private ScrollPositioner scrollPositioner = new ScrollPositioner();
	private const float IconSize = 66f;
	private const float IconMargin = 5f;
	private Vector2 headsScrollPos;
	private static float headsViewWidth => IconMargin + (IconSize+IconMargin)*heads.Count;
	private Vector2 bodiesScrollPos;
	private static float bodiesViewWidth => IconMargin + (IconSize+IconMargin)*bodies.Count;

	// Preview Colours
	private Color previewSkintone;
	private float previewMelanin; 

	public override Vector2 InitialSize => new Vector2(900f, 600f);
	private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);

	private const float PortraitZoom = 1.0f;
	private const float TabMargin = 18f;
	private const float sqrButtonSize = 40f;
	private const float rotButtonSize = 30f;
	private const float LeftRectPercent = 0.3f;
	private const float AdjustmentRowHeight = 126f;
	private const float AdjustmentRowButtonsHeight = 24f;
	private const float HairColorSelectorHeight = 110f;

	// Static Lists for Internal Use
	private static List<HeadInfo> heads;
	private static int headCount_M, headCount_F, headCount_N;
	private static Dictionary<BodyTypeDef, List<Gender>> bodies;


	public Dialog_Biosculpting(Building_BiosculpterPod pod) {
		this.Pod = pod;
		pawnPreop = pod.Occupant;

		// Populate Head List
		if(heads == null) {
			heads = new List<HeadInfo>();
			// Male heads
			foreach (string head in GraphicDatabaseUtility.GraphicNamesInFolder("Things/Pawn/Humanlike/Heads/Male")) {
				heads.Add(parseHeadPath("Things/Pawn/Humanlike/Heads/Male/" + head));
			}
			headCount_M = heads.Count;
			// Female heads
			foreach (string head in GraphicDatabaseUtility.GraphicNamesInFolder("Things/Pawn/Humanlike/Heads/Female")) {
				heads.Add(parseHeadPath("Things/Pawn/Humanlike/Heads/Female/" + head));
			}
			headCount_F = heads.Count - headCount_M;
			// None heads
			try {
			foreach (string head in GraphicDatabaseUtility.GraphicNamesInFolder("Things/Pawn/Humanlike/Heads/None")) {
				heads.Add(parseHeadPath("Things/Pawn/Humanlike/Heads/None/" + head));
			}} catch {}
			headCount_N = heads.Count - headCount_F;
		}
		// Populate Body Type List
		if(bodies == null) {
			bodies = new Dictionary<BodyTypeDef, List<Gender>>();
			BodyTypeDef tmpBodyType;
			foreach (Backstory backstory in BackstoryDatabase.allBackstories.Values) {
				tmpBodyType = null;
				if (backstory.slot == BackstorySlot.Childhood) {continue;}
				tmpBodyType = field_bodyTypeMaleResolved.Invoke(backstory);
				if(tmpBodyType != null) {
					if(!bodies.Keys.Contains(tmpBodyType)) {
						bodies.Add(tmpBodyType, new List<Gender>());
						//Log.Message("Added new body type: " + tmpBodyType.ToString());
					}
					if(!bodies[tmpBodyType].Contains(Gender.Male)) {
						//Log.Message(tmpBodyType.ToString() + " can be male.");
						bodies[tmpBodyType].Add(Gender.Male);
					}
				}

				tmpBodyType = field_bodyTypeFemaleResolved.Invoke(backstory);
				if(tmpBodyType != null) {
					if(!bodies.Keys.Contains(tmpBodyType)) {
						bodies.Add(tmpBodyType, new List<Gender>());
						//Log.Message("Added new body type: " + tmpBodyType.ToString());
					}
					if(!bodies[tmpBodyType].Contains(Gender.Female)) {
						//Log.Message(tmpBodyType.ToString() + " can be Female.");
						bodies[tmpBodyType].Add(Gender.Female);
					}
				}

				if(tmpBodyType != null) { continue;	}

				tmpBodyType = field_bodyTypeMaleResolved.Invoke(backstory);
				if(tmpBodyType != null) {
					if(!bodies.Keys.Contains(tmpBodyType)) {
						bodies.Add(tmpBodyType, new List<Gender>());
						//Log.Message("Added new body type: " + tmpBodyType.ToString());
					}
					if(!bodies[tmpBodyType].Contains(Gender.None)) {
						//Log.Message(tmpBodyType.ToString() + " can be neuter.");
						bodies[tmpBodyType].Add(Gender.None);
					}
				}
			}
		}

		BiosynthesisResearched = ResearchProjectDef.Named("Bioregeneration").IsFinished;

		// Generate temporary pawn for previewing operations
		SetupPawnPostop();

		forcePause = false;
		closeOnCancel = true;
		mainTab = BiosculptingTab.Treatments;
	}
	public override void PostOpen() {
		if (!ModLister.CheckIdeology("Biosculpting")) {
			Close();
		}
		else {
			base.PostOpen();
		}
	}

	public override void DoWindowContents(Rect inRect) {
		Text.Font = GameFont.Medium;
		Rect rect = new Rect(inRect);
		rect.height = Text.LineHeight * 2f;
		Widgets.Label(rect, "BiosculptingHeader".Translate().CapitalizeFirst() + ": " + Find.ActiveLanguageWorker.WithDefiniteArticle(pawnPreop.Name.ToStringShort, pawnPreop.gender, plural: false, name: true).ApplyTag(TagType.Name));
		Text.Font = GameFont.Small;
		inRect.yMin = rect.yMax + 4f;
		// Overview
		Rect overview = inRect;
		overview.width *= 0.2f;
		DrawOverview(overview);
		// Main
		Rect main = inRect;
		main.xMin = overview.xMax + 10f;
		DrawTabs(main);
		// Close Button
		Rect closeButton = new Rect(inRect.xMax - 26f, 0f, 26f, 26f);
		if (Mouse.IsOver(closeButton)) {
			Widgets.DrawHighlight(closeButton);
			TooltipHandler.TipRegion(closeButton, "Cancel".Translate());
		}
		if (Widgets.CloseButtonFor(closeButton)) {
			Reset();
			Close();
		}
		
		UpdateAdjustments();
		if(wrongGender.Count > 0) {
			var previousAlignment = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			string text = Find.ActiveLanguageWorker.WithDefiniteArticle(pawnPreop.Name.ToStringShort, pawnPreop.gender, plural: false, name: true).ApplyTag(TagType.Name) + " ";
			text += "BiosculptingWillUpsetPawn".Translate(pawnPreop.gender.Opposite().GetLabel().ToLower());
			text += " (";
			text += string.Join(", ", wrongGender);
			text += ")";
			GUI.color = ColorLibrary.RedReadable;
			Widgets.Label(new Rect(rect.x, rect.yMin - 15f, rect.width, Text.LineHeight * 2f + 10f), "Warning".Translate() + ": " + text);
			GUI.color = Color.white;
			Text.Anchor = previousAlignment;
		}
	}

	private void DrawOverview(Rect rect) {
		GUI.BeginGroup(rect);
		Rect pawnWindow = new Rect(rect);
		pawnWindow.height *= 0.45f;
		pawnWindow.y -= pawnWindow.height * 0.35f;
		RenderTexture image = PortraitsCache.Get(
				pawn: pawnPreop,
				size: new Vector2(pawnWindow.width, pawnWindow.height),
				rotation: overviewPortraitRot,
				cameraOffset: PortraitOffset,
				cameraZoom: PortraitZoom,
				supersample: true,
				compensateForUIScale: true,
				renderHeadgear: false, 
				renderClothes: false,
				overrideApparelColors: null,
				overrideHairColor: null,
				stylingStation: true);
		GUI.DrawTexture(pawnWindow, image);
		Rect panel = new Rect(0f, rect.height * 0.4f, rect.width, rect.height * 0.6f);
		DrawScheduleOverview(panel);
		GUI.EndGroup();
	}

	private void DrawScheduleOverview(Rect rect) {
		Text.Font = GameFont.Medium;
		Rect header = new Rect(rect);
		header.height =  Text.LineHeight * 1.5f;
		Widgets.Label(header, "OverviewHeader".Translate().CapitalizeFirst());
		Text.Font = GameFont.Small;
		rect.yMin = header.yMax;
		Widgets.DrawMenuSection(rect);
	}

	private void DrawTabs(Rect rect) {
		tabs.Clear();
		tabs.Add(new TabRecord("TabBiosculpterTreatments".Translate().CapitalizeFirst(), delegate {mainTab = BiosculptingTab.Treatments;}, mainTab == BiosculptingTab.Treatments));
		tabs.Add(new TabRecord("TabBiosculpterAdjustments".Translate().CapitalizeFirst(), delegate {mainTab = BiosculptingTab.Adjustments;}, mainTab == BiosculptingTab.Adjustments));
		tabs.Add(new TabRecord("TabBiosculpterEnhancements".Translate().CapitalizeFirst(), delegate {mainTab = BiosculptingTab.Enhancements;}, mainTab == BiosculptingTab.Enhancements));
		Widgets.DrawMenuSection(rect);
		TabDrawer.DrawTabs(rect, tabs);
		//rect = rect.ContractedBy(18f);
		switch (mainTab) {
		case BiosculptingTab.Treatments:
			DrawTabTreatments(rect);
			break;
		case BiosculptingTab.Adjustments:
			DrawTabAdjustments(rect);
			break;
		case BiosculptingTab.Enhancements:
			DrawTabEnhancements(rect);
			break;
		}
	}

	private void DrawTabTreatments(Rect rect) {
	}

	private void DrawTabAdjustments(Rect rect) {
		Rect pawnWindow = new Rect(rect.x + rect.width * 0.35f, rect.y - rect.height * 0.15f, rect.width  * 0.3f, rect.height * 0.75f);
		pawnWindow = pawnWindow.ContractedBy(20f);
		RenderTexture image = PortraitsCache.Get(
				pawn: pawnPostop,
				size: new Vector2(pawnWindow.width, pawnWindow.height),
				rotation: adjustmentsPortraitRot,
				cameraOffset: PortraitOffset,
				cameraZoom: PortraitZoom,
				supersample: true,
				compensateForUIScale: true,
				renderHeadgear: false, 
				renderClothes: false,
				overrideApparelColors: null,
				overrideHairColor: null,
				stylingStation: true);
		GUI.DrawTexture(pawnWindow, image);

		// Rotation Buttons
		if (Widgets.ButtonText(new Rect(pawnWindow.xMin, pawnWindow.yMax-rotButtonSize, rotButtonSize, rotButtonSize), "↻")) {
			adjustmentsPortraitRot.Rotate(RotationDirection.Clockwise);
			pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawnPostop);
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}
		if (Widgets.ButtonText(new Rect(pawnWindow.xMax-rotButtonSize, pawnWindow.yMax-rotButtonSize, rotButtonSize, rotButtonSize), "↺")) {
			adjustmentsPortraitRot.Rotate(RotationDirection.Counterclockwise);
			pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawnPostop);
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}
		
		Rect genderSelect = new Rect(rect.xMin+20f, rect.yMin + 20f, rect.width*0.35f, sqrButtonSize);
		DrawGenderSelect(genderSelect);
		
		Rect skintoneSelect = new Rect(rect.xMin+20f, genderSelect.yMax + 10f, rect.width*0.35f, rect.height*0.4f);
		DrawSkintoneSelect(skintoneSelect);

		Rect headSelect = new Rect(rect.xMin+20f, pawnWindow.yMax + 10f, pawnWindow.xMax - rect.xMin, IconSize + 2*IconMargin + 15f);
		DrawHeadSelect(headSelect);

		Rect bodySelect = new Rect(headSelect);
		bodySelect.y += IconSize + 2*IconMargin + 30f;
		DrawBodySelect(bodySelect);

		Rect adjustmentsList = new Rect(rect.xMax-rect.width*0.35f +10f, rect.yMin+20f, rect.width*0.35f-30f, rect.height*0.6f-40f);
		DrawAdjustmentList(adjustmentsList);

		Rect randomHead = new Rect(adjustmentsList.xMin, headSelect.yMin, adjustmentsList.width/2f - 10f, headSelect.height /2f -5f);
		if (Widgets.ButtonText(randomHead, "Random".Translate().CapitalizeFirst()))	{
			RandomHeadshape(anyGenderHead);
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}

		Rect anyGenderHeadBox = new Rect (randomHead.xMin+10f, randomHead.yMax + 10f, randomHead.height-10f, randomHead.height-10f);
		Widgets.DrawTextureFitted(anyGenderHeadBox, anyGenderIcon, 1f);
		Widgets.Checkbox(anyGenderHeadBox.xMax + 20f, anyGenderHeadBox.y, ref anyGenderHead, randomHead.height-10f);
		if (Mouse.IsOver(new Rect(anyGenderHeadBox){width = anyGenderHeadBox.width*2f + 20f})) {
			TooltipHandler.TipRegion(rect, "AllowAnyGender".Translate().CapitalizeFirst());
		}

		Rect randomBody = new Rect(randomHead);
		randomBody.y += IconSize + 2*IconMargin + 30f;
		if (Widgets.ButtonText(randomBody, "Random".Translate().CapitalizeFirst()))	{
			RandomBodytype(anyGenderBody);
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}

		Rect anyGenderBodyBox = new Rect (randomBody.xMin+10f, randomBody.yMax + 10f, randomBody.height-10f, randomBody.height-10f);
		Widgets.DrawTextureFitted(anyGenderBodyBox, anyGenderIcon, 1f);
		Widgets.Checkbox(anyGenderBodyBox.xMax + 20f, anyGenderBodyBox.y, ref anyGenderBody, randomBody.height-10f);
		if (Mouse.IsOver(new Rect(anyGenderBodyBox){width = anyGenderBodyBox.width*2f + 20f})) {
			TooltipHandler.TipRegion(rect, "AllowAnyGender".Translate().CapitalizeFirst());
		}

		Rect confirm = new Rect(randomHead);
		confirm.x = randomHead.xMax + 20f;
		if (Widgets.ButtonText(confirm, "Confirm".Translate().CapitalizeFirst()))	{
			Reset();
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}

		Rect reset = new Rect(randomBody);
		reset.x = randomBody.xMax + 20f;
		if (Widgets.ButtonText(reset, "Reset".Translate().CapitalizeFirst()))	{
			Reset();
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}

	}

	private void DrawTabEnhancements(Rect rect) {
	}

	private void DrawGenderSelect(Rect rect) {
		Color deactivatedOverlay = new Color(Widgets.MenuSectionBGFillColor.r, Widgets.MenuSectionBGFillColor.g, Widgets.MenuSectionBGFillColor.b, 0.8f);
		Gender curGen;
		// Male
		curGen = Gender.Male;
		Rect rectGender = new Rect(rect.xMin + rect.width/4f - sqrButtonSize/2f, rect.yMin, sqrButtonSize, sqrButtonSize);
		if (Mouse.IsOver(rectGender)) {
			Widgets.DrawHighlight(rectGender);
			TooltipHandler.TipRegion(rectGender, curGen.GetLabel().Translate().CapitalizeFirst());
		}
		Widgets.DrawTextureFitted(rectGender, curGen.GetIcon(), 0.8f);
		if (pawnPostop.kindDef.fixedGender != curGen && pawnPostop.kindDef.fixedGender != null && BiosynthesisResearched && pawnPostop.gender != curGen) {
			Widgets.DrawBoxSolid(rectGender, deactivatedOverlay);
		}
		if (pawnPostop.gender == curGen) {
			Widgets.DrawHighlight(rectGender);
			if(wrongGender.Contains(FeatureType.Sex)) {
				Widgets.DrawBoxSolidWithOutline(rectGender, new Color(0f,0f,0f,0f), new Color(1f, 0.2f, 0.2f, 1f), 2);
			} else {
				Widgets.DrawBox(rectGender, 2);
			}
		}
		if (Widgets.ButtonInvisible(rectGender)) {
			if (!BiosynthesisResearched) {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			} else if (pawnPostop.kindDef.fixedGender == curGen || pawnPostop.kindDef.fixedGender == null) {
				pawnPostop.gender = curGen;
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			} else {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
		}
		
		// None
		curGen = Gender.None;
		rectGender.x += rect.width/4f;
		if (Mouse.IsOver(rectGender)) {
			Widgets.DrawHighlight(rectGender);
			TooltipHandler.TipRegion(rectGender, "None".Translate().CapitalizeFirst());
		}
		Widgets.DrawTextureFitted(rectGender, curGen.GetIcon(), 0.8f);
		if (pawnPostop.kindDef.fixedGender != curGen && BiosynthesisResearched) {
			Widgets.DrawBoxSolid(rectGender, deactivatedOverlay);
		}
		if (pawnPostop.gender == curGen) {
			Widgets.DrawHighlight(rectGender);
			if(wrongGender.Contains(FeatureType.Sex)) {
				Widgets.DrawBoxSolidWithOutline(rectGender, new Color(0f,0f,0f,0f), new Color(1f, 0.2f, 0.2f, 1f), 2);
			} else {
				Widgets.DrawBox(rectGender, 2);
			}
		}
		if (Widgets.ButtonInvisible(rectGender)) {
			if (!BiosynthesisResearched) {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			} else if (pawnPostop.kindDef.fixedGender == curGen) {
				pawnPostop.gender = curGen;
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			} else {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
		}
		// Female
		rectGender.x += rect.width/4f;
		curGen = Gender.Female;
		if (Mouse.IsOver(rectGender)) {
			Widgets.DrawHighlight(rectGender);
			TooltipHandler.TipRegion(rectGender, curGen.ToString().Translate().CapitalizeFirst());
		}
		Widgets.DrawTextureFitted(rectGender, curGen.GetIcon(), 0.8f);
		if (pawnPostop.kindDef.fixedGender != curGen && pawnPostop.kindDef.fixedGender != null && BiosynthesisResearched && pawnPostop.gender != curGen) {
			Widgets.DrawBoxSolid(rectGender, deactivatedOverlay);
		}
		if (pawnPostop.gender == curGen) {
			Widgets.DrawHighlight(rectGender);
			if(wrongGender.Contains(FeatureType.Sex)) {
				Widgets.DrawBoxSolidWithOutline(rectGender, new Color(0f,0f,0f,0f), new Color(1f, 0.2f, 0.2f, 1f), 2);
			} else {
				Widgets.DrawBox(rectGender, 2);
			}
		}
		if (Widgets.ButtonInvisible(rectGender)) {
			if (!BiosynthesisResearched) {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			} else if (pawnPostop.kindDef.fixedGender == curGen || pawnPostop.kindDef.fixedGender == null) {
				pawnPostop.gender = curGen;
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			} else {
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
		}
	}

	private void DrawSkintoneSelect(Rect rect) {
		var previousFont = Text.Font;
		var previousAlignment = Text.Anchor;
		// Select Natural Skintone
		Rect rectNatural = new Rect(rect.xMin, rect.yMin, skinStyleButton, skinStyleButton);
		Rect rectSynthetic = new Rect(rect.xMin + rect.width/2f, rect.yMin, skinStyleButton, skinStyleButton);

		if (Mouse.IsOver(rectNatural)) {
			Widgets.DrawLightHighlight(rectNatural);
			TooltipHandler.TipRegion(rectNatural, "NaturalSkintones".Translate().CapitalizeFirst());
		}
		Widgets.DrawBoxSolid(rectNatural, PawnSkinColors.GetSkinColor(previewMelanin));
		Widgets.DrawBox(rectNatural);
		if (naturalSkin) {
			Widgets.DrawHighlight(rectNatural);
			Widgets.DrawBox(rectNatural, 3);
		}
		if (Widgets.ButtonInvisible(rectNatural)) {
			naturalSkin = true;
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
		rectNatural.xMin = rectNatural.xMax+5f;
		rectNatural.xMax = rectSynthetic.xMin -5f;
		rectNatural.height = skinStyleButton/2f;
		Widgets.Label(rectNatural, "NaturalSkintones".Translate().CapitalizeFirst());
		rectNatural.yMin = rectNatural.yMax;
		rectNatural.height  = skinStyleButton/2f;
		Text.Font = GameFont.Tiny;
		if (Widgets.ButtonText(rectNatural, "Random".Translate().CapitalizeFirst()))	{
			RandomNaturalSkintone();
		}
		Text.Font = GameFont.Small;


		// Select Synthetic Skintone
		if (Mouse.IsOver(rectSynthetic)) {
			Widgets.DrawLightHighlight(rectSynthetic);
			TooltipHandler.TipRegion(rectSynthetic, "SyntheticSkintones".Translate().CapitalizeFirst());
		}
		Widgets.DrawBoxSolid(rectSynthetic, previewSkintone);
		Widgets.DrawBox(rectSynthetic);
		if (!naturalSkin) {
			Widgets.DrawHighlight(rectSynthetic);
			Widgets.DrawBox(rectSynthetic, 3);
		}
		if (Widgets.ButtonInvisible(rectSynthetic)) {
			naturalSkin = false;
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
		rectSynthetic.xMin = rectSynthetic.xMax+5f;
		rectSynthetic.xMax = rect.xMax -5f;
		rectSynthetic.height  = skinStyleButton/2f;
		Widgets.Label(rectSynthetic, "SyntheticSkintones".Translate().CapitalizeFirst());
		rectSynthetic.yMin = rectSynthetic.yMax;
		rectSynthetic.height  = skinStyleButton/2f;
		Text.Font = GameFont.Tiny;
		if (Widgets.ButtonText(rectSynthetic, "Random".Translate().CapitalizeFirst()))	{
			RandomSyntheticSkintone();
		}

		Text.Anchor = TextAnchor.MiddleCenter;
		Rect label = new Rect(rect.xMin, rect.y + skinStyleButton+5f, rect.width-sqrButtonSize, elementHeight);
		Rect slider = new Rect(rect){y = label.y+20f, width = rect.width-sqrButtonSize, height = elementHeight};
		Rect textField = new Rect(slider.xMax+5f, slider.yMin - elementMargin, elementHeight, elementHeight);

		// Melanin
		Text.Font = GameFont.Small;
		Widgets.Label(label, "Melanin".Translate());
		previewMelanin = Widgets.HorizontalSlider(slider.ContractedBy(0, elementMarginSlider), previewMelanin.toMelaninInt(), 0, 100, roundTo: 1).toMelaninFloat();
		Text.Font = GameFont.Tiny;
		previewMelanin = int.Parse(Widgets.TextField(textField, previewMelanin.toMelaninInt().ToString(), 3, regexMelanin)).toMelaninFloat();
		label.y += sliderSpacing;
		slider.y += sliderSpacing;
		textField.y += sliderSpacing;

		// Red
		Text.Font = GameFont.Small;
		Widgets.Label(label, "Red".Translate());
		previewSkintone.r = Widgets.HorizontalSlider(slider.ContractedBy(0, elementMarginSlider), previewSkintone.r.toColorInt(), 0, 255, roundTo: 1).toColorFloat();
		Text.Font = GameFont.Tiny;
		previewSkintone.r = int.Parse(Widgets.TextField(textField, previewSkintone.r.toColorInt().ToString(), 3, regexColorByte)).toColorFloat();
		label.y += sliderSpacing;
		slider.y += sliderSpacing;
		textField.y += sliderSpacing;

		// Green
		Text.Font = GameFont.Small;
		Widgets.Label(label, "Green".Translate());
		previewSkintone.g = Widgets.HorizontalSlider(slider.ContractedBy(0, elementMarginSlider), previewSkintone.g.toColorInt(), 0, 255, roundTo: 1).toColorFloat();
		Text.Font = GameFont.Tiny;
		previewSkintone.g = int.Parse(Widgets.TextField(textField, previewSkintone.g.toColorInt().ToString(), 3, regexColorByte)).toColorFloat();
		label.y += sliderSpacing;
		slider.y += sliderSpacing;
		textField.y += sliderSpacing;

		// Blue
		Text.Font = GameFont.Small;
		Widgets.Label(label, "Blue".Translate());
		previewSkintone.b = Widgets.HorizontalSlider(slider.ContractedBy(0, elementMarginSlider), previewSkintone.b.toColorInt(), 0, 255, roundTo: 1).toColorFloat();
		Text.Font = GameFont.Tiny;
		previewSkintone.b = int.Parse(Widgets.TextField(textField, previewSkintone.b.toColorInt().ToString(), 3, regexColorByte)).toColorFloat();
		label.y += sliderSpacing;
		slider.y += sliderSpacing;
		textField.y += sliderSpacing;

		if (naturalSkin) {
			pawnPostop.story.melanin = previewMelanin;
			pawnPostop.story.skinColorOverride = null;
		} else {
			pawnPostop.story.skinColorOverride = previewSkintone;
		}
		// Refresh the graphics cache
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
		Text.Font = previousFont;
		Text.Anchor = previousAlignment;
	}
	private void DrawHeadSelect(Rect rect) {
		Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
		Rect rect2 = new Rect(0f, 0f, headsViewWidth, IconSize + 1.5f*IconMargin);
		scrollPositioner.ClearInterestRects();
		Widgets.BeginScrollView(rect, ref headsScrollPos, rect2);
		Widgets.ScrollHorizontal(rect, ref headsScrollPos, rect2);
		Rect icon = new Rect(IconMargin, IconMargin, IconSize, IconSize);
		Rect gendered = new Rect(IconMargin+IconSize*0.7f, IconMargin, IconSize*0.3f, IconSize*0.3f);
		Rect iconOffset;
		Graphic_Multi headGraphic;
		Material headMat = SolidColorMaterials.NewSolidColorMaterial(pawnPostop.story.SkinColor, ShaderUtility.GetSkinShader(pawnPostop.story.SkinColorOverriden));
		headMat.renderQueue = 4000;
		foreach(HeadInfo head in heads) {
			iconOffset = new Rect(icon);
			if (icon.xMax < headsScrollPos.x) {icon.x += IconSize + IconMargin; gendered.x += IconSize + IconMargin; continue;}
			else if (icon.xMin > rect.width + headsScrollPos.x) {break;}
			else if (icon.xMin + IconMargin < headsScrollPos.x) {iconOffset.xMin = headsScrollPos.x - IconMargin;}
			else if (icon.xMax > rect.width + headsScrollPos.x) {iconOffset.xMax = rect.width + headsScrollPos.x;}

			headGraphic = GraphicDatabaseHeadRecords.GetHeadNamed(head.path, pawnPostop.story.SkinColor, !naturalSkin) as Graphic_Multi;

			Widgets.DrawTextureFitted(
					iconOffset,
					headGraphic.MatSouth.mainTexture,
					1f, // Scale factor MUST be 1f, breaks if you set it to anything higher
					new Vector2(iconOffset.width/IconSize,45/33f),
					new Rect(0.335f+0.33f*(iconOffset.xMin-icon.xMin)/IconSize, 0.25f, 0.33f*(iconOffset.xMax-iconOffset.xMin)/IconSize, 0.45f),
					mat: headMat);

			Widgets.DrawTextureFitted (gendered, head.gender.GetIcon(), 1f);

			if (headGraphicPostop == head.path) {
				Widgets.DrawHighlight(icon);
				if(wrongGender.Contains(FeatureType.Face)) {
					Widgets.DrawBoxSolidWithOutline(icon, new Color(0f,0f,0f,0f), new Color(1f, 0.2f, 0.2f, 1f), 2);
				} else {
					Widgets.DrawBox(icon, 2);
				}
			}
			if (Widgets.ButtonInvisible(icon)) {
				headGraphicPostop = head.path;
				pawnPostop.story.crownType = head.crownType;
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
				PortraitsCache.SetDirty(pawnPostop);
			}
			icon.x += IconSize + IconMargin;
			gendered.x += IconSize + IconMargin;
		}
		Widgets.EndScrollView();
		scrollPositioner.ScrollHorizontally(ref headsScrollPos, rect.size);
		Widgets.DrawBoxSolidWithOutline(rect, new Color(0f,0f,0f,0f), menuBorderColor);
	}

	private void DrawBodySelect(Rect rect) {
		Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
		Rect rect2 = new Rect(0f, 0f, bodiesViewWidth, IconSize + 1.5f*IconMargin);
		scrollPositioner.ClearInterestRects();
		Widgets.BeginScrollView(rect, ref bodiesScrollPos, rect2);
		Widgets.ScrollHorizontal(rect, ref bodiesScrollPos, rect2);
		Rect icon = new Rect(IconMargin, IconMargin, IconSize, IconSize);
		Rect gendered = new Rect(IconMargin+IconSize*0.7f, IconMargin, IconSize*0.3f, IconSize*0.3f);
		Rect iconOffset;
		Graphic_Multi bodyGraphic;
		Material bodyMat = SolidColorMaterials.NewSolidColorMaterial(pawnPostop.story.SkinColor, ShaderUtility.GetSkinShader(pawnPostop.story.SkinColorOverriden));
		bodyMat.renderQueue = 4000;
		foreach(BodyTypeDef body in bodies.Keys) {
			iconOffset = new Rect(icon);
			if (icon.xMax < bodiesScrollPos.x) {icon.x += IconSize + IconMargin; gendered.x += IconSize + IconMargin; continue;}
			else if (icon.xMin > rect.width + bodiesScrollPos.x) {break;}

			else if (icon.xMin + IconMargin < bodiesScrollPos.x) {iconOffset.xMin = bodiesScrollPos.x - IconMargin;}
			else if (icon.xMax - IconMargin > rect.width + bodiesScrollPos.x) {iconOffset.xMax = rect.width + bodiesScrollPos.x + IconMargin;}

			bodyGraphic = GraphicDatabase.Get<Graphic_Multi>(body.bodyNakedGraphicPath, ShaderUtility.GetSkinShader(pawnPostop.story.SkinColorOverriden), Vector2.one, pawnPostop.story.SkinColor) as Graphic_Multi;

			Widgets.DrawTextureFitted(
					iconOffset,
					bodyGraphic.MatSouth.mainTexture,
					1f, // Scale factor MUST be 1f, breaks if you set it to anything higher
					new Vector2(iconOffset.width/IconSize,75/66f),
					new Rect(0.17f+0.66f*(iconOffset.xMin-icon.xMin)/IconSize, 0.05f, 0.66f*(iconOffset.xMax-iconOffset.xMin)/IconSize, 0.75f),
					mat: bodyMat);
			
			if(bodies[body].Count == 1) {
				Widgets.DrawTextureFitted (gendered, bodies[body][0].GetIcon(), 1f);
			}

			if (pawnPostop.story.bodyType == body) {
				Widgets.DrawHighlight(icon);
				if(wrongGender.Contains(FeatureType.Body)) {
					Widgets.DrawBoxSolidWithOutline(icon, new Color(0f,0f,0f,0f), new Color(1f, 0.2f, 0.2f, 1f), 2);
				} else {
					Widgets.DrawBox(icon, 2);
				}
			}
			if (Widgets.ButtonInvisible(icon)) {
				pawnPostop.story.bodyType = body;
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
				PortraitsCache.SetDirty(pawnPostop);
			}
			gendered.x += IconSize + IconMargin;
			icon.x += IconSize + IconMargin;
		}
		Widgets.EndScrollView();
		scrollPositioner.ScrollHorizontally(ref bodiesScrollPos, rect.size);
		Widgets.DrawBoxSolidWithOutline(rect, new Color(0f,0f,0f,0f), menuBorderColor);
	}

	private void DrawAdjustmentList(Rect rect) {
		Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
		var previousAlignment = Text.Anchor;
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.UpperCenter;
		Widgets.Label(rect, "NewAdjustments".Translate());
		UpdateAdjustments();
		Text.Font = GameFont.Small;
		Rect row = new Rect(rect.xMin, rect.yMin+1.5f*Text.LineHeight, rect.width, 1.5f*Text.LineHeight);
		bool even = true;
		foreach (FeatureType feature in adjusted.Keys) {
			Rect rf = new Rect(row) {width = 0.28f * row.width}.ContractedBy(5f);
			Rect ra = new Rect(row) {xMin = row.xMin + 0.28f * row.width}.ContractedBy(5f);
			if (even) {
				Widgets.DrawBoxSolid(row, Widgets.MenuSectionBGFillColor);
			}
			even = !even;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rf, feature.ToString().Translate().CapitalizeFirst());
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(ra, adjusted[feature]);
			row.y +=Text.LineHeight *1.5f;
		}
		Text.Anchor = previousAlignment;
		Widgets.DrawBoxSolidWithOutline(rect, new Color(0f,0f,0f,0f), menuBorderColor);
	}

	private void RandomNaturalSkintone() {
		previewMelanin = Rand.Value;
		pawnPostop.story.melanin = previewMelanin;
		naturalSkin = true;
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
		SoundDefOf.Tick_Low.PlayOneShotOnCamera();
	}
	private void RandomSyntheticSkintone() {
		previewSkintone = new Color(Rand.Value,Rand.Value,Rand.Value);
		pawnPostop.story.skinColorOverride = previewSkintone;
		naturalSkin = false;
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
		SoundDefOf.Tick_Low.PlayOneShotOnCamera();
	}

	private void RandomHeadshape(bool anyGender = false) {
		int min = 0, max = heads.Count;
		if (!anyGender) {
			switch (desiredGender) {
			case (Gender.Male) :
				min = 0;
				max = min + headCount_M;
				break;
			case (Gender.Female) :
				min = headCount_M;
				max = min + headCount_F;
				break;
			case (Gender.None) :
				min = headCount_M + headCount_F;
				max = min + headCount_N;
				break;
			}
		}
		HeadInfo headTmp = heads[Rand.Range(min,max)];
		headGraphicPostop = headTmp.path;
		pawnPostop.story.crownType = headTmp.crownType;
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
	}
	private void RandomBodytype(bool anyGender = false) {
		List<BodyTypeDef> okayBodies = new List<BodyTypeDef>();
		foreach (BodyTypeDef body in bodies.Keys) {
			if(bodies[body].Contains(desiredGender) || anyGender) {okayBodies.Add(body);}
		}
		pawnPostop.story.bodyType = okayBodies[Rand.Range(0,okayBodies.Count)];
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
	}


	private void UpdateAdjustments() {
		wrongGender.Clear();
		adjusted.Clear();
		// Head
		HeadInfo headOld = parseHeadPath(pawnPreop.story.HeadGraphicPath);
		HeadInfo headNew = parseHeadPath(pawnPostop.story.HeadGraphicPath);
		if (headNew.gender == desiredGender) {wrongGender.Remove(FeatureType.Face);}
		else {wrongGender.Add(FeatureType.Face);}

		if (headNew.crownType != headOld.crownType) {adjusted.Add(FeatureType.Crown, headNew.crownType.ToString());}
		else {adjusted.Remove(FeatureType.Crown);}
		if (headNew.gender != headOld.gender) {adjusted.Add(FeatureType.Face, headNew.gender.ToString().CapitalizeFirst());}
		else {adjusted.Remove(FeatureType.Face);}
		if (headNew.jawType != headOld.jawType) {adjusted.Add(FeatureType.Jawline, headNew.jawType);}
		else {adjusted.Remove(FeatureType.Jawline);}
		// Body
		if (bodies[pawnPostop.story.bodyType].Contains(desiredGender)) {wrongGender.Remove(FeatureType.Body);}
		else {wrongGender.Add(FeatureType.Body);}

		if (pawnPostop.story.bodyType != pawnPreop.story.bodyType) {adjusted.Add(FeatureType.Body, pawnPostop.story.bodyType.ToString().CapitalizeFirst());}
		else {adjusted.Remove(FeatureType.Body);}
		// Sex
		if (pawnPostop.gender == desiredGender) {wrongGender.Remove(FeatureType.Sex);}
		else {wrongGender.Add(FeatureType.Sex);}

		if (pawnPostop.gender != pawnPreop.gender) {adjusted.Add(FeatureType.Sex, pawnPostop.gender.ToString().CapitalizeFirst());}
		else {adjusted.Remove(FeatureType.Sex);}
		// Skin
		if (pawnPostop.story.SkinColor.IndistinguishableFromFast(pawnPreop.story.SkinColor) && pawnPreop.story.SkinColorOverriden != naturalSkin) {adjusted.Remove(FeatureType.Skin);}
		else {
			if (naturalSkin) {
				if (pawnPostop.story.melanin > pawnPreop.story.melanin) {
					adjusted.Add(FeatureType.Skin, "Darken".Translate() + " (" + previewMelanin.toMelaninInt().ToString() + "%)");
				}
				else {
					adjusted.Add(FeatureType.Skin, "Lighten".Translate() + " (" + previewMelanin.toMelaninInt().ToString() + "%)");
				}
			} else {
				adjusted.Add(FeatureType.Skin, "SyntheticSkintones".Translate() + " (#" + 
						previewSkintone.r.toColorInt().ToString("X2") + 
						previewSkintone.g.toColorInt().ToString("X2") + 
						previewSkintone.b.toColorInt().ToString("X2") + ")");
			}
		}
	}

	private void Reset() {
		wrongGender.Clear();
		// Gender
		pawnPostop.gender = pawnPreop.gender;
		// Story tracker stuff
		pawnPostop.story.melanin = pawnPreop.story.melanin;
		pawnPostop.story.skinColorOverride = pawnPreop.story.skinColorOverride;
		pawnPostop.story.bodyType = pawnPreop.story.bodyType;
		pawnPostop.story.crownType = pawnPreop.story.crownType;
		// Harmony stuff because headGraphicPath is private and only has a public getter
		headGraphicPostop = pawnPreop.story.HeadGraphicPath;

		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();

		// Hediff stuff
		foreach (Hediff h in pawnPreop.health.hediffSet.hediffs) {
			if(h.Visible) {
				pawnPostop.health.AddHediff(h.def, h.Part);
			}
		}
		HealthUtility.HealNonPermanentInjuriesAndFreshWounds(pawnPostop);

		// Reset Flags
		if (pawnPostop.gender != desiredGender) {wrongGender.Add(FeatureType.Sex);}
		if (parseHeadPath(pawnPostop.story.HeadGraphicPath).gender != desiredGender) {wrongGender.Add(FeatureType.Face);}
		if (!bodies[pawnPostop.story.bodyType].Contains(desiredGender)) {wrongGender.Add(FeatureType.Body);}
		previewMelanin = pawnPostop.story.melanin;
		previewSkintone = pawnPostop.story.SkinColor;
		naturalSkin = !pawnPostop.story.SkinColorOverriden;
		anyGenderHead = false;
		anyGenderBody = false;

		// Refresh the graphics cache
		pawnPostop.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawnPostop);
	}

	private void SetupPawnPostop() {
		// Generate temporary pawn for previewing operations
		pawnPostop = PawnGenerator.GeneratePawn(pawnPreop.kindDef);
		StripTattoosAndHair(pawnPostop);
		Reset();
	}
	private void StripTattoosAndHair(Pawn pawn) {
		pawn.story.hairDef = HairDefOf.Shaved;
		pawn.style.beardDef = BeardDefOf.NoBeard;
		pawn.style.FaceTattoo = TattooDefOf.NoTattoo_Face;
		pawn.style.BodyTattoo = TattooDefOf.NoTattoo_Body;
		pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
		PortraitsCache.SetDirty(pawn);
	}
}
    public static class SettingsExt {
        public static readonly float colorSpace = 255f;
        public static readonly float melaninSpace = 100f;
        public static int toColorInt(this float c) {return Mathf.RoundToInt(c * colorSpace);}
        public static float toColorFloat(this int c) {return c / colorSpace;}
        public static float toColorFloat(this float c) {return c / colorSpace;}
        public static int toMelaninInt(this float c) {return Mathf.RoundToInt(c * melaninSpace);}
		public static float toMelaninFloat(this int c) {return c / melaninSpace;}
		public static float toMelaninFloat(this float c) {return c / melaninSpace;}
    }
}