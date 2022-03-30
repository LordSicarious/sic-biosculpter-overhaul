// RimWorld.Gizmo_BiorefuelableBiofuelStatus
using RimWorld;
using UnityEngine;
using Verse;

namespace BiosculpterOverhaul {
    [StaticConstructorOnStartup]
    public class Gizmo_BiorefuelableFuelStatus : Gizmo
    {
        public CompBiorefuelable biorefuelable;
        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);
        private static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");
        private const float ArrowScale = 0.5f;
        public Gizmo_BiorefuelableFuelStatus() {
            order = -100f;
        }

        public override float GetWidth(float maxWidth) {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
            {
                Rect rect;
                Rect rect2 = (rect = overRect.AtZero().ContractedBy(6f));
                rect.height = overRect.height / 2f;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect, biorefuelable.Props.BiofuelGizmoLabel);
                Rect rect3 = rect2;
                rect3.yMin = overRect.height / 2f;
                float fillPercent = biorefuelable.Biofuel / biorefuelable.Props.biofuelCapacity;
                Widgets.FillableBar(rect3, fillPercent, FullBarTex, EmptyBarTex, doBorder: false);
                if (biorefuelable.Props.targetBiofuelLevelConfigurable)
                {
                    float num = biorefuelable.TargetBiofuelLevel / biorefuelable.Props.biofuelCapacity;
                    float x = rect3.x + num * rect3.width - (float)TargetLevelArrow.width * 0.5f / 2f;
                    float y = rect3.y - (float)TargetLevelArrow.height * 0.5f;
                    GUI.DrawTexture(new Rect(x, y, (float)TargetLevelArrow.width * 0.5f, (float)TargetLevelArrow.height * 0.5f), TargetLevelArrow);
                }
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect3, biorefuelable.Biofuel.ToString("F0") + " / " + biorefuelable.Props.biofuelCapacity.ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
            });
            return new GizmoResult(GizmoState.Clear);
        }
    }
}