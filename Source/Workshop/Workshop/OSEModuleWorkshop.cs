namespace Workshop
{
    using System;

    using KIS;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        private AvailablePart _builtPart;
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
            foreach (ConfigNode cn in node.nodes)
            {
                if (cn.name == "BUILTPART" && cn.HasValue("Name") && cn.HasValue("RocketPartsNeeded") && cn.HasValue("RocketPartsUsed"))
                {
                    var availablePart = PartLoader.getPartInfoByName(cn.GetValue("Name"));
                    if (availablePart != null)
                    {
                        _builtPart = availablePart;
                        _rocketPartsNeeded = double.Parse(cn.GetValue("RocketPartsNeeded"));
                        _rocketPartsUsed = double.Parse(cn.GetValue("RocketPartsUsed"));
                    }
                }
                if (cn.name == "QUEUEDPART" && cn.HasValue("Name"))
                {
                    var availablePart = PartLoader.getPartInfoByName(cn.GetValue("Name"));
                    _queue.Add(availablePart);
                }
            }
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            if (_builtPart != null)
            {
                var builtPartNode = node.AddNode("BUILTPART");
                builtPartNode.AddValue("Name", _builtPart.name);
                builtPartNode.AddValue("RocketPartsNeeded", _rocketPartsNeeded);
                builtPartNode.AddValue("RocketPartsUsed", _rocketPartsUsed);
            }

            foreach (var queuedPart in _queue)
            {
                var queuedPartNode = node.AddNode("QUEUEDPART");
                queuedPartNode.AddValue("Name", queuedPart.name);
            }

            base.OnSave(node);
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
                _builtPart = nextQueuedPart;
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
                Status = "Building " + _builtPart.title;
                _broker.RequestResource(part, "ElectricCharge", ecNeeded);
                _rocketPartsUsed += _broker.RequestResource(part, "RocketParts", partsNeeded);
            }
            Progress = (float)(_rocketPartsUsed / _rocketPartsNeeded * 100);
        }

        private void FinishManufacturing()
        {
            if (AddToContainer(_builtPart))
            {
                _builtPart = null;
                _rocketPartsUsed = 0;
                _rocketPartsNeeded = 0;
                Progress = 0;
                Status = "Online";
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
