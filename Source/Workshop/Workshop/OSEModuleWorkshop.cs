namespace Workshop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using KAS;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        public AvailablePart BuiltPart;

        private double _rocketPartsUsed;
        private double _rocketPartsNeeded;

        private readonly OseClock _clock;
        private readonly ResourceBroker _broker;
        private readonly OseWorkshopWindow _window;

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

        [KSPEvent(guiActive = true, guiName = "Select item")]
        public void ContextMenuOnSelectItem()
        {
            _window.Visible = true;
        }

        public OseModuleWorkshop()
        {
            _clock = new OseClock();
            _broker = new ResourceBroker();
            _window = new OseWorkshopWindow(this);
        }

        public override void OnLoad(ConfigNode node)
        {
            Reset();
        }

        public override void OnUpdate()
        {
            var deltaTime = _clock.GetDeltaTime();
            try
            {
                if (BuiltPart != null)
                {
                    if (Progress >= 100)
                    {
                        if (AddToContainer(BuiltPart))
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
                            _rocketPartsUsed += _broker.RequestResource(part, "RocketParts", partsNeeded);
                        }
                        Progress = (float)(_rocketPartsUsed / _rocketPartsNeeded * 100);
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
            BuiltPart = null;
            _rocketPartsUsed = 0;
            Progress = 0;
            Status = "Online";
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

        public void OnPartSelected(AvailablePart availablePart)
        {
            SelectedPartTitle = availablePart.title;
            _rocketPartsNeeded = availablePart.GetRocketPartsNeeded();
            BuiltPart = availablePart;
        }

        public IEnumerable<AvailablePart> GetStorableParts()
        {
            return PartLoader.LoadedPartsList.Where(availablePart => availablePart.HasStorableKasModule());
        }
    }
}
