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
        private readonly OseClock _clock = new OseClock();

        [KSPField]
        public double ElectricChargePerSecond = 25;

        [KSPField]
        public double RocketPartsPerSecond = 0.1;

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Status", guiActive = true)]
        public string Status = "Online";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Progress", guiUnits = "%", guiFormat = "F1")]
        [UI_ProgressBar(minValue = 0, maxValue = 100F)]
        public float Progress = 0;

        [KSPField(guiName = "Selected Part", guiActive = true)]
        public string SelectedPartTitle = "N/A";

        [KSPEvent(guiActive = true, guiName = "Next")]
        public void ContextMenuOnNextItem()
        {
            try
            {
                var availableParts = GetStorableParts().ToList();
                _selectedPart = _selectedPart == null ? availableParts.First() : availableParts.NextOf(_selectedPart);
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
                var availableParts = GetStorableParts().ToList();
                _selectedPart = _selectedPart == null ? availableParts.Last() : availableParts.PreviousOf(_selectedPart);
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

        public override void OnUpdate()
        {
            var deltaTime = _clock.GetDeltaTime();
            try
            {
                if (_builtPart != null)
                {
                    if (Progress >= 100)
                    {
                        if (AddToContainer(_builtPart))
                        {
                            Reset();
                        }
                        else
                        {
                            Status = "Not enough free space";
                        }
                    }
                    else
                    {
                        var partsNeeded = deltaTime * RocketPartsPerSecond;
                        var ecNeeded = deltaTime * ElectricChargePerSecond;
                        if (part.protoModuleCrew.Count < MinimumCrew)
                        {
                            Status = "Not enough Crew to operate";
                        }
                        else if (AmountAvailable("RocketParts") < partsNeeded)
                        {
                            Status = "Not enough Rocket Parts";
                        }
                        else if (AmountAvailable("ElectricCharge") < ecNeeded)
                        {
                            Status = "Not enough Electric Charge";
                        }
                        else
                        {
                            Status = "Producing...";
                            RequestResource("ElectricCharge", ecNeeded);
                            _sparePartsUsed += RequestResource("RocketParts", partsNeeded);
                        }
                        Progress = (float)(_sparePartsUsed / _sparePartsNeeded * 100);
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

            Fields["Progress"].guiActive = false;
            Events["ContextMenuOnBuildItem"].guiActive = true;
            Events["ContextMenuOnNextItem"].guiActive = true;
            Events["ContextMenuOnPreviousItem"].guiActive = true;
        }

        private void StartProduction(AvailablePart availablePart)
        {
            _builtPart = availablePart;

            Fields["Progress"].guiActive = true;
            Events["ContextMenuOnBuildItem"].guiActive = false;
            Events["ContextMenuOnNextItem"].guiActive = false;
            Events["ContextMenuOnPreviousItem"].guiActive = false;
        }

        private bool AddToContainer(AvailablePart availablePart)
        {
            var kasModuleContainers = vessel.FindPartModulesImplementing<KASModuleContainer>();

            if (kasModuleContainers == null || kasModuleContainers.Count == 0)
            {
                throw new Exception("No KAS Container found");
            }

            var kasModuleGrab = availablePart.partPrefab.Modules.OfType<KASModuleGrab>().First();

            foreach (var container in kasModuleContainers)
            {
                if (container.totalSize + kasModuleGrab.storedSize < container.maxSize)
                {
                    var item = KASModuleContainer.PartContent.Get(container.contents, availablePart.name);
                    item.pristine_count += 1;
                    container.part.mass += item.totalMass;
                    container.totalSize += item.totalSize;
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<AvailablePart> GetStorableParts()
        {
            return PartLoader.LoadedPartsList.Where(availablePart => availablePart.HasStorableKasModule());
        }

        private double AmountAvailable(string resName)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resName);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            return resList.Sum(r => r.amount);
        }

        private double RequestResource(string resName, double resAmount)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resName);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            var demandLeft = resAmount;
            var amountTaken = 0d;

            foreach (var r in resList)
            {
                if (r.amount >= demandLeft)
                {
                    amountTaken += demandLeft;
                    r.amount -= demandLeft;
                    demandLeft = 0;
                }
                else
                {
                    amountTaken += r.amount;
                    demandLeft -= r.amount;
                    r.amount = 0;
                }
            }

            return amountTaken;
        }
    }
}
