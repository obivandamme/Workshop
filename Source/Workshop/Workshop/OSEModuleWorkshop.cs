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
        private WorkshopItem _builtPart;
        private WorkshopItem _canceledPart;
        private WorkshopItem _addedPart;

        private double _massProcessed;
        private float _progress;

        private readonly Clock _clock;
        private readonly WorkshopQueue _queue;

        // GUI Properties
        private List<FilterBase> _filters;
        private FilterBase _selectedFilter;
        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
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
        public int MaxPartVolume = 300;

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
                if (_builtPart != null)
                {
                    _builtPart.DisableIcon();
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
                LoadAvailableParts();
                LoadModuleState(node);
                LoadFilters();
            }
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
            var maxVolume = this.GetMaxVolume();
            foreach (var loadedPart in PartLoader.LoadedPartsList)
            {
                try
                {
                    if (ResearchAndDevelopment.PartModelPurchased(loadedPart) && KIS_Shared.GetPartVolume(loadedPart.partPrefab) <= maxVolume)
                    {
                        items.Add(new WorkshopItem(loadedPart));
                    }
                }
                catch (Exception)
                {
                    Debug.Log("[OSE] - Part " + loadedPart.name + " could not be added to available parts list");
                }
            }
            this._availableItems = items.OrderBy(i => i.Part.title).ToArray();
            _filteredItems = items.OrderBy(i => i.Part.title).ToArray();
        }

        private float GetMaxVolume()
        {
            try
            {
                var maxInventoyVolume = part.vessel.FindPartModulesImplementing<ModuleKISInventory>().Max(i => i.maxVolume);
                var maxVolume = Math.Min(maxInventoyVolume, this.MaxPartVolume);
                Debug.Log("[OSE] - Max volume is: " + maxVolume + "liters");
                return maxVolume;
            }
            catch (Exception)
            {
                Debug.LogError("[OSE] - Error while determing maximum volume");
                return 0;
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
                this.ApplyFilter();
                this.RemoveCanceledPartFromQueue();
                this.AddNewPartToQueue();
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
            else if (_builtPart != null)
            {
                ExecuteManufacturing(deltaTime);
            }
            else
            {
                StartManufacturing();
            }
        }

        private void RemoveCanceledPartFromQueue()
        {
            if (_canceledPart == null)
            {
                return;
            }

            _canceledPart.DisableIcon();
            _queue.Remove(_canceledPart);
            _canceledPart = null;
        }

        private void AddNewPartToQueue()
        {
            if (_addedPart == null)
            {
                return;
            }

            _queue.Add(_addedPart);
            _addedPart = null;
        }

        private void ApplyFilter()
        {
            if (_selectedFilter == null)
            {
                return;
            }

            foreach (var item in _filteredItems)
            {
                item.DisableIcon();
            }
            _filteredItems = _selectedFilter.Filter(_availableItems);
            _selectedFilter = null;
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
                Status = "Building " + _builtPart.Part.title;

                RequestResource(UpkeepResource, deltaTime);
                _massProcessed += ConsumeInputResource(deltaTime);
            }

            _progress = (float)(_massProcessed / (_builtPart.Part.partPrefab.mass * this.ConversionRate) * 100);
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
            var destinationInventory = AddToContainer(_builtPart);
            if (destinationInventory != null)
            {
                ScreenMessages.PostScreenMessage("3D Printing of " + _builtPart.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
                _builtPart.DisableIcon();
                _builtPart = null;
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

            _windowPos = GUILayout.Window(
                   this.GetInstanceID(),
                   _windowPos,
                   this.DrawWindowContents,
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
                    _selectedFilter = filter;
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
                if (item.Icon == null)
                {
                    item.EnableIcon();
                }
                WorkshopGui.ItemThumbnail(item.Icon);
                WorkshopGui.ItemDescription(item.Part, this.InputResource);
                if (GUILayout.Button("Queue", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                {
                    _addedPart = new WorkshopItem(item.Part);
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
                if (item.Icon == null)
                {
                    item.EnableIcon();
                }
                WorkshopGui.ItemThumbnail(item.Icon);
                WorkshopGui.ItemDescription(item.Part, this.InputResource);
                if (GUILayout.Button("Remove", WorkshopStyles.Button(), GUILayout.Width(60f), GUILayout.Height(40f)))
                {
                    this._canceledPart = item;
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
                if (_builtPart.Icon == null)
                {
                    _builtPart.EnableIcon();
                }
                WorkshopGui.ItemThumbnail(_builtPart.Icon);
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(50));
            }
            WorkshopGui.ProgressBar(_progress);
            GUILayout.EndHorizontal();
        }
    }
}
