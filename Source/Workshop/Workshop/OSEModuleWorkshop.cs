namespace Workshop
{
    using System;

    using KIS;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        public AvailablePart BuiltPart;

        private double _rocketPartsUsed;
        private double _rocketPartsNeeded;

        private readonly Clock _clock;
        private readonly ResourceBroker _broker;
        private readonly WorkshopWindow _window;
        private readonly WorkshopQueue _queue;

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

        [KSPEvent(guiActive = true, guiName = "Select item")]
        public void ContextMenuOnSelectItem()
        {
            _window.Visible = true;
        }

        public OseModuleWorkshop()
        {
            _clock = new Clock();
            _broker = new ResourceBroker();
            _queue = new WorkshopQueue();
            _window = new WorkshopWindow(_queue);
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
                            Status = "Building " + BuiltPart.title;
                            _broker.RequestResource(part, "ElectricCharge", ecNeeded);
                            _rocketPartsUsed += _broker.RequestResource(part, "RocketParts", partsNeeded);
                        }
                        Progress = (float)(_rocketPartsUsed / _rocketPartsNeeded * 100);
                    }
                }
                else
                {
                    var nextQueuedPart = _queue.Pop();
                    if (nextQueuedPart != null)
                    {
                        OnPartSelected(nextQueuedPart);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnUpdate - " + ex.Message);
            }
            base.OnUpdate();
        }

        public override void OnInactive()
        {
            _window.Visible = false;
            base.OnInactive();
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
            var kasModuleContainers = vessel.FindPartModulesImplementing<ModuleKISInventory>();

            if (kasModuleContainers == null || kasModuleContainers.Count == 0)
            {
                throw new Exception("No KAS Container found");
            }

            foreach (var container in kasModuleContainers)
            {
                if (container.totalVolume + KIS_Shared.GetPartVolume(availablePart.partPrefab) < container.maxVolume)
                {
                    container.AddItem(availablePart, availablePart.internalConfig);
                    return true;
                }
            }
            return false;
        }

        public void OnPartSelected(AvailablePart availablePart)
        {
            _rocketPartsNeeded = availablePart.GetRocketPartsNeeded();
            BuiltPart = availablePart;
        }
    }
}
