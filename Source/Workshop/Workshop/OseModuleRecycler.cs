namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using UnityEngine;

    public class OseModuleRecycler : PartModule
    {
        private WorkshopItem _scrappedPart;
        private double _massProcessed;
        private float _progress;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;
        private readonly List<Demand> _upkeep;

        // GUI Properties
        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;
        private bool _showGui;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string ResourceName = "RocketParts";

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
                _showGui = true;
            }
        }

        public OseModuleRecycler()
        {
            _clock = new Clock();
            _queue = new WorkshopQueue();
            _upkeep = new List<Demand>();
        }

        public override void OnStart(StartState state)
        {
            if (WorkshopSettings.IsKISAvailable)
            {
                GameEvents.onVesselChange.Add(this.OnVesselChange);
            }
            else
            {
                this.Fields["Status"].guiActive = false;
                this.Events["ContextMenuOnOpenWorkbench"].guiActive = false;
            }
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
                        this._scrappedPart = new WorkshopItem(availablePart);
                        this._scrappedPart.EnableIcon();
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
            if (this._scrappedPart != null)
            {
                var builtPartNode = node.AddNode("BUILTPART");
                builtPartNode.AddValue("Name", this._scrappedPart.Part.name);
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
                else if (this._scrappedPart != null)
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
                this._scrappedPart = nextQueuedPart;
                this._scrappedPart.EnableIcon();
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
                Status = "Scrapping " + this._scrappedPart.Part.title;

                //Consume Upkeep
                foreach (var res in _upkeep)
                {
                    this.StoreResource(res.ResourceName, res.Ratio * deltaTime);
                }

                //Consume Recipe Input
                var density = PartResourceLibrary.Instance.GetDefinition(this.ResourceName).density;
                var resourcesUsed = this.StoreResource(ResourceName, deltaTime * ProductivityFactor);
                _massProcessed += resourcesUsed * density;
            }

            this._progress = (float)(_massProcessed / this._scrappedPart.Part.partPrefab.mass * 100);
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
            this._scrappedPart.DisableIcon();
            this._scrappedPart = null;
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
                   GetInstanceID(),
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
                    if (item.Value.icon == null)
                    {
                        item.Value.EnableIcon(128);
                    }
                    GUILayout.BeginHorizontal();
                    WorkshopGui.ItemThumbnail(item.Value.icon);
                    WorkshopGui.ItemDescription(item.Value.availablePart, ResourceName);
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
                WorkshopGui.ItemDescription(item.Part, ResourceName);
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
            if (this._scrappedPart != null)
            {
                WorkshopGui.ItemThumbnail(this._scrappedPart.Icon);
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
