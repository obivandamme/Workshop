using KSP.Localization;
using System.Linq;

namespace Workshop
{
    public class OseModuleInventoryPreference : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool IsFavored = false;

        [KSPEvent(guiActive = false, guiName = "#LOC_Workshop_InventoryModule_FavorInventory")] // Favor Inventory
        public void ContextMenuOnFavorInventory()
        {
            if (IsFavored)
            {
                IsFavored = false;
                Events["ContextMenuOnFavorInventory"].guiName = Localizer.GetStringByTag("LOC_Workshop_InventoryModule_FavorInventory"); //"Favor Inventory";
            }
            else
            {
                IsFavored = true;
                Events["ContextMenuOnFavorInventory"].guiName = Localizer.GetStringByTag("LOC_Workshop_InventoryModule_UnfavorInventory");  //"Unfavor Inventory";
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
                Events["ContextMenuOnFavorInventory"].guiName = Localizer.GetStringByTag("LOC_Workshop_InventoryModule_UnfavorInventory"); //"Unfavor Inventory";
            }
        }

        private bool VesselHasWorkshop()
        {
            return HighLogic.LoadedSceneIsFlight && vessel.Parts.Any(p => p.GetComponent<OseModuleWorkshop>() != null);
        }
    }
}
