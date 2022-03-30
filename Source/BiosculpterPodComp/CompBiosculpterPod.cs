// RimWorld.CompBiosculpterPod
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BiosculpterOverhaul {
	[StaticConstructorOnStartup]
    public class CompBiosculpterPod : ThingComp {

		private const float CacheForSecs = 2f;

		private const float BackgroundRect_YOff = 0.08108108f;

		private const float Pawn_YOff = 3f / 74f;

		private int EffectCycle = 0;

		public bool devFillPodLatch;

		private Effecter readyEffecter;

		private Effecter operatingEffecter;

		public CompProperties_BiosculpterPod Props => props as CompProperties_BiosculpterPod;

		private static Material PodInteriorMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.08f, 0.06f, 0.1f, 1f));

		public Building_BiosculpterPod Parent => parent as Building_BiosculpterPod;

		public Pawn Occupant {
			get	{return Parent.Occupant;}
		}

		public CompBiosculpterPod() {
			PodInteriorMat.renderQueue = 2501;
		}

		public override void Initialize(CompProperties props) {
			base.Initialize(props);
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref devFillPodLatch, "devFillPodLatch", defaultValue: false);
		}

		public override void CompTick() {
			if (!ModLister.CheckIdeology("Biosculpting")) {
				return;
			}
			base.CompTick();
			if (!Parent.IsPowered) {
				readyEffecter?.Cleanup();
				readyEffecter = null;
			} else if (Props.readyEffecter != null) {
				if (readyEffecter == null) {
					readyEffecter = Props.readyEffecter.Spawn();
					ColorizeEffecter(readyEffecter, Props.standbyColor);
					readyEffecter.Trigger(Parent, new TargetInfo(Parent.InteractionCell, Parent.Map));
				} else if (Parent.IsWorking) {
					ColorizeEffecter(readyEffecter, Props.operatingColor);
				} else if (Parent.IsOccupied) {
					ColorizeEffecter(readyEffecter, Props.occupiedColor);
				} else {
					ColorizeEffecter(readyEffecter, Props.standbyColor);
				}
				readyEffecter.EffectTick(Parent, new TargetInfo(Parent.InteractionCell, Parent.Map));
			}

			if (!Parent.IsWorking)	{
				operatingEffecter?.Cleanup();
				operatingEffecter = null;
			}
			else {
				Pawn occupant = Parent.Occupant;
				if (Props.operatingEffecter != null) {
					if (!Parent.IsPowered) {
						operatingEffecter?.Cleanup();
						operatingEffecter = null;
					} else {
						if (operatingEffecter == null)
						{
							operatingEffecter = Props.operatingEffecter.Spawn();
							ColorizeEffecter(operatingEffecter, Props.operatingColor);
							operatingEffecter.Trigger(Parent, new TargetInfo(Parent.InteractionCell, Parent.Map));
						}
						operatingEffecter.EffectTick(Parent, new TargetInfo(Parent.InteractionCell, Parent.Map));
					}
				}
			}
			EffectCycle++;
			if (EffectCycle == int.MaxValue) { EffectCycle = int.MinValue; }
		}

		private void ColorizeEffecter(Effecter effecter, Color color) {
			foreach (SubEffecter child in effecter.children) {
				SubEffecter_Sprayer subEffecter_Sprayer;
				if ((subEffecter_Sprayer = child as SubEffecter_Sprayer) != null) {
					subEffecter_Sprayer.colorOverride = color * child.def.color;
				}
			}
		}

		public override void PostDraw()	{
			base.PostDraw();
			Rot4 rotation = Parent.Rotation;
			Vector3 s = new Vector3(Parent.def.graphicData.drawSize.x * 0.9f, 1f, Parent.def.graphicData.drawSize.y * 0.9f);
			Vector3 drawPos = Parent.DrawPos;
			drawPos.y -= 0.08108108f;
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, rotation.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, PodInteriorMat, 0);
			if (Parent.IsOccupied) {
				Pawn occupant = Parent.Occupant;
				Vector3 drawLoc = drawPos + FloatingOffset();
				Rot4 rotation2 = rotation;
				if (rotation2 == Rot4.East || rotation2 == Rot4.West) {
					drawLoc.z += 0.2f;
				}
				occupant.Drawer.renderer.RenderPawnAt(drawLoc, null, neverAimWeapon: true);
			}
		}

		private Vector3 FloatingOffset() {
			float num = EffectCycle % 500f / 500f;
			float num2 = Mathf.Sin((float)Math.PI * num);
			float num3 = num2 * num2 * 0.04f;
			return new Vector3(0f, 0f, Parent.IsPowered ? num3 : 0f);
		}
	}
}