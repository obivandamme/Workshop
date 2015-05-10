using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Workshop
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class OseAddonEditorFilter : MonoBehaviour
    {
        private static List<AvailablePart> avPartItems = new List<AvailablePart>();
        internal string category = "Filter by Function";
        internal string subCategoryTitle = "Workshop Items";
        internal string iconName = "R&D_node_icon_advmetalworks";

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

            avPartItems.Clear();
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (avPart.partPrefab && avPart.partPrefab.GetComponent<OseModuleWorkshop>()) 
                {
                    avPartItems.Add(avPart);
                }
            }
        }

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            if (avPartItems.Contains(avPart))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SubCategories()
        {
            RUI.Icons.Selectable.Icon icon = PartCategorizer.Instance.iconLoader.GetIcon(iconName);
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, icon, p => EditorItemsFilter(p));

            RUIToggleButtonTyped button = Filter.button.activeButton;
            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
