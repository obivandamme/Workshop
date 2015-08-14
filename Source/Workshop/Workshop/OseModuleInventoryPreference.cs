using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workshop
{
    public class OseModuleInventoryPreference : PartModule
    {
        [KSPField(guiActive = true, guiName = "OSE Debug", isPersistant = true)]
        public bool isFavored = false;

        [KSPEvent(guiActive = false, guiName = "Favor Inventory")]
        public void ContextMenuOnFavorInventory()
        {
            if (isFavored)
            {
                isFavored = false;
                Events["ContextMenuOnFavorInventory"].guiName = "Favor Inventory";
            }
            else
            {
                isFavored = true;
                Events["ContextMenuOnFavorInventory"].guiName = "Unfavor Inventory";
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (VesselHasWorkshop())
            {
                Events["ContextMenuOnFavorInventory"].guiActive = true;
            }
            if (isFavored)
            {
                Events["ContextMenuOnFavorInventory"].guiName = "Unfavor Inventory";
            }
        }

        private bool VesselHasWorkshop()
        {
            if (this.part.vessel == null)
                return false;
            if (this.part.vessel.Parts == null)
                return false;

            foreach (var part in vessel.Parts)
            {
                if (part.GetComponent<OseModuleWorkshop>() != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
