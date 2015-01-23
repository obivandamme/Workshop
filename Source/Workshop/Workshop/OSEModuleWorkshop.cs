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
        private AvailablePart _builtPart;
        private AvailablePart _selectedPart;
        private readonly List<AvailablePart> _availableParts = new List<AvailablePart>();
        private readonly ResourceBroker _broker = new ResourceBroker();
        private readonly OseClock _clock = new OseClock();

        [KSPField]
        public double ElectricChargePerSecond = 25;

        [KSPField]
        public double SparePartsPerSecond = 0.1;

        [KSPField]
        public int MinimumCrew = 2;

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
                _selectedPart = _selectedPart == null ? _availableParts.First() : _availableParts.NextOf(_selectedPart);
                _sparePartsNeeded = _selectedPart.GetRocketPartsNeeded();
                SelectedPartTitle = _selectedPart.title;
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
                _selectedPart = _selectedPart == null ? _availableParts.Last() : _availableParts.PreviousOf(_selectedPart);
                _sparePartsNeeded = _selectedPart.GetRocketPartsNeeded();
                SelectedPartTitle = _selectedPart.title;
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
                    if (availablePart.HasStorableKasModule())
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
                        if (part.protoModuleCrew.Count < MinimumCrew)
                        {
                            Status = "Not enough Crew to operate";
                        }
                        else if (_broker.AmountAvailable(part, "RocketParts") < partsNeeded)
                        {
                            Status = "Not enough Rocket Parts";
                        }
                        else if (_broker.AmountAvailable(part, "ElectricCharge") < ecNeeded)
                        {
                            Status = "Not enough Electric Charge";
                        }
                        else
                        {
                            Status = "Producing...";
                            _broker.RequestResource(part, "ElectricCharge", ecNeeded);
                            _sparePartsUsed += _broker.RequestResource(part, "RocketParts", partsNeeded);
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

        private void AddToContainer(AvailablePart availablePart)
        {
            var kasModuleContainers = vessel.FindPartModulesImplementing<KASModuleContainer>();
            if (kasModuleContainers == null)
            {
                throw new Exception("No KAS Container found");

            }

            var kasModuleGrab = availablePart.partPrefab.Modules.OfType<KASModuleGrab>().First();

            foreach (var container in kasModuleContainers)
            {
                if (container.totalSize + kasModuleGrab.storedSize < container.maxSize)
                {
                    var item = KASModuleContainer.PartContent.Get(container.contents, availablePart.name);
                    if (item == null)
                    {
                        throw new Exception("PartContent.Get did not return part");
                    }

                    item.pristine_count += 1;
                    container.part.mass += item.totalMass;
                    container.totalSize += item.totalSize;
                    break;
                }
            }
        }
    }
}
