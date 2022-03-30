// BiosculpterOverhaul.IngredientValueGetter_NutritionAndFixed
using RimWorld;
using Verse;

namespace BiosculpterOverhaul {
public class IngredientValueGetter_NutritionAndFixed : IngredientValueGetter {
	public override float ValuePerUnitOf(ThingDef t) {
		if (t.IsNutritionGivingIngestible) {
			return t.GetStatValueAbstract(StatDefOf.Nutrition);
		}
		return 1f;
	}

	public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing) {
		if (ing.IsFixedIngredient) {
            return "BillRequires".Translate(ing.GetBaseCount(), ing.filter.Summary);
        }
        return "BillRequiresNutrition".Translate(ing.GetBaseCount()) + " (" + ing.filter.Summary + ")";
	}
}
}