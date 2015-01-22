namespace Workshop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;
    using KAS;

    public class OseModuleWorkshop : PartModule
    {
        private double _sparePartsUsed;
        private double _sparePartsNeeded;
        private int _selectedPartIndex;
        private AvailablePart _builtPart;
        private AvailablePart _selectedPart;
        private readonly List<AvailablePart> _availableParts = new List<AvailablePart>();
        private readonly ResourceBroker _broker = new ResourceBroker();
        private readonly OseClock _clock = new OseClock();

        [KSPField]
        public double ElectricChargePerSecond = 25;

        [KSPField]
        public double SparePartsPerSecond = 0.1;

        [KSPField(guiName = "Status", guiActive = true)]
        public string Status = "Online";

        [KSPField(guiName = "Progress", guiActive = true, guiUnits = "%")]
        public double Progress = 0;

        [KSPField(guiName = "Selected Part", guiActive = true)]
        public string SelectedPartTitle = "N/A";

        [KSPEvent(guiActive = true, guiName = "Next")]
        public void ContextMenuOnNextItem()
        {
            try
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
                _sparePartsNeeded = GetSparePartsNeeded(_selectedPart);
                SelectedPartTitle = _availableParts[_selectedPartIndex].title;
                Debug.Log("[OSE] - Ose_ModuleWorkshop_ContextMenuNextPart - Parts Needed: " + _sparePartsNeeded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_ContextMenuNextPart - " + ex.Message);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Previous")]
        public void ContextMenuOnPreviousItem()
        {
            try
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
                _sparePartsNeeded = GetSparePartsNeeded(_selectedPart);
                SelectedPartTitle = _availableParts[_selectedPartIndex].title;
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_ContextMenuPreviousPart - " + ex.Message);
            }
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
                StartProduction(_selectedPart);
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_ContextMenuBuildItem - " + ex.Message);
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
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnStart - " + ex.Message);
            }
        }

        public override void OnUpdate()
        {
            var deltaTime = _clock.GetDeltaTime();
            try
            {
                if (_builtPart != null)
                {
                    if (Progress >= 100)
                    {
                        AddToContainer(_builtPart);
                        Reset();
                    }
                    else
                    {
                        var partsNeeded = deltaTime * SparePartsPerSecond;
                        var ecNeeded = deltaTime * ElectricChargePerSecond;
                        if (_broker.AmountAvailable(part, "SpareParts") < partsNeeded)
                        {
                            Status = "Not enough Spare Parts";
                        }
                        else if (_broker.AmountAvailable(part, "ElectricCharge") < ecNeeded)
                        {
                            Status = "Not enough Electric Charge";
                        }
                        else
                        {
                            _broker.RequestResource(part, "ElectricCharge", ecNeeded);
                            _sparePartsUsed += _broker.RequestResource(part, "SpareParts", partsNeeded);
                        }
                        Progress = _sparePartsUsed / _sparePartsNeeded * 100;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnUpdate - " + ex.Message);
            }
            base.OnUpdate();
        }

        private void Reset()
        {
            _builtPart = null;
            _sparePartsUsed = 0;
            Progress = 0;
            Status = "Online";
            Events["ContextMenuOnBuildItem"].guiActive = true; 
            Events["ContextMenuOnNextItem"].guiActive = true;
            Events["ContextMenuOnPreviousItem"].guiActive = true;
        }

        private void StartProduction(AvailablePart availablePart)
        {
            _builtPart = availablePart;
            Status = "Producing";
            Events["ContextMenuOnBuildItem"].guiActive = false;
            Events["ContextMenuOnNextItem"].guiActive = false;
            Events["ContextMenuOnPreviousItem"].guiActive = false;
        }

        private double GetSparePartsNeeded(AvailablePart availablePart)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition("SpareParts");
            var density = resource.density;
            return availablePart.partPrefab.mass / density;
        }

        private void AddToContainer(AvailablePart availablePart)
        {
            var container = part.Modules.OfType<KASModuleContainer>().First();
            if (container == null)
            {
                throw new Exception("No KAS Container found");

            }

            var item = KASModuleContainer.PartContent.Get(container.contents, availablePart.name);
            if (item == null)
            {
                throw new Exception("PartContent.Get did not return part");
            }

            item.pristine_count += 1;
            container.part.mass += item.totalMass;
            container.totalSize += item.totalSize;
        }
    }
}
