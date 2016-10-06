namespace Workshop
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Collections.Generic;

    using KIS;

    using UnityEngine;

    using Recipes;

    public class OseModuleWorkshop : PartModule
    {
        private WorkshopItem[] _availableItems;
        private FilterResult _filteredItems;

        private Blueprint _processedBlueprint;
        private WorkshopItem _processedItem;

        private float _maxVolume;

        private readonly WorkshopQueue _queue;

        // Animations
        private AnimationState _heatAnimation;
        private AnimationState _workAnimation;

        // GUI Properties
        private FilterBase[] _filters;
        private Texture[] _filterTextures;

        private int _activeFilterId;
        private int _selectedFilterId;

        private int _activePage;
        private int _selectedPage;

        private Rect _windowPos = new Rect(50, 50, 640, 680);
        private bool _showGui;

        private bool _confirmDelete;

        [KSPField(isPersistant = true)]
        public bool manufacturingPaused;

        [KSPField(isPersistant = true)]
        public float progress;

        [KSPField]
        public bool Animate = false;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string UpkeepResource = "ElectricCharge";

        [KSPField]
        public int MinimumCrew = 2;
        
        [KSPField(guiName = "Workshop Status", guiActive = true)]
        public string Status = "Online";

        [KSPEvent(guiName = "Open Workbench", guiActive = true)]
        public void ContextMenuOpenWorkbench()
        {
            if (_showGui)
            {
                foreach (var item in _filteredItems.Items)
                {
                    item.DisableIcon();
                }
                foreach (var item in _queue)
                {
                    item.DisableIcon();
                }
                if (_processedItem != null)
                {
                    _processedItem.DisableIcon();
                }
                _showGui = false;
            }
            else
            {
                LoadAvailableParts();
                _showGui = true;
            }
        }

        public OseModuleWorkshop()
        {
            _queue = new WorkshopQueue();
        }

        public override void OnStart(StartState state)
        {
            if (WorkshopSettings.IsKISAvailable)
            {
                Debug.Log("[OSE] - KIS is available - Initialize Workshop");
                SetupAnimations();
                LoadMaxVolume();
                LoadFilters();
                GameEvents.onVesselChange.Add(OnVesselChange);
            }
            else
            {
                Fields["Status"].guiActive = false;
                Events["ContextMenuOnOpenWorkbench"].guiActive = false;
            }
            base.OnStart(state);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                LoadModuleState(node);
            }
            base.OnLoad(node);
        }

        private void SetupAnimations()
        {
            if (Animate)
            {
                foreach (var animator in part.FindModelAnimators("workshop_emissive"))
                {
                    _heatAnimation = animator["workshop_emissive"];
                    if (_heatAnimation != null)
                    {
                        _heatAnimation.speed = 0;
                        _heatAnimation.enabled = true;
                        _heatAnimation.wrapMode = WrapMode.ClampForever;
                        animator.Blend("workshop_emissive");
                        break;
                    }
                    Debug.LogError("[OSE] - Unable to load workshop_emissive animation");
                }
                foreach (var animator in part.FindModelAnimators("work"))
                {
                    _workAnimation = animator["work"];
                    if (_workAnimation != null)
                    {
                        _workAnimation.speed = 0;
                        _workAnimation.enabled = true;
                        _workAnimation.wrapMode = WrapMode.ClampForever;
                        animator.Blend("work");
                        break;
                    }
                    Debug.LogError("[OSE] - Unable to load work animation");
                }
            }
        }

        private void LoadFilters()
        {
            var filters = new List<FilterBase>();
            filters.Add(new FilterModule("ModuleKISItem"));
            filters.Add(new FilterCategory(PartCategories.none));
            
            var filterTextures = new List<Texture>();
            filterTextures.Add(WorkshopUtils.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_evatech"));
            filterTextures.Add(WorkshopUtils.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_robotics"));
            
            var categoryFilterAddOns = vessel.FindPartModulesImplementing<OseModuleCategoryAddon>();
            if (categoryFilterAddOns != null)
            {
                foreach (var addon in categoryFilterAddOns.Distinct(new OseModuleCategoryAddonEqualityComparer()).ToArray())
                {
                    Debug.Log("[OSE] - Found addon for category: " + addon.Category);
                    filters.Add(new FilterCategory(addon.Category));
                    filterTextures.Add(WorkshopUtils.LoadTexture(addon.IconPath));
                }
                
            }

            _filters = filters.ToArray();
            _filterTextures = filterTextures.ToArray();
        }

        private void LoadModuleState(ConfigNode node)
        {
            foreach (ConfigNode cn in node.nodes)
            {
                if (cn.name == "ProcessedItem")
                {
                    _processedItem = new WorkshopItem();
                    _processedItem.Load(cn);
                }
                if (cn.name == "ProcessedBlueprint")
                {
                    _processedBlueprint = new Blueprint();
                    _processedBlueprint.Load(cn);
                }
                if (cn.name == "Queue")
                {
                    _queue.Load(cn);
                }
            }
        }

        private void LoadAvailableParts()
        {
            Debug.Log("[OSE] - " + PartLoader.LoadedPartsList.Count + " loaded parts");
            Debug.Log("[OSE] - " + PartLoader.LoadedPartsList.Count(WorkshopUtils.PartResearched) + " unlocked parts");

            var items = new List<WorkshopItem>();
            foreach (var loadedPart in PartLoader.LoadedPartsList.Where(p => p.name != "flag" && p.name != "kerbalEVA" && p.name != "kerbalEVAfemale"))
            {
                try
                {
                    if (IsValid(loadedPart))
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
            _filteredItems = _filters[_activeFilterId].Filter(_availableItems, 0);
        }

        private bool IsValid(AvailablePart loadedPart)
        {
            return WorkshopUtils.PartResearched(loadedPart) && WorkshopUtils.GetPackedPartVolume(loadedPart) <= _maxVolume && WorkshopBlacklistItemsDatabase.Blacklist.Contains(loadedPart.name) == false;
        }

        private void LoadMaxVolume()
        {
            try
            {
                var inventories = KISWrapper.GetInventories(vessel);
                if (inventories.Count == 0)
                {
                    Debug.Log("[OSE] - No Inventories found on this vessel!");

                }
                else
                {

                    Debug.Log("[OSE] - " + inventories.Count + " inventories found on this vessel!");
                    _maxVolume = inventories.Max(i => i.maxVolume);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - Error while determing maximum volume of available inventories!");
                Debug.LogError("[OSE] - " + ex.Message);
                Debug.LogError("[OSE] - " + ex.StackTrace);
            }
            Debug.Log("[OSE] - Max volume is: " + _maxVolume + " liters");
        }

        public override void OnSave(ConfigNode node)
        {
            if (_processedItem != null)
            {
                var itemNode = node.AddNode("ProcessedItem");
                _processedItem.Save(itemNode);

                var blueprintNode = node.AddNode("ProcessedBlueprint");
                _processedBlueprint.Save(blueprintNode);
            }

            var queueNode = node.AddNode("Queue");
            _queue.Save(queueNode);

            base.OnSave(node);
        }

        public override void OnUpdate()
        {
            try
            {
                ApplyFilter();
                ApplyPaging();
                ProcessItem();
            }
            catch (Exception ex)
            {
                Debug.LogError("[OSE] - OseModuleWorkshop_OnUpdate");
                Debug.LogException(ex);
            }
            base.OnUpdate();
        }

        private void ProcessItem()
        {
            if (manufacturingPaused)
                return;

            if (progress >= 100)
            {
                FinishManufacturing();
            }
            else if (_processedItem != null)
            {
                ExecuteManufacturing();
            }
            else
            {
                StartManufacturing();
            }
        }

        private void ApplyFilter()
        {
            if (_activeFilterId != _selectedFilterId)
            {
                foreach (var item in _filteredItems.Items)
                {
                    item.DisableIcon();
                }

                var selectedFilter = _filters[_selectedFilterId];
                _activePage = 0;
                _selectedPage = 0;
                _filteredItems = selectedFilter.Filter(_availableItems, _activePage * 30);
                _activeFilterId = _selectedFilterId;
            }
        }

        private void ApplyPaging()
        {
            if (_activePage != _selectedPage)
            {
                foreach (var item in _filteredItems.Items)
                {
                    item.DisableIcon();
                }

                var selectedFilter = _filters[_activeFilterId];
                _filteredItems = selectedFilter.Filter(_availableItems, _selectedPage * 30);
                _activePage = _selectedPage;
            }
        }

        private void StartManufacturing()
        {
            var nextQueuedPart = _queue.Pop();
            if (nextQueuedPart != null)
            {
                _processedItem = nextQueuedPart;
                _processedBlueprint = WorkshopRecipeDatabase.ProcessPart(nextQueuedPart.Part);
                
                if (Animate && _heatAnimation != null && _workAnimation != null)
                {
                    StartCoroutine(StartAnimations());
                }
            }
        }

        private IEnumerator StartAnimations()
        {
            _heatAnimation.enabled = true;
            _heatAnimation.normalizedSpeed = 0.5f;
            while (_heatAnimation.normalizedTime < 1)
            {
                yield return null;
            }
            _heatAnimation.enabled = false;

            _workAnimation.enabled = true;
            _workAnimation.wrapMode = WrapMode.Loop;
            _workAnimation.normalizedSpeed = 0.5f;
        }

        private IEnumerator StopAnimations()
        {
            _heatAnimation.enabled = true;
            _heatAnimation.normalizedTime = 1;
            _heatAnimation.normalizedSpeed = -0.5f;

            _workAnimation.enabled = true;
            _workAnimation.wrapMode = WrapMode.Loop;
            _workAnimation.normalizedSpeed = 0.5f;

            while (_workAnimation.normalizedTime < 1)
            {
                yield return null;
            }
            _workAnimation.enabled = false;


            while (_heatAnimation.normalizedTime > 0)
            {
                yield return null;
            }
            _heatAnimation.enabled = false;
        }

        private void ExecuteManufacturing()
        {
            var resourceToConsume = _processedBlueprint.First(r => r.Processed < r.Units);
            var unitsToConsume = Math.Min(resourceToConsume.Units - resourceToConsume.Processed, TimeWarp.deltaTime * ProductivityFactor);

            if (part.protoModuleCrew.Count < MinimumCrew)
            {
                Status = "Not enough Crew to operate";
            }

            else if (AmountAvailable(UpkeepResource) < TimeWarp.deltaTime)
            {
                Status = "Not enough " + UpkeepResource;
            }

            else if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && Funding.Instance.Funds < _processedBlueprint.Funds)
            {
                Status = "Not enough funds to process";
            }

            else if (AmountAvailable(resourceToConsume.Name) < unitsToConsume)
            {
                Status = "Not enough " + resourceToConsume.Name;
            }
            else
            {
                Status = "Printing " + _processedItem.Part.title;
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && _processedBlueprint.Funds > 0)
                {
                    Funding.Instance.AddFunds(-_processedBlueprint.Funds, TransactionReasons.Vessels);
                    _processedBlueprint.Funds = 0;
                }
                RequestResource(UpkeepResource, TimeWarp.deltaTime);
                resourceToConsume.Processed += RequestResource(resourceToConsume.Name, unitsToConsume);
                progress = (float)(_processedBlueprint.GetProgress() * 100);
            }
        }

        private double AmountAvailable(string resource)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            double amount, maxAmount;
            part.GetConnectedResourceTotals(res.id, out amount, out maxAmount);
            return amount;
        }

        private float RequestResource(string resource, double amount)
        {
            var res = PartResourceLibrary.Instance.GetDefinition(resource);
            return (float)this.part.RequestResource(res.id, amount);
        }

        private void FinishManufacturing()
        {
            var destinationInventory = AddToContainer(_processedItem);
            if (destinationInventory != null)
            {
                ScreenMessages.PostScreenMessage("3D Printing of " + _processedItem.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
                _processedItem.DisableIcon();
                _processedItem = null;
                _processedBlueprint = null;
                progress = 0;
                Status = "Online";

                if (Animate && _heatAnimation != null && _workAnimation != null)
                {
                    StartCoroutine(StopAnimations());
                }
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
                ContextMenuOpenWorkbench();
            }
            base.OnInactive();
        }

        private void OnVesselChange(Vessel v)
        {
            if (_showGui)
            {
                ContextMenuOpenWorkbench();
            }
        }

        private ModuleKISInventory AddToContainer(WorkshopItem item)
        {
            var inventories = KISWrapper.GetInventories(vessel);

            if (inventories == null || inventories.Count == 0)
            {
                throw new Exception("No KIS Inventory found!");
            }

            var freeInventories = inventories
                .Where(i => WorkshopUtils.HasFreeSpace(i, item))
                .Where(WorkshopUtils.HasFreeSlot)
                .Where(WorkshopUtils.IsOccupied)
                .ToArray();

            if (freeInventories.Any())
            {
                // first pass with favored inventories
                var favoredInventories = freeInventories
                    .Where(i => i.part.GetComponent<OseModuleInventoryPreference>() != null)
                    .Where(i => i.part.GetComponent<OseModuleInventoryPreference>().IsFavored).ToArray();

                foreach (var inventory in favoredInventories)
                {
                    var kisItem = inventory.AddItem(item.Part.partPrefab);
                    if (kisItem == null)
                    {
                        throw new Exception("Error adding item " + item.Part.name + " to inventory");
                    }
                    foreach (var resourceInfo in kisItem.GetResources())
                    {
                        if (WorkshopRecipeDatabase.HasResourceRecipe(resourceInfo.resourceName))
                        {
                            kisItem.SetResource(resourceInfo.resourceName, (int)resourceInfo.maxAmount);
                        }
                        else
                        {
                            kisItem.SetResource(resourceInfo.resourceName, 0);
                        }
                    }
                    return inventory;
                }

                // second pass with the rest
                foreach (var inventory in freeInventories)
                {
                    var kisItem = inventory.AddItem(item.Part.partPrefab);
                    if (kisItem == null)
                    {
                        throw new Exception("Error adding item " + item.Part.name + " to inventory");
                    }
                    foreach (var resourceInfo in kisItem.GetResources())
                    {
                        if (WorkshopRecipeDatabase.HasResourceRecipe(resourceInfo.resourceName))
                        {
                            kisItem.SetResource(resourceInfo.resourceName, (int)resourceInfo.maxAmount);
                        }
                        else
                        {
                            kisItem.SetResource(resourceInfo.resourceName, 0);
                        }
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

            _windowPos = GUI.Window(GetInstanceID(), _windowPos, DrawWindowContents, "Workbench (" + _maxVolume + " litres)");
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

            var queueSkin = new GUIStyle(GUI.skin.box);
            queueSkin.alignment = TextAnchor.UpperCenter;
            queueSkin.padding.top = 5;

            // Filters
            _selectedFilterId = GUI.Toolbar(new Rect(15, 35, 615, 30), _selectedFilterId, _filterTextures);

            // Available Items
            const int itemRows = 10;
            const int itemColumns = 3;
            for (var y = 0; y < itemRows; y++)
            {
                for (var x = 0; x < itemColumns; x++)
                {
                    var left = 15 + x * 55;
                    var top = 70 + y * 55;
                    var itemIndex = y * itemColumns + x;
                    if (_filteredItems.Items.Length > itemIndex)
                    {
                        var item = _filteredItems.Items[itemIndex];
                        if (item.Icon == null)
                        {
                            item.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Icon.texture))
                        {
                            _queue.Add(new WorkshopItem(item.Part));
                        }
                        if (Event.current.type == EventType.Repaint && new Rect(left, top, 50, 50).Contains(Event.current.mousePosition))
                        {
                            mouseOverItem = item;
                        }
                    }
                }
            }

            if (_activePage > 0)
            {
                if (GUI.Button(new Rect(15, 645, 75, 25), "Prev"))
                {
                    _selectedPage = _activePage - 1;
                }
            }

            if (_activePage < _filteredItems.MaxPages)
            {
                if (GUI.Button(new Rect(100, 645, 75, 25), "Next"))
                {
                    _selectedPage = _activePage + 1;
                }
            }

            // Queued Items
            const int queueRows = 4;
            const int queueColumns = 7;
            GUI.Box(new Rect(190, 345, 440, 270), "Queue", queueSkin);
            for (var y = 0; y < queueRows; y++)
            {
                for (var x = 0; x < queueColumns; x++)
                {
                    var left = 205 + x * 60;
                    var top = 370 + y * 60;
                    var itemIndex = y * queueColumns + x;
                    if (_queue.Count > itemIndex)
                    {
                        var item = _queue[itemIndex];
                        if (item.Icon == null)
                        {
                            item.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Icon.texture))
                        {
                            _queue.Remove(item);
                        }
                        if (Event.current.type == EventType.Repaint && new Rect(left, top, 50, 50).Contains(Event.current.mousePosition))
                        {
                            mouseOverItem = item;
                        }
                    }
                }
            }

            // Tooltip
            GUI.Box(new Rect(190, 70, 440, 270), "");
            if (mouseOverItem != null)
            {
                var blueprint = WorkshopRecipeDatabase.ProcessPart(mouseOverItem.Part);
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), WorkshopUtils.GetKisStats(mouseOverItem.Part), statsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), blueprint.Print(ProductivityFactor), statsStyle);
                GUI.Box(new Rect(200, 190, 420, 140), WorkshopUtils.GetDescription(mouseOverItem.Part), tooltipDescriptionStyle);
            }

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
            GUI.Box(new Rect(250, 620, 280, 50), "");
            if (progress >= 1)
            {
                var color = GUI.color;
                GUI.color = new Color(0, 1, 0, 1);
                GUI.Box(new Rect(250, 620, 280 * progress / 100, 50), "");
                GUI.color = color;
            }
            GUI.Label(new Rect(250, 620, 280, 50), " " + progress.ToString("0.0") + " / 100");

            //Pause/resume production
            string buttonLabel = "||";
            if (manufacturingPaused || _processedItem == null)
                buttonLabel = ">";
            if (GUI.Button(new Rect(530, 620, 50, 50), buttonLabel) && _processedItem != null)
            {
                manufacturingPaused = !manufacturingPaused;
            }

            //Cancel production
            if (GUI.Button(new Rect(580, 620, 50, 50), "X"))
            {
                if (_confirmDelete)
                {
                    _processedItem.DisableIcon();
                    _processedItem = null;
                    _processedBlueprint = null;
                    progress = 0;
                    Status = "Online";

                    if (Animate && _heatAnimation != null && _workAnimation != null)
                    {
                        StartCoroutine(StopAnimations());
                    }
                    _confirmDelete = false;
                }

                else
                {
                    _confirmDelete = true;
                    ScreenMessages.PostScreenMessage("Click the cancel button again to confirm cancelling current production", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            if (GUI.Button(new Rect(_windowPos.width - 25, 5, 20, 20), "X"))
            {
                ContextMenuOpenWorkbench();
            }

            GUI.DragWindow();
        }
    }
}
