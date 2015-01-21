namespace Workshop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;
    using KAS;

    public class OseModuleWorkshop : PartModule
    {
        private int _selectedPartIndex;
        private AvailablePart _selectedPart;
        private readonly List<AvailablePart> _availableParts = new List<AvailablePart>();

        [KSPField(guiName = "Available Parts", guiActive = true, guiActiveEditor = true)]
        public int AvailablePartsCount;

        [KSPField(guiName = "Selected Part", guiActive = true)]
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
            _selectedPart = _availableParts[_selectedPartIndex];
            SelectedPartTitle = _availableParts[_selectedPartIndex].title;
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
            _selectedPart = _availableParts[_selectedPartIndex];
            SelectedPartTitle = _availableParts[_selectedPartIndex].title;
        }

        [KSPEvent(guiActive = true, guiName = "Build Item")]
        public void ContextMenuOnBuildItem()
        {
            try
            {
                if (_selectedPart == null)
                {
                    throw new Exception("No part selected");
                }

                var container = part.Modules.OfType<KASModuleContainer>().First();
                if (container == null)
                {
                    throw new Exception("No KAS Container found");

                }

                var item = KASModuleContainer.PartContent.Get(container.contents, _selectedPart.name);
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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            try
            {
                foreach (var availablePart in PartLoader.LoadedPartsList)
                {
                    if (availablePart.partPrefab.Modules != null && availablePart.partPrefab.Modules.OfType<KASModuleGrab>().Any())
                    {
                        _availableParts.Add(availablePart);
                    }
                }
                AvailablePartsCount = _availableParts.Count;
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop - " + ex.Message);
            }
        }
    }
}
