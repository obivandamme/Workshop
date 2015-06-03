namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using UnityEngine;

    public class OseModuleRecycler : PartModule
    {
        private WorkshopItem _builtPart;
        private double _massProcessed;
        private float _progress;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;
        private readonly List<Demand> _upkeep;

        // GUI Properties
        private readonly int _windowId;
        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;
        private bool _showGui;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string Upkeep = "";

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Recycler Status", guiActive = true)]
        public string Status = "Online";

        [KSPEvent(guiActive = true, guiName = "Open Recycler")]
        public void ContextMenuOnOpenWorkbench()
        {
            if (_showGui)
            {
                foreach (var inventory in part.vessel.FindPartModulesImplementing<ModuleKISInventory>().Where(i => i.showGui == false).ToList())
                {
                    foreach (var item in inventory.items)
                    {
                        item.Value.DisableIcon();
                    }
                }
                _showGui = false;
            }
            else
            {
                foreach (var inventory in part.vessel.FindPartModulesImplementing<ModuleKISInventory>())
                {
                    foreach (var item in inventory.items)
                    {
                        item.Value.EnableIcon(128);
                    }
                }
                _showGui = true;
            }
        }

        public OseModuleRecycler()
        {
            _windowId = new System.Random().Next(65536);
            _clock = new Clock();
            _queue = new WorkshopQueue();
            _upkeep = new List<Demand>();
        }

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselChange.Add(this.OnVesselChange);
            base.OnStart(state);
        }

        public override void OnLoad(ConfigNode node)
        {
            LoadModuleState(node);
            LoadUpkeep();
            base.OnLoad(node);
        }

        private void LoadModuleState(ConfigNode node)
        {
            foreach (ConfigNode cn in node.nodes)
            {
                if (cn.name == "BUILTPART" && cn.HasValue("Name") && cn.HasValue("MassProcessed"))
                {
                    var availablePart = PartLoader.getPartInfoByName(cn.GetValue("Name"));
                    if (availablePart != null)
                    {
                        _builtPart = new WorkshopItem(availablePart);
                        _builtPart.EnableIcon();
                        _massProcessed = double.Parse(cn.GetValue("MassProcessed"));
                    }
                }
                if (cn.name == "QUEUEDPART" && cn.HasValue("Name"))
                {
                    var availablePart = PartLoader.getPartInfoByName(cn.GetValue("Name"));
                    var item = new WorkshopItem(availablePart);
                    _queue.Add(item);
                }
            }
        }

        private void LoadUpkeep()
        {
            var resources = Upkeep.Split(',');
            for (var i = 0; i < resources.Length; i += 2)
            {
                _upkeep.Add(new Demand
                {
                    ResourceName = resources[i],
                    Ratio = float.Parse(resources[i + 1])
                });
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (_builtPart != null)
            {
                var builtPartNode = node.AddNode("BUILTPART");
                builtPartNode.AddValue("Name", _builtPart.Part.name);
                builtPartNode.AddValue("MassProcessed", _massProcessed);
            }

            foreach (var queuedPart in _queue)
            {
                var queuedPartNode = node.AddNode("QUEUEDPART");
                queuedPartNode.AddValue("Name", queuedPart.Part.name);
            }

            base.OnSave(node);
        }

        public override void OnUpdate()
        {
            var deltaTime = _clock.GetDeltaTime();
            try
            {
                if (this._progress >= 100)
                {
                    FinishManufacturing();
                }
                else if (_builtPart != null)
                {
                    ExecuteManufacturing(deltaTime);
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
                _builtPart = nextQueuedPart;
                _builtPart.EnableIcon();
            }
        }

        private void ExecuteManufacturing(double deltaTime)
        {
            var preRequisitesMessage = CheckPrerequisites(deltaTime);

            if (preRequisitesMessage != "Ok")
            {
                Status = preRequisitesMessage;
            }
            else
            {
                Status = "Building " + _builtPart.Part.title;
                foreach (var res in _upkeep)
                {
                    this.StoreResource(res.ResourceName, res.Ratio * deltaTime);
                }

                //Consume Recipe Input
                var demand = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().Demand;
                var totalRatio = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().TotalRatio;
                foreach (var res in demand)
                {
                    var resourcesUsed = this.StoreResource(res.ResourceName, (res.Ratio / totalRatio) * deltaTime * ProductivityFactor);
                    _massProcessed += resourcesUsed * res.Density;
                }
            }

            this._progress = (float)(_massProcessed / _builtPart.Part.partPrefab.mass * 100);
        }

        public double AmountAvailable(string resource)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            return resList.Sum(r => r.amount);
        }

        public double StoreResource(string resource, double amount)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            var demandLeft = amount;
            var amountStored = 0d;

            foreach (var r in resList)
            {
                if (r.maxAmount - r.amount > demandLeft)
                {
                    r.amount += demandLeft;
                    amountStored += demandLeft;
                    demandLeft = 0;
                }
                else
                {
                    var amountToStore = r.maxAmount - r.amount;
                    r.amount += amountToStore;
                    demandLeft -= amountToStore;
                    amountStored += amountToStore;
                }
            }

            return amountStored;
        }

        private void FinishManufacturing()
        {
            _builtPart.DisableIcon();
            _builtPart = null;
            _massProcessed = 0;
            this._progress = 0;
            Status = "Online";
        }

        public override void OnInactive()
        {
            if (_showGui)
            {
                ContextMenuOnOpenWorkbench();
            }
            base.OnInactive();
        }

        void OnVesselChange(Vessel v)
        {
            if (_showGui)
            {
                this.ContextMenuOnOpenWorkbench();
            }
        }

        private string CheckPrerequisites(double deltaTime)
        {
            if (this.part.protoModuleCrew.Count < MinimumCrew)
            {
                return "Not enough Crew to operate";
            }

            foreach (var res in _upkeep)
            {
                if (this.AmountAvailable(res.ResourceName) < (res.Ratio * deltaTime))
                {
                    return "Not enough " + res.ResourceName;
                }
            }

            return "Ok";
        }

        // ReSharper disable once UnusedMember.Local => Unity3D
        // ReSharper disable once InconsistentNaming => Unity3D
        void OnGUI()
        {
            if (_showGui)
            {
                DrawWindow();
            }
        }

        private void DrawWindow()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            _windowPos = GUILayout.Window(
                   _windowId,
                   _windowPos,
                   DrawWindowContents,
                   "Recycler Menu",
                   GUILayout.ExpandWidth(true),
                   GUILayout.ExpandHeight(true),
                   GUILayout.MinWidth(64),
                   GUILayout.MinHeight(64));
        }

        private void DrawWindowContents(int windowId)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawAvailableItems();
            DrawQueuedItems();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            DrawBuiltItem();

            if (GUI.Button(new Rect(_windowPos.width - 24, 4, 20, 20), "X"))
            {
                ContextMenuOnOpenWorkbench();
            }

            GUI.DragWindow();
        }

        private void DrawAvailableItems()
        {
            GUILayout.BeginVertical();
            _scrollPosItems = GUILayout.BeginScrollView(_scrollPosItems, WorkshopStyles.Databox(), GUILayout.Width(400f), GUILayout.Height(250f));
            foreach (var inventory in part.vessel.FindPartModulesImplementing<ModuleKISInventory>())
            {
                foreach (var item in inventory.items)
                {
                    GUILayout.BeginHorizontal();
                    WorkshopGui.ItemThumbnail(item.Value.icon);
                    WorkshopGui.ItemDescription(item.Value.availablePart);
                    if (GUILayout.Button("Queue", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                    {
                        var queuedItem = new WorkshopItem(item.Value.availablePart);
                        queuedItem.EnableIcon();
                        this._queue.Add(queuedItem);
                        inventory.DeleteItem(item.Key);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawQueuedItems()
        {
            GUILayout.BeginVertical();
            _scrollPosQueue = GUILayout.BeginScrollView(_scrollPosQueue, WorkshopStyles.Databox(), GUILayout.Width(400f), GUILayout.Height(250f));
            foreach (var item in this._queue)
            {
                GUILayout.BeginHorizontal();
                WorkshopGui.ItemThumbnail(item.Icon);
                WorkshopGui.ItemDescription(item.Part);
                if (GUILayout.Button("Remove", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                {
                    item.DisableIcon();
                    this._queue.Remove(item);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawBuiltItem()
        {
            GUILayout.BeginHorizontal();
            if (_builtPart != null)
            {
                WorkshopGui.ItemThumbnail(_builtPart.Icon);
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(50));
            }
            WorkshopGui.ProgressBar(this._progress);
            GUILayout.EndHorizontal();
        }
    }
}
