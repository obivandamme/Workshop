using System.Linq;

namespace Workshop
{
    public class OseModuleInventoryPreference : PartModule
    {
        [KSPField(isPersistant = true)]
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
            return HighLogic.LoadedSceneIsFlight && this.vessel.Parts.Any(p => p.GetComponent<OseModuleWorkshop>() != null);
        }
    }
}
