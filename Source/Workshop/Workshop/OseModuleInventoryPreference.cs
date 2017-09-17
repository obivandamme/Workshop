using System.Linq;

namespace Workshop
{
    public class OseModuleInventoryPreference : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool IsFavored = false;

        [KSPEvent(guiActive = false, guiName = "Favor Inventory")]
        public void ContextMenuOnFavorInventory()
        {
            if (IsFavored)
            {
                IsFavored = false;
                Events["ContextMenuOnFavorInventory"].guiName = "Favor Inventory";
            }
            else
            {
                IsFavored = true;
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
            if (IsFavored)
            {
                Events["ContextMenuOnFavorInventory"].guiName = "Unfavor Inventory";
            }
        }

        private bool VesselHasWorkshop()
        {
            return HighLogic.LoadedSceneIsFlight && vessel.Parts.Any(p => p.GetComponent<OseModuleWorkshop>() != null);
        }
    }
}
