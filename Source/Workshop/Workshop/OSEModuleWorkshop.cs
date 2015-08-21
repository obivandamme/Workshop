namespace Workshop
{
    using System;
    using System.Collections;
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
        private int _maxPage;

        private Rect _windowPos = new Rect(50, 50, 640, 680);
        private bool _showGui;

        // Processing
        private float _structuralResourcesRequired;
        private float _functionalResourcesRequired;
        private float _structuralRatio;
        private float _functionalRatio;
        private float _totalRatio;
        private PartResourceDefinition _structuralResource;
        private PartResourceDefinition _functionalResource;
        public string StructuralResource = "MaterialKits";
        public string FunctionalResource = "SpecializedParts";

        private void LoadResources()
        {
            _structuralResource = PartResourceLibrary.Instance.GetDefinition(StructuralResource);
            _functionalResource = PartResourceLibrary.Instance.GetDefinition(FunctionalResource);
        }

        private void PrepareRecipe(WorkshopItem item)
        {
            var partMass = item.Part.partPrefab.mass;
            var partCost = item.Part.cost;
            var partFundsPerTon = partCost / partMass;
            var structuralFundsPerTon = _structuralResource.unitCost / _structuralResource.density;
            var functionalFundsPerTon = _functionalResource.unitCost / _functionalResource.density;

            if (partFundsPerTon < structuralFundsPerTon)
            {
                _structuralRatio = 1;
                _functionalRatio = 0;
                _totalRatio = 1;
            }
            else if (partFundsPerTon > functionalFundsPerTon)
            {
                _structuralRatio = 0;
                _functionalRatio = 1;
                _totalRatio = 1;
            }
            else
            {
                _structuralRatio = 1;
                _functionalRatio = (structuralFundsPerTon - partFundsPerTon) / (partFundsPerTon - functionalFundsPerTon);
                _totalRatio = _structuralRatio + _functionalRatio;
            }

            var combinedDensity = (_structuralRatio * _structuralResource.density + _functionalRatio * _functionalResource.density) / _totalRatio;
            var combinedResourcesRequiredByMass = partMass / combinedDensity;

            var combinedUnitCost = (_structuralRatio * _structuralResource.unitCost + _functionalRatio * _functionalResource.unitCost) / _totalRatio;
            var combinedResourcesRequiredByCost = partCost / combinedUnitCost;

            var totalCombinedResourcesRequired = Math.Max(combinedResourcesRequiredByMass, combinedResourcesRequiredByCost);

            _structuralResourcesRequired = totalCombinedResourcesRequired * _structuralRatio / _totalRatio;
            _functionalResourcesRequired = totalCombinedResourcesRequired * _functionalRatio / _totalRatio;
        }

        // Configuration

        [KSPField]
        public bool Animate = false;

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
            _clock = new Clock();
            _queue = new WorkshopQueue();
        }

        public override void OnStart(StartState state)
        {
            if (WorkshopSettings.IsKISAvailable)
            {
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
            base.OnLoad(node);
            if (HighLogic.LoadedSceneIsFlight)
            {
                LoadModuleState(node);
            }
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
                    else
                    {
                        Debug.LogError("[OSE] - Unable to load workshop_emissive animation");
                    }
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
                    }
                    else
                    {
                        Debug.LogError("[OSE] - Unable to load work animation");
                    }
                }
            }
        }

        private void LoadFilters()
        {
            _filters = new FilterBase[11];
            _filters[0] = new FilterBase();
            _filters[1] = new FilterCategory(PartCategories.Pods);
            _filters[2] = new FilterCategory(PartCategories.FuelTank);
            _filters[3] = new FilterCategory(PartCategories.Engine);
            _filters[4] = new FilterCategory(PartCategories.Control);
            _filters[5] = new FilterCategory(PartCategories.Structural);
            _filters[6] = new FilterCategory(PartCategories.Aero);
            _filters[7] = new FilterCategory(PartCategories.Utility);
            _filters[8] = new FilterCategory(PartCategories.Science);
            _filters[9] = new FilterCategory(PartCategories.none);
            _filters[10] = new FilterModule("ModuleKISItem");

            _filterTextures = new Texture[11];
            _filterTextures[0] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_veryheavyrocketry");
            _filterTextures[1] = this.LoadTexture("Squad/PartList/SimpleIcons/RDicon_commandmodules");
            _filterTextures[2] = this.LoadTexture("Squad/PartList/SimpleIcons/RDicon_fuelSystems-advanced");
            _filterTextures[3] = this.LoadTexture("Squad/PartList/SimpleIcons/RDicon_propulsionSystems");
            _filterTextures[4] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_largecontrol");
            _filterTextures[5] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generalconstruction");
            _filterTextures[6] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advaerodynamics");
            _filterTextures[7] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generic");
            _filterTextures[8] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advsciencetech");
            _filterTextures[9] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_robotics");
            _filterTextures[10] = this.LoadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_evatech");
        }

        private Texture2D LoadTexture(string path)
        {
            var textureInfo = GameDatabase.Instance.databaseTexture.FirstOrDefault(t => t.name == path);
            if (textureInfo == null)
            {
                Debug.LogError("[OSE] - Filter - Unable to load texture file " + path);
                return new Texture2D(25, 25);
            }
            return textureInfo.texture;
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
                        _processedItem = new WorkshopItem(availablePart);
                        _massProcessed = double.Parse(cn.GetValue("MassProcessed"));
                        if (Animate && _heatAnimation != null && _workAnimation != null)
                        {
                            StartCoroutine(StartAnimations());
                        }
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
            Debug.Log("[OSE] - " + PartLoader.LoadedPartsList.Count + " loaded parts");
            Debug.Log("[OSE] - " + PartLoader.LoadedPartsList.Where(p => PartResearched(p)).Count() + " unlocked parts");
            var items = new List<WorkshopItem>();
            foreach (var loadedPart in PartLoader.LoadedPartsList)
            {
                try
                {
                    if (PartResearched(loadedPart) && KIS_Shared.GetPartVolume(loadedPart.partPrefab) <= _maxVolume)
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

        private bool PartResearched(AvailablePart p)
        {
            return ResearchAndDevelopment.PartTechAvailable(p) && ResearchAndDevelopment.PartModelPurchased(p);
        }

        private void LoadMaxVolume()
        {
            _maxVolume = MaxPartVolume;
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
                    _maxVolume = Math.Min(maxInventoyVolume, MaxPartVolume);
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
                builtPartNode.AddValue("Name", _processedItem.Part.name);
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
                ApplyFilter();
                ApplyPaging();
                RemoveCanceledItemFromQueue();
                AddNewItemToQueue();
                ProcessItem(deltaTime);
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
            if (_canceledItem == null)
            {
                return;
            }

            _canceledItem.DisableIcon();
            _queue.Remove(this._canceledItem);
            _canceledItem = null;
        }

        private void AddNewItemToQueue()
        {
            if (_addedItem == null)
            {
                return;
            }

            _queue.Add(_addedItem);
            _addedItem = null;
        }

        private void ApplyFilter()
        {
            if (_activeFilterId != _selectedFilterId)
            {
                foreach (var item in _filteredItems)
                {
                    item.DisableIcon();
                }

                var selectedFilter = _filters[_selectedFilterId];
                _activePage = 0;
                _filteredItems = selectedFilter.Filter(_availableItems, _activePage * 30);
                _activeFilterId = _selectedFilterId;
                _maxPage = _filteredItems.Count() / 30;
            }
        }

        private void ApplyPaging()
        {
            if (_activePage != _selectedPage)
            {
                foreach (var item in _filteredItems)
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

        private void ExecuteManufacturing(double deltaTime)
        {
            var preRequisitesMessage = CheckPrerequisites(deltaTime);

            if (preRequisitesMessage != "Ok")
            {
                Status = preRequisitesMessage;
            }
            else
            {
                Status = "Building " + _processedItem.Part.title;
                RequestResource(UpkeepResource, deltaTime);
                _massProcessed += ConsumeInputResource(deltaTime);
            }

            _progress = (float)((_massProcessed / _processedItem.Part.partPrefab.mass) * 100);
        }

        private double ConsumeInputResource(double deltaTime)
        {
            var density = PartResourceLibrary.Instance.GetDefinition(InputResource).density;
            var resourcesUsed = RequestResource(InputResource, deltaTime * ProductivityFactor);
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
            var destinationInventory = AddToContainer(_processedItem);
            if (destinationInventory != null)
            {
                ScreenMessages.PostScreenMessage("3D Printing of " + _processedItem.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
                _processedItem.DisableIcon();
                _processedItem = null;
                _massProcessed = 0;
                _progress = 0;
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
                ContextMenuOnOpenWorkbench();
            }
            base.OnInactive();
        }

        private void OnVesselChange(Vessel v)
        {
            if (_showGui)
            {
                ContextMenuOnOpenWorkbench();
            }
        }

        private string CheckPrerequisites(double deltaTime)
        {
            if (part.protoModuleCrew.Count < MinimumCrew)
            {
                return "Not enough Crew to operate";
            }

            if (AmountAvailable(UpkeepResource) < deltaTime)
            {
                return "Not enough " + this.UpkeepResource;
            }

            if (AmountAvailable(InputResource) < deltaTime * ProductivityFactor)
            {
                return "Not enough " + InputResource;
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
                    .Where(i => i.part.GetComponent<OseModuleInventoryPreference>().isFavored).ToArray();

                foreach (var inventory in favoredInventories)
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
                   GetInstanceID(),
                   _windowPos,
                   DrawWindowContents,
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

            var queueSkin = new GUIStyle(GUI.skin.box);
            queueSkin.alignment = TextAnchor.UpperCenter;
            queueSkin.padding.top = 5;

            // Filters
            _selectedFilterId = GUI.Toolbar(new Rect(15, 35, 615, 30), _selectedFilterId, _filterTextures);

            // Available Items
            const int ItemRows = 10;
            const int ItemColumns = 3;
            for (var y = 0; y < ItemRows; y++)
            {
                for (var x = 0; x < ItemColumns; x++)
                {
                    var left = 15 + x * 55;
                    var top = 70 + y * 55;
                    var itemIndex = y * ItemColumns + x;
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

            if (_activePage > 0)
            {
                if (GUI.Button(new Rect(15, 645, 75, 25), "Prev"))
                {
                    _selectedPage = _activePage - 1;
                }
            }

            if (_activePage < _maxPage)
            {
                if (GUI.Button(new Rect(100, 645, 75, 25), "Next"))
                {
                    _selectedPage = _activePage + 1;
                }
            }

            // Queued Items
            const int QueueRows = 4;
            const int QueueColumns = 7;
            GUI.Box(new Rect(190, 345, 440, 270), "Queue", queueSkin);
            for (var y = 0; y < QueueRows; y++)
            {
                for (var x = 0; x < QueueColumns; x++)
                {
                    var left = 205 + x * 60;
                    var top = 370 + y * 60;
                    var itemIndex = y * QueueColumns + x;
                    if (_queue.Count > itemIndex)
                    {
                        var item = _queue[itemIndex];
                        if (item.Icon == null)
                        {
                            item.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Icon.texture))
                        {
                            _canceledItem = item;
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
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), mouseOverItem.GetKisStats(), statsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), mouseOverItem.GetWorkshopStats(InputResource, ProductivityFactor), statsStyle);
                GUI.Box(new Rect(200, 190, 420, 140), mouseOverItem.GetDescription(), tooltipDescriptionStyle);
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
    }
}
