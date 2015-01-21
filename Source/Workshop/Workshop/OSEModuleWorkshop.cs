namespace Workshop
{
    using System;
    using System.Linq;

    using UnityEngine;
    using KAS;

    public class OseModuleWorkshop : PartModule
    {
        private int _selectedPartIndex;
        private string _selectedPartName = "N/A";

        [KSPField(guiName = "Selected Part: ", guiActive = true)]
        public string SelectedPartTitle = "N/A";

        [KSPEvent(guiActive = true, guiName = "Next")]
        public void ContextMenuNextPart()
        {
            if (_selectedPartIndex == PartLoader.LoadedPartsList.Count - 1)
            {
                _selectedPartIndex = 0;
            }
            else
            {
                _selectedPartIndex += 1;
            }
            _selectedPartName = PartLoader.LoadedPartsList[_selectedPartIndex].name;
            SelectedPartTitle = PartLoader.LoadedPartsList[_selectedPartIndex].title;
        }

        [KSPEvent(guiActive = true, guiName = "Previous")]
        public void ContextMenuPreviousPart()
        {
            if (_selectedPartIndex == 0)
            {
                _selectedPartIndex = PartLoader.LoadedPartsList.Count - 1;
            }
            else
            {
                _selectedPartIndex -= 1;
            }
            _selectedPartName = PartLoader.LoadedPartsList[_selectedPartIndex].name;
            SelectedPartTitle = PartLoader.LoadedPartsList[_selectedPartIndex].title;
        }

        [KSPEvent(guiActive = true, guiName = "Build Item")]
        public void ContextMenuOnCreateStrut()
        {
            try
            {
                if (_selectedPartName == "N/A")
                {
                    throw new Exception("No part selected");
                }

                var avPart = PartLoader.getPartInfoByName(_selectedPartName);
                if (avPart == null)
                {
                    throw new Exception("No Available Part found");
                }

                var container = part.Modules.OfType<KASModuleContainer>().First();
                if (container == null)
                {
                    throw new Exception("No KAS Container found");

                }

                var item = KASModuleContainer.PartContent.Get(container.contents, avPart.name);
                if (item == null)
                {
                    throw new Exception("PartContent.Get did not return part");
                }

                item.pristine_count += 1;
                container.part.mass += item.totalMass;
                container.totalSize += item.totalSize;
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop - " + ex.Message);
            }
        }
    }
}
