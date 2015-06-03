namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        private WorkshopItem[] _items;
        private WorkshopItem[] _filteredItems;
        private WorkshopItem _builtPart;
        private double _massProcessed;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;
        private readonly List<Demand> _upkeep;
        private readonly List<Limit> _limits;

        // GUI Properties
        private readonly int _windowId;
        private List<FilterBase> _filters = new List<FilterBase>();
        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;
        private bool _showGui;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string Upkeep = "";

        [KSPField]
        public string Limits = "";

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Workshop Status", guiActive = true)]
        public string Status = "Online";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Progress", guiUnits = "%", guiFormat = "F1")]
        [UI_ProgressBar(minValue = 0, maxValue = 100F)]
        public float Progress = 0;

        [KSPEvent(guiActive = true, guiName = "Open Workbench")]
        public void ContextMenuOnOpenWorkbench()
        {
            if (_showGui)
            {
                foreach (var item in _filteredItems)
                {
                    item.DisableIcon();
                }
                _showGui = false;
            }
            else
            {
                foreach (var item in _filteredItems)
                {
                    item.EnableIcon();
                }
                _showGui = true;
            }
        }

        public OseModuleWorkshop()
        {
            Debug.Log("[Workshop] - Constructed Module");
            _windowId = new System.Random().Next(65536);
            _clock = new Clock();
            _queue = new WorkshopQueue();
            _upkeep = new List<Demand>();
            _limits = new List<Limit>();
        }

        public override void OnStart(StartState state)
        {
            LoadAvailableParts();
            GameEvents.onVesselChange.Add(this.OnVesselChange);
            base.OnStart(state);
        }

        public override void OnLoad(ConfigNode node)
        {
            LoadModuleState(node);
            LoadUpkeep();
            LoadLimits();
            LoadFilters();
            base.OnLoad(node);
        }

        private void LoadFilters()
        {
            _filters = new List<FilterBase>
                       {
                           new FilterBase("Squad/PartList/SimpleIcons/R&D_node_icon_veryheavyrocketry"),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_commandmodules", PartCategories.Pods),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_fuelSystems-advanced", PartCategories.FuelTank),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_propulsionSystems", PartCategories.Engine),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_largecontrol", PartCategories.Control),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_generalconstruction", PartCategories.Structural),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_advaerodynamics", PartCategories.Aero),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_generic", PartCategories.Utility),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_advsciencetech", PartCategories.Science),
                           new FilterModule("Squad/PartList/SimpleIcons/R&D_node_icon_evatech", "ModuleKISItem")
                       };
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

        private void LoadLimits()
        {
            var techs = Limits.Split(',');
            for (var i = 0; i < techs.Length; i += 2)
            {
                _limits.Add(new Limit
                {
                    Technology = techs[i],
                    MaxVolume = float.Parse(techs[i + 1])
                });
            }
        }

        private void LoadAvailableParts()
        {
            var maxVolume = this.GetMaxVolume();
            _items = PartLoader.LoadedPartsList
                .Where(WorkshopUtils.HasRecipeModule)
                .Where(p => KIS_Shared.GetPartVolume(p.partPrefab) <= maxVolume)
                .Where(ResearchAndDevelopment.PartModelPurchased)
                .Select(p => new WorkshopItem(p)).ToArray();
            _filteredItems = _items;
        }

        private float GetMaxVolume()
        {
            var maxVolume = part.vessel.FindPartModulesImplementing<ModuleKISInventory>().Max(i => i.maxVolume);
            var unlockedLimits = _limits.Where(l => HasTech(l.Technology)).ToArray();
            if (unlockedLimits.Length > 0)
            {
                var largestUnlockedLimit = unlockedLimits.Max(l => l.MaxVolume);
                maxVolume = Math.Min(maxVolume, largestUnlockedLimit);
            }
            Debug.Log("[Workshop] - Max volume is: " + maxVolume + "liters");
            return maxVolume;
        }

        private static bool HasTech(string techid)
        {
            try
            {
                var persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                var config = ConfigNode.Load(persistentfile);
                var gameconf = config.GetNode("GAME");
                var scenarios = gameconf.GetNodes("SCENARIO");
                return scenarios
                    .Where(scenario => scenario.GetValue("name") == "ResearchAndDevelopment")
                    .SelectMany(scenario => scenario.GetNodes("Tech"))
                    .Any(technode => technode.GetValue("id") == techid);
            }
            catch (Exception)
            {
                return false;
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
                if (Progress >= 100)
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
                _builtPart = new WorkshopItem(nextQueuedPart.Part);
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
                    RequestResource(res.ResourceName, res.Ratio * deltaTime);
                }

                //Consume Recipe Input
                var demand = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().Demand;
                var totalRatio = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().TotalRatio;
                foreach (var res in demand)
                {
                    var resourcesUsed = RequestResource(res.ResourceName, (res.Ratio / totalRatio) * deltaTime * ProductivityFactor);
                    _massProcessed += resourcesUsed * res.Density;
                }
            }

            Progress = (float)(_massProcessed / _builtPart.Part.partPrefab.mass * 100);
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
                _builtPart.DisableIcon();
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

            var demand = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().Demand;
            var totalRatio = _builtPart.Part.partPrefab.GetComponent<OseModuleRecipe>().TotalRatio;
            foreach (var res in demand)
            {
                if (this.AmountAvailable(res.ResourceName) < (res.Ratio / totalRatio) * deltaTime * ProductivityFactor)
                {
                    return "Not enough " + res.ResourceName;
                }
            }

            return "Ok";
        }

        private bool AddToContainer(WorkshopItem item)
        {
            var kisModuleContainers = vessel.FindPartModulesImplementing<ModuleKISInventory>();

            if (kisModuleContainers == null || kisModuleContainers.Count == 0)
            {
                throw new Exception("No KIS Container found");
            }

            foreach (var container in kisModuleContainers)
            {
                if (container.GetContentVolume() + KIS_Shared.GetPartVolume(item.Part.partPrefab) < container.maxVolume)
                {
                    var kisItem = container.AddItem(item.Part.partPrefab);
                    foreach (var resourceInfo in kisItem.GetResources())
                    {
                        kisItem.SetResource(resourceInfo.resourceName, 0);
                    }
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
            this.DrawFilters();
            
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

        private void DrawFilters()
        {
            GUILayout.Box("", GUILayout.Width(805), GUILayout.Height(42));
            var boxRect = GUILayoutUtility.GetLastRect();
            for (var index = 0; index < this._filters.Count; index++)
            {
                var filter = this._filters[index];
                if (WorkshopGui.FilterButton(filter, new Rect(boxRect.xMin + 5 + (37 * index), boxRect.yMin + 5, 32, 32)))
                {
                    foreach (var filteredItem in _filteredItems)
                    {
                        filteredItem.DisableIcon();
                    }
                    this._filteredItems = filter.Filter(this._items);
                    foreach (var filteredItem in _filteredItems)
                    {
                        filteredItem.EnableIcon();
                    }
                }
            }
        }

        private void DrawAvailableItems()
        {
            GUILayout.BeginVertical();
            _scrollPosItems = GUILayout.BeginScrollView(_scrollPosItems, WorkshopStyles.Databox(), GUILayout.Width(400f), GUILayout.Height(250f));
            foreach (var item in this._filteredItems)
            {
                GUILayout.BeginHorizontal();
                WorkshopGui.ItemThumbnail(item.Icon);
                WorkshopGui.ItemDescription(item.Part);
                if (GUILayout.Button("Queue", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                {
                    var queuedItem = new WorkshopItem(item.Part);
                    queuedItem.EnableIcon();
                    this._queue.Add(queuedItem);
                }
                GUILayout.EndHorizontal();
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
            WorkshopGui.ProgressBar(Progress);
            GUILayout.EndHorizontal();
        }

        //private void DrawAvailableInventories()
        //{
        //    GUILayout.Label("- Available Inventories -", GuiStyles.Heading());
        //    _scrollPosInventories = GUILayout.BeginScrollView(_scrollPosInventories, GuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(100f));
        //    foreach (var inventory in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISInventory>())
        //    {
        //        GUILayout.BeginHorizontal();
        //        GUILayout.Label(" " + inventory.maxVolume, GuiStyles.Center(), GUILayout.Width(400f));
        //        if (GUILayout.Button("Highlight", GuiStyles.Button(), GUILayout.Width(80f)))
        //        {
        //            inventory.part.SetHighlight(true, false);
        //        }
        //        if (GUILayout.Button("Unhighlight", GuiStyles.Button(), GUILayout.Width(80f)))
        //        {
        //            inventory.part.SetHighlight(false, false);
        //        }
        //        GUILayout.EndHorizontal();
        //    }
        //    GUILayout.EndScrollView();
        //}
    }
}
