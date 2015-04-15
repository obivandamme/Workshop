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
        public float ElectricChargePerSecond = 25;

        [KSPField]
        public float RocketPartsPerSecond = 0.1f;

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Status", guiActive = true)]
        public string Status = "Online";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Progress", guiUnits = "%", guiFormat = "F1")]
        [UI_ProgressBar(minValue = 0, maxValue = 100F)]
        public float Progress = 0;

        [KSPEvent(guiActive = true, guiName = "Open Workbench")]
        public void ContextMenuOnOpenWorkbench()
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
                        FinishManufacturing();
                    }
                    else
                    {
                        ExecuteManufacturing(deltaTime);
                    }
                }
                else
                {
                    StartManufacturing();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnUpdate - " + ex.Message);
            }
            base.OnUpdate();
        }

        private void StartManufacturing()
        {
            var nextQueuedPart = _queue.Pop();
            if (nextQueuedPart != null)
            {
                _rocketPartsNeeded = nextQueuedPart.GetRocketPartsNeeded();
                BuiltPart = nextQueuedPart;
            }
        }

        private void ExecuteManufacturing(double deltaTime)
        {
            var partsNeeded = deltaTime * RocketPartsPerSecond;
            var ecNeeded = deltaTime * ElectricChargePerSecond;
            var preRequisitesMessage = CheckPrerequisites(partsNeeded, ecNeeded);

            if (preRequisitesMessage != "Ok")
            {
                Status = preRequisitesMessage;
            }
            else
            {
                Status = "Building " + BuiltPart.title;
                _broker.RequestResource(part, "ElectricCharge", ecNeeded);
                _rocketPartsUsed += _broker.RequestResource(part, "RocketParts", partsNeeded);
            }
            Progress = (float)(_rocketPartsUsed / _rocketPartsNeeded * 100);
        }

        private void FinishManufacturing()
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

        public override void OnInactive()
        {
            _window.Visible = false;
            base.OnInactive();
        }

        private string CheckPrerequisites(double partsNeeded, double ecNeeded)
        {
            if (part.protoModuleCrew.Count < MinimumCrew)
            {
                return "Not enough Crew to operate";
            }
            if (_broker.AmountAvailable(part, "RocketParts") < partsNeeded)
            {
                return "Not enough Rocket Parts";
            }
            if (_broker.AmountAvailable(part, "ElectricCharge") < ecNeeded)
            {
                return "Not enough Electric Charge";
            }
            return "Ok";
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
            var kisModuleContainers = part.FindModulesImplementing<ModuleKISInventory>();

            if (kisModuleContainers == null || kisModuleContainers.Count == 0)
            {
                throw new Exception("No KIS Container found");
            }

            foreach (var container in kisModuleContainers)
            {
                if (container.GetContentVolume() + KIS_Shared.GetPartVolume(availablePart.partPrefab) < container.maxVolume)
                {
                    container.AddItem(availablePart.partPrefab);
                    return true;
                }
            }
            return false;
        }
    }
}
