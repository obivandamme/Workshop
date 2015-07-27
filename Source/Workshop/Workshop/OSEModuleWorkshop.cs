namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using UnityEngine;

    public class OseModuleWorkshop : PartModule
    {
        private WorkshopItem[] _availableItems;
        private WorkshopItem[] _filteredItems;

        private WorkshopItem _processedItem;
        private WorkshopItem _canceledItem;
        private WorkshopItem _addedItem;

        private double _massProcessed;
        private float _progress;
        private float _maxVolume;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;

        // GUI Properties
        private List<FilterBase> _filters;
        private int _activeFilterId;
        private int _selectedFilterId;

        private int _activePage;
        private int _selectedPage;
        private int _maxPage;

        private Rect _windowPos = new Rect(50, 50, 640, 680);
        private Vector2 _scrollPosQueue = Vector2.zero;
        private bool _showGui;

        [KSPField]
        public float ConversionRate = 1f;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string UpkeepResource = "ElectricCharge";

        [KSPField]
        public string InputResource = "MaterialKits";

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField]
        public float MaxPartVolume = 300f;

        [KSPField(guiName = "Workshop Status", guiActive = true)]
        public string Status = "Online";

        [KSPEvent(guiActive = true, guiName = "Open Workbench")]
        public void ContextMenuOnOpenWorkbench()
        {
            if (_showGui)
            {
                foreach (var item in _filteredItems)
                {
                    item.DisableIcon();
                }
                foreach (var item in _queue)
                {
                    item.DisableIcon();
                }
                if (this._processedItem != null)
                {
                    this._processedItem.DisableIcon();
                }
                _showGui = false;
            }
            else
            {
                _showGui = true;
            }
        }

        public OseModuleWorkshop()
        {
            _clock = new Clock();
            _queue = new WorkshopQueue();
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
            base.OnLoad(node);
            if (HighLogic.LoadedSceneIsFlight)
            {
                LoadMaxVolume();
                LoadAvailableParts();
                LoadModuleState(node);
                LoadFilters();
            }
        }

        private void LoadFilters()
        {
            _filters = new List<FilterBase>
                       {
                           new FilterBase("Squad/PartList/SimpleIcons/R&D_node_icon_veryheavyrocketry", "All"),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_commandmodules", "Pods", PartCategories.Pods),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_fuelSystems-advanced", "Tanks", PartCategories.FuelTank),
                           new FilterCategory("Squad/PartList/SimpleIcons/RDicon_propulsionSystems", "Engine", PartCategories.Engine),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_largecontrol", "Control", PartCategories.Control),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_generalconstruction", "Structural", PartCategories.Structural),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_advaerodynamics", "Aero", PartCategories.Aero),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_generic", "Util", PartCategories.Utility),
                           new FilterCategory("Squad/PartList/SimpleIcons/R&D_node_icon_advsciencetech", "Science", PartCategories.Science),
                           new FilterModule("Squad/PartList/SimpleIcons/R&D_node_icon_evatech", "EVA", "ModuleKISItem")
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
                        this._processedItem = new WorkshopItem(availablePart);
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

        private void LoadAvailableParts()
        {
            var items = new List<WorkshopItem>();
            foreach (var loadedPart in PartLoader.LoadedPartsList)
            {
                try
                {
                    if (ResearchAndDevelopment.PartModelPurchased(loadedPart) && KIS_Shared.GetPartVolume(loadedPart.partPrefab) <= _maxVolume)
                    {
                        items.Add(new WorkshopItem(loadedPart));
                    }
                }
                catch (Exception)
                {
                    Debug.Log("[OSE] - Part " + loadedPart.name + " could not be added to available parts list");
                }
            }
            _availableItems = items.OrderBy(i => i.Part.title).ToArray();
            _filteredItems = items.OrderBy(i => i.Part.title).Take(30).ToArray();
            _maxPage = _availableItems.Count() / 30;
        }

        private void LoadMaxVolume()
        {
            _maxVolume = this.MaxPartVolume;
            try
            {
                var inventories = vessel.FindPartModulesImplementing<ModuleKISInventory>();
                if (inventories.Count == 0)
                {
                    Debug.Log("[OSE] - No Inventories found on this vessel!");
                    
                }
                else
                {

                    Debug.Log("[OSE] - " + inventories.Count + " inventories found on this vessel!");
                    var maxInventoyVolume = inventories.Max(i => i.maxVolume);
                    _maxVolume = Math.Min(maxInventoyVolume, this.MaxPartVolume);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - Error while determing maximum volume of available inventories - using configured value!");
                Debug.LogError("[OSE] - " + ex.Message);
                Debug.LogError("[OSE] - " + ex.StackTrace);
            }
            Debug.Log("[OSE] - Max volume is: " + _maxVolume + "liters");
        }

        public override void OnSave(ConfigNode node)
        {
            if (this._processedItem != null)
            {
                var builtPartNode = node.AddNode("BUILTPART");
                builtPartNode.AddValue("Name", this._processedItem.Part.name);
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
                this.ApplyFilter();
                this.ApplyPaging();
                this.RemoveCanceledItemFromQueue();
                this.AddNewItemToQueue();
                this.ProcessItem(deltaTime);
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnUpdate - " + ex.Message);
            }
            base.OnUpdate();
        }

        private void ProcessItem(double deltaTime)
        {
            if (_progress >= 100)
            {
                FinishManufacturing();
            }
            else if (this._processedItem != null)
            {
                ExecuteManufacturing(deltaTime);
            }
            else
            {
                StartManufacturing();
            }
        }

        private void RemoveCanceledItemFromQueue()
        {
            if (this._canceledItem == null)
            {
                return;
            }

            this._canceledItem.DisableIcon();
            _queue.Remove(this._canceledItem);
            this._canceledItem = null;
        }

        private void AddNewItemToQueue()
        {
            if (this._addedItem == null)
            {
                return;
            }

            _queue.Add(this._addedItem);
            this._addedItem = null;
        }

        private void ApplyFilter()
        {
            if (_activeFilterId == _selectedFilterId)
            {
                return;
            }
            
            foreach (var item in _filteredItems)
            {
                item.DisableIcon();
            }

            var selectedFilter = _filters[_selectedFilterId];
            _filteredItems = selectedFilter.Filter(_availableItems, _activePage * 30);
            _activeFilterId = _selectedFilterId;
        }

        private void ApplyPaging()
        {
            if (_activePage == _selectedPage)
            {
                return;
            }

            foreach (var item in _filteredItems)
            {
                item.DisableIcon();
            }

            var selectedFilter = _filters[_activeFilterId];
            _filteredItems = selectedFilter.Filter(_availableItems, _selectedPage * 30);
            _activePage = _selectedPage;
        }

        private void StartManufacturing()
        {
            var nextQueuedPart = _queue.Pop();
            if (nextQueuedPart != null)
            {
                this._processedItem = nextQueuedPart;
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
                Status = "Building " + this._processedItem.Part.title;

                RequestResource(UpkeepResource, deltaTime);
                _massProcessed += ConsumeInputResource(deltaTime);
            }

            _progress = (float)(_massProcessed / (this._processedItem.Part.partPrefab.mass * this.ConversionRate) * 100);
        }

        private double ConsumeInputResource(double deltaTime)
        {
            var density = PartResourceLibrary.Instance.GetDefinition(this.InputResource).density;
            var resourcesUsed = this.RequestResource(this.InputResource, deltaTime * this.ProductivityFactor);
            return resourcesUsed * density;
        }

        private double AmountAvailable(string resource)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            var resList = new List<PartResource>();
            part.GetConnectedResources(res.id, res.resourceFlowMode, resList);
            return resList.Sum(r => r.amount);
        }

        private double RequestResource(string resource, double amount)
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
            var destinationInventory = AddToContainer(this._processedItem);
            if (destinationInventory != null)
            {
                ScreenMessages.PostScreenMessage("3D Printing of " + this._processedItem.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
                this._processedItem.DisableIcon();
                this._processedItem = null;
                _massProcessed = 0;
                _progress = 0;
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

        private void OnVesselChange(Vessel v)
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

            if (this.AmountAvailable(this.UpkeepResource) < deltaTime)
            {
                return "Not enough " + this.UpkeepResource;
            }

            if (this.AmountAvailable(this.InputResource) < deltaTime * this.ProductivityFactor)
            {
                return "Not enough " + this.InputResource;
            }


            return "Ok";
        }

        private ModuleKISInventory AddToContainer(WorkshopItem item)
        {
            var inventories = vessel.FindPartModulesImplementing<ModuleKISInventory>();

            if (inventories == null || inventories.Count == 0)
            {
                throw new Exception("No KIS Inventory found!");
            }

            var freeInventories = inventories.Where(i => WorkshopUtils.HasFreeSpace(i, item) && WorkshopUtils.HasFreeSlot(i) && WorkshopUtils.IsOccupied(i)).ToArray();

            if (freeInventories.Any())
            {
                foreach (var inventory in freeInventories)
                {
                    var kisItem = inventory.AddItem(item.Part.partPrefab);
                    if (kisItem == null)
                    {
                        throw new Exception("Error adding item " + item.Part.name + " to inventory");
                    }
                    foreach (var resourceInfo in kisItem.GetResources())
                    {
                        kisItem.SetResource(resourceInfo.resourceName, 0);
                    }
                    return inventory;
                }
            }
            return null;
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

            _windowPos = GUI.Window(
                   this.GetInstanceID(),
                   _windowPos,
                   this.DrawWindowContents,
                   "Workbench (" + _maxVolume + " litres)");
        }

        private void DrawWindowContents(int windowId)
        {
            WorkshopItem mouseOverItem = null;

            // styles 
            var statsStyle = new GUIStyle(GUI.skin.box);
            statsStyle.fontSize = 11;
            statsStyle.alignment = TextAnchor.UpperLeft;
            statsStyle.padding.left = statsStyle.padding.top = 5;

            var tooltipDescriptionStyle = new GUIStyle(GUI.skin.box);
            tooltipDescriptionStyle.fontSize = 11;
            tooltipDescriptionStyle.alignment = TextAnchor.UpperCenter;
            tooltipDescriptionStyle.padding.top = 5;

            // Filters
            var labels = new[] { "All", "Pods", "Tanks", "Engines", "Control", "Struct", "Aero", "Util", "Science", "EVA" };
            _selectedFilterId = GUI.Toolbar(new Rect(15, 35, 615, 30), _selectedFilterId, labels);

            // Available Items
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var left = 15 + x * 55;
                    var top = 70 + y * 55;
                    var itemIndex = y * 3 + x;
                    if (_filteredItems.Length > itemIndex)
                    {
                        var item = _filteredItems[itemIndex];
                        if (item.Icon == null)
                        {
                            item.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Icon.texture))
                        {
                            _addedItem = new WorkshopItem(item.Part);
                        }
                        if (Event.current.type == EventType.Repaint && new Rect(left, top, 50, 50).Contains(Event.current.mousePosition))
                        {
                            mouseOverItem = item;
                        }
                    }
                }
            }
            if (GUI.Button(new Rect(15, 645, 75, 25), "Prev"))
            {
                if (_activePage > 0)
                {
                    _selectedPage = _activePage - 1;
                }
            }
            if (GUI.Button(new Rect(100, 645, 75, 25), "Next"))
            {
                if (_activePage < _maxPage)
                {
                    _selectedPage = _activePage + 1;
                }
            }

            // Tooltip
            GUI.Box(new Rect(190, 70, 440, 270), "");
            if (mouseOverItem != null)
            {
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), mouseOverItem.GetKisStats(), statsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), mouseOverItem.GetOseStats(InputResource, ConversionRate, ProductivityFactor), statsStyle);
                GUI.Box(new Rect(200, 190, 420, 140), mouseOverItem.GetDescription(), tooltipDescriptionStyle);
            }
            

            // Queued Items
            GUI.Box(new Rect(190, 345, 440, 270), "Queue");

            // Currently build item
            if (_processedItem != null)
            {
                if (_processedItem.Icon == null)
                {
                    _processedItem.EnableIcon(64);
                }
                GUI.Box(new Rect(190, 620, 50, 50), _processedItem.Icon.texture);
            }
            else
            {
                GUI.Box(new Rect(190, 620, 50, 50), "");
            }

            // Progressbar
            GUI.Box(new Rect(250, 620, 380, 50), "");
            if (_progress >= 1)
            {
                var color = GUI.color;
                GUI.color = new Color(0, 1, 0, 1);
                GUI.Box(new Rect(250, 620, 380 * _progress / 100, 50), "");
                GUI.color = color;
            }
            GUI.Label(new Rect(250, 620, 380, 50), " " + _progress.ToString("0.0") + " / 100");
            
            if (GUI.Button(new Rect(_windowPos.width - 25, 5, 20, 20), "X"))
            {
                ContextMenuOnOpenWorkbench();
            }

            GUI.DragWindow();
        }
        
        private void DrawQueuedItems()
        {
            GUILayout.BeginVertical();
            _scrollPosQueue = GUILayout.BeginScrollView(_scrollPosQueue, WorkshopStyles.Databox(), GUILayout.Width(400f), GUILayout.Height(250f));
            foreach (var item in this._queue)
            {
                GUILayout.BeginHorizontal();
                if (item.Icon == null)
                {
                    item.EnableIcon(128);
                }
                WorkshopGui.ItemThumbnail(item.Icon);
                WorkshopGui.ItemDescription(item.Part, this.InputResource, this.ConversionRate);
                if (GUILayout.Button("Remove", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                {
                    this._canceledItem = item;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
