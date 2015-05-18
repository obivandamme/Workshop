namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using KSP.IO;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        private List<WorkshopItem> _items;
        private AvailablePart _builtPart;
        private double _massProcessed;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;
        private readonly List<Demand> _upkeep;

        // GUI Properties
        private readonly int _windowId;
        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;
        private Vector2 _scrollPosInventories = Vector2.zero;
        private bool _showGui;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string Upkeep = "";

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Status", guiActive = true)]
        public string Status = "Online";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Progress", guiUnits = "%", guiFormat = "F1")]
        [UI_ProgressBar(minValue = 0, maxValue = 100F)]
        public float Progress = 0;

        [KSPEvent(guiActive = true, guiName = "Print Textures")]
        public void PrintTextures()
        {
            foreach (var textureInfo in GameDatabase.Instance.databaseTexture)
            {
                if (textureInfo.name.StartsWith("Squad/PartList"))
                {
                    print(textureInfo.name);
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Open Workbench")]
        public void ContextMenuOnOpenWorkbench()
        {
            if (_showGui)
            {
                foreach (var item in _items)
                {
                    item.DisableIcon();
                }
                _showGui = false;
            }
            else
            {
                foreach (var item in _items)
                {
                    item.EnableIcon();
                }
                _showGui = true;
            }
        }

        public OseModuleWorkshop()
        {
            _windowId = new System.Random().Next(65536);
            _clock = new Clock();
            _queue = new WorkshopQueue();
            _upkeep = new List<Demand>();
        }

        public override void OnLoad(ConfigNode node)
        {
            LoadModuleState(node);
            LoadUpkeep();
            LoadAvailableParts();
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
                        _builtPart = availablePart;
                        _massProcessed = double.Parse(cn.GetValue("MassProcessed"));
                    }
                }
                if (cn.name == "QUEUEDPART" && cn.HasValue("Name"))
                {
                    var availablePart = PartLoader.getPartInfoByName(cn.GetValue("Name"));
                    _queue.Add(availablePart);
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

        private void LoadAvailableParts()
        {
            _items = PartLoader.LoadedPartsList.Where(availablePart => availablePart.HasRecipeModule()).Select(p => new WorkshopItem(p)).ToList();
        }

        public override void OnSave(ConfigNode node)
        {
            if (_builtPart != null)
            {
                var builtPartNode = node.AddNode("BUILTPART");
                builtPartNode.AddValue("Name", _builtPart.name);
                builtPartNode.AddValue("MassProcessed", _massProcessed);
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
                if (Progress >= 100)
                {
                    FinishManufacturing();
                }
                if (_builtPart != null)
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
                Status = "Building " + _builtPart.title;
                foreach (var res in _upkeep)
                {
                    RequestResource(res.ResourceName, res.Ratio * deltaTime);
                }

                //Consume Recipe Input
                var demand = _builtPart.partPrefab.GetComponent<OseModuleRecipe>().Demand;
                var totalRatio = _builtPart.partPrefab.GetComponent<OseModuleRecipe>().TotalRatio;
                foreach (var res in demand)
                {
                    var resourcesUsed = RequestResource(res.ResourceName, (res.Ratio / totalRatio) * deltaTime * ProductivityFactor);
                    _massProcessed += resourcesUsed * res.Density;
                }
            }

            Progress = (float)(_massProcessed / _builtPart.partPrefab.mass * 100);
        }

        public double AmountAvailable(string resource)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            return resList.Sum(r => r.amount);
        }

        public double RequestResource(string resource, double amount)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            var demandLeft = amount;
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

        private void FinishManufacturing()
        {
            if (AddToContainer(_builtPart))
            {
                _builtPart = null;
                _massProcessed = 0;
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
            if (_showGui)
            {
                ContextMenuOnOpenWorkbench();
            }
            base.OnInactive();
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

            var demand = _builtPart.partPrefab.GetComponent<OseModuleRecipe>().Demand;
            var totalRatio = _builtPart.partPrefab.GetComponent<OseModuleRecipe>().TotalRatio;
            foreach (var res in demand)
            {
                if (this.AmountAvailable(res.ResourceName) < (res.Ratio / totalRatio) * deltaTime * ProductivityFactor)
                {
                    return "Not enough " + res.ResourceName;
                }
            }

            return "Ok";
        }

        private bool AddToContainer(AvailablePart availablePart)
        {
            var kisModuleContainers = vessel.FindPartModulesImplementing<ModuleKISInventory>();

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
                   "Workshop Build Menu",
                   GUILayout.ExpandWidth(true),
                   GUILayout.ExpandHeight(true),
                   GUILayout.MinWidth(64),
                   GUILayout.MinHeight(64));
        }

        private void DrawWindowContents(int windowId)
        {
            GUILayout.Space(15);

            DrawFilter();
            DrawAvailableItems();
            DrawQueuedItems();
            DrawAvailableInventories();

            if (GUI.Button(new Rect(_windowPos.width - 24, 4, 20, 20), "X"))
            {
                ContextMenuOnOpenWorkbench();
            }

            GUI.DragWindow();
        }

        private void DrawFilter()
        {
            GUILayout.BeginHorizontal();
            for (var i = 0; i < 5; i++)
            {
                GUILayout.Box("", GUILayout.Width(25), GUILayout.Height(25));
                var textureRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(textureRect, this.GetTexture("R&D_node_icon_advmetalworks"), ScaleMode.ScaleToFit);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawAvailableItems()
        {
            GUILayout.Label("- Available items -", GuiStyles.Heading());
            _scrollPosItems = GUILayout.BeginScrollView(_scrollPosItems, GuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(250f));
            foreach (var item in _items)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box("", GUILayout.Width(25), GUILayout.Height(25));
                var textureRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(textureRect, item.Icon.texture, ScaleMode.ScaleToFit);
                GUILayout.Label(" " + item.Part.title, GuiStyles.Center(), GUILayout.Width(295f));
                GUILayout.Label(" " + item.Part.partPrefab.mass, GuiStyles.Center(), GUILayout.Width(80f));
                if (GUILayout.Button("Queue", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    _queue.Add(item.Part);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawQueuedItems()
        {
            GUILayout.Label("- Queued items -", GuiStyles.Heading());
            _scrollPosQueue = GUILayout.BeginScrollView(_scrollPosQueue, GuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(150f));
            foreach (var availablePart in _queue)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + availablePart.title, GuiStyles.Center(), GUILayout.Width(400f));
                if (GUILayout.Button("Remove", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    _queue.Remove(availablePart);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawAvailableInventories()
        {
            GUILayout.Label("- Available Inventories -", GuiStyles.Heading());
            _scrollPosInventories = GUILayout.BeginScrollView(_scrollPosInventories, GuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(100f));
            foreach (var inventory in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISInventory>())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + inventory.maxVolume, GuiStyles.Center(), GUILayout.Width(400f));
                if (GUILayout.Button("Highlight", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    inventory.part.SetHighlight(true, false);
                }
                if (GUILayout.Button("Unhighlight", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    inventory.part.SetHighlight(false, false);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private Texture2D GetTexture(string iconName)
        {
            return GameDatabase.Instance.databaseTexture.Single(t => t.name == "Squad/PartList/SimpleIcons/" + iconName).texture;
        }
    }
}
