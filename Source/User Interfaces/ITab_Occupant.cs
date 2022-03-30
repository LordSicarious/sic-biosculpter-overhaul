// BiosculpterOverhaul.ITab_Occupant
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BiosculpterOverhaul {
    public class ITab_Occupant : ITab_ContentsCasket {
        public ITab_Occupant() {
            labelKey = "Occupant";
            containedItemsKey = "Occupant";
            canRemoveThings = false;
        }
    }
}