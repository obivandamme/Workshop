using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.UI;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class OseAddonEditorFilter : MonoBehaviour
    {
        private static readonly List<string> AvPartItems = new List<string>();
        internal string Category = "Filter by Function";
        internal string SubCategoryTitle = "Workshop Items";
        internal string IconName = "R&D_node_icon_advmetalworks";

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

            AvPartItems.Clear();
            AvPartItems.AddRange(new []
                                 {
                                     "OSE.Workshop",
                                     "OSE.Converter",
                                     "ose3000",
                                     "ose6000"
                                 });
        }

        private void SubCategories()
        {
            var icon = PartCategorizer.Instance.iconLoader.GetIcon(IconName);
            var filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == Category);
            PartCategorizer.AddCustomSubcategoryFilter(filter, SubCategoryTitle, icon, p => AvPartItems.Contains(p.name));

            var button = filter.button.activeButton;

            //button.SetState(true, RUIToggleButtonTyped.ClickType.FORCED, null);
            //button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            //button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
