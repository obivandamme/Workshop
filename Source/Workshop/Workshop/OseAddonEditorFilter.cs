using System.Collections.Generic;
using UnityEngine;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class OseAddonEditorFilter : MonoBehaviour
    {
        private static readonly List<AvailablePart> AvPartItems = new List<AvailablePart>();
        internal string Category = "Filter by Function";
        internal string SubCategoryTitle = "Workshop Items";
        internal string IconName = "R&D_node_icon_advmetalworks";

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

            AvPartItems.Clear();
            foreach (var avPart in PartLoader.LoadedPartsList)
            {
                if (avPart.partPrefab && avPart.partPrefab.GetComponent<OseModuleWorkshop>()) 
                {
                    AvPartItems.Add(avPart);
                }
            }
        }

        private void SubCategories()
        {
            var icon = PartCategorizer.Instance.iconLoader.GetIcon(IconName);
            var filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == Category);
            PartCategorizer.AddCustomSubcategoryFilter(filter, SubCategoryTitle, icon, p => AvPartItems.Contains(p));

            var button = filter.button.activeButton;
            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
