namespace Workshop
{
	using System;
	using System.Collections;
	using System.Linq;
	using System.Collections.Generic;
	
	using KIS;

	using UnityEngine;

	using Recipes;
    using System.Reflection;
    using System.Text;

    public class OseModuleWorkshop : PartModule
	{
        private static Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;

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
        private FilterSearch _searchFilter  = new FilterSearch();
		private Texture[] _filterTextures;

		private int _activeFilterId;
		private int _selectedFilterId;

		private int _activePage;
		private int _selectedPage;

        private string _oldSsearchText = "";

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

		[KSPField]
		public bool UseSpecializationBonus = true;

		[KSPField]
		public string ExperienceEffect = "RepairSkill";

		[KSPField]
		public float SpecialistEfficiencyFactor = 0.02f;
		
		[KSPField(guiName = "Workshop Status", guiActive = true, guiActiveEditor = false)]
		public string Status = "Online";

        [KSPField]
        public string WorkAnimationName = "work";

		protected float adjustedProductivity = 1.0f;

		private readonly Texture2D _pauseTexture;
		private readonly Texture2D _playTexture;
		private readonly Texture2D _binTexture;

        [KSPEvent(guiName = "Open OSE Workbench", guiActive = true, guiActiveEditor = false)]
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
			_pauseTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_pause");
			_playTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_play");
			_binTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_bin");
		}

        public override string GetInfo()
        {
            var sb = new StringBuilder("<color=#8dffec>KIS Part Printing Workshop</color>");

            sb.Append($"\nMinimum Crew: {MinimumCrew}");
            sb.Append($"\nBase productivity factor: {ProductivityFactor:P0}");
            sb.Append($"\nUse specialist bonus: ");
            sb.Append(RUIutils.GetYesNoUIString(UseSpecializationBonus));
            if (UseSpecializationBonus)
            {
                sb.Append($"\nSpecialist skill: {ExperienceEffect}");
                sb.Append($"\nSpecialist bonus: {SpecialistEfficiencyFactor:P0} per level");
            }
            return sb.ToString();
        }

        public override void OnStart(StartState state)
		{
			if (HighLogic.LoadedSceneIsFlight && WorkshopSettings.IsKISAvailable)
			{
                WorkshopUtils.Log("KIS is available - Initialize Workshop");
				SetupAnimations();
				LoadMaxVolume();
				LoadFilters();
				GameEvents.onVesselChange.Add(OnVesselChange);
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
                    WorkshopUtils.LogError("Unable to load workshop_emissive animation");
				}
				foreach (var animator in part.FindModelAnimators(WorkAnimationName))
				{
					_workAnimation = animator[WorkAnimationName];
					if (_workAnimation != null)
					{
						_workAnimation.speed = 0;
						_workAnimation.enabled = true;
						_workAnimation.wrapMode = WrapMode.ClampForever;
						animator.Blend(WorkAnimationName);
						break;
					}
                    WorkshopUtils.LogError("Unable to load work animation");
				}
			}
		}

		private void LoadFilters()
		{
			var filters = new List<FilterBase>();
			var filterTextures = new List<Texture>();
			
			var categoryFilterAddOns = vessel.FindPartModulesImplementing<OseModuleCategoryAddon>();
			if (categoryFilterAddOns != null)
			{
				foreach (var addon in categoryFilterAddOns.Distinct(new OseModuleCategoryAddonEqualityComparer()).ToArray())
				{
                    WorkshopUtils.LogVerbose($"Found addon for category: {addon.Category}");
					filters.Add(new FilterCategory(addon.Category));
					filterTextures.Add(WorkshopUtils.LoadTexture(addon.IconPath));
				}
				
			}

			_filters = filters.ToArray();
			_filterTextures = filterTextures.ToArray();
            _searchFilter = new FilterSearch();
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
            WorkshopUtils.LogVerbose(PartLoader.LoadedPartsList.Count + " loaded parts");
            WorkshopUtils.LogVerbose(PartLoader.LoadedPartsList.Count(WorkshopUtils.PartResearched) + " unlocked parts");

			var items = new List<WorkshopItem>();
			foreach (var loadedPart in PartLoader.LoadedPartsList.Where(p => p.name != "flag" && !p.name.StartsWith("kerbalEVA")))
			{
				try
				{
					if (IsValid(loadedPart))
					{
						items.Add(new WorkshopItem(loadedPart));
					}
				}
				catch (Exception ex)
				{
                    WorkshopUtils.LogError("Part " + loadedPart.name + " could not be added to available parts list", ex);
				}
			}
			_availableItems = items.OrderBy(i => i.Part.title).ToArray();
            if (string.IsNullOrEmpty(_searchFilter.FilterText))
                _filteredItems = _filters[_activeFilterId].Filter(_availableItems, 0);
            else
                _filteredItems = _searchFilter.Filter(_availableItems, 0);
		}

		private bool IsValid(AvailablePart loadedPart)
		{
			return WorkshopUtils.PartResearched(loadedPart) && WorkshopUtils.GetPackedPartVolume(loadedPart) <= _maxVolume && !WorkshopBlacklistItemsDatabase.Blacklist.Contains(loadedPart.name);
		}

		private void LoadMaxVolume()
		{
			try
			{
				var inventories = KISWrapper.GetInventories(vessel);
				if (inventories.Count == 0)
				{
                    WorkshopUtils.LogError("No Inventories found on this vessel!");

				}
				else
				{

                    WorkshopUtils.Log(inventories.Count + " inventories found on this vessel!");
					_maxVolume = inventories.Max(i => i.maxVolume);
				}
			}
			catch (Exception ex)
			{
                WorkshopUtils.LogError("Error while determing maximum volume of available inventories!", ex);
			}
            WorkshopUtils.Log($"Max volume is: {_maxVolume} liters");
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
				UpdateProductivity();
				ApplyFilter();
				ProcessItem();
			}
			catch (Exception ex)
			{
                WorkshopUtils.LogError("OseModuleWorkshop_OnUpdate", ex);
			}
			base.OnUpdate();
		}

		private void UpdateProductivity()
		{
			if (_processedItem != null && UseSpecializationBonus)
			{ 
				adjustedProductivity = WorkshopUtils.GetProductivityBonus(this.part, ExperienceEffect, SpecialistEfficiencyFactor, ProductivityFactor);
			}
		}

		private void ProcessItem()
		{
			if (manufacturingPaused)
			{
				Status = "Paused";
				return;
			}

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
			if (_activeFilterId != _selectedFilterId || _activePage != _selectedPage || _oldSsearchText != _searchFilter.FilterText)
			{
				foreach (var item in _filteredItems.Items)
				{
					item.DisableIcon();
				}

                if (_activeFilterId != _selectedFilterId)
                {
                    _activePage = 0;
                    _selectedPage = 0;
                }

				var selectedFilter = _filters[_selectedFilterId];
                if (string.IsNullOrEmpty(_searchFilter.FilterText))
                    _filteredItems = selectedFilter.Filter(_availableItems, _selectedPage * 30);
                else
                    _filteredItems = _searchFilter.Filter(_availableItems, _selectedPage * 30);
                _activeFilterId = _selectedFilterId;
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
			var unitsToConsume = Math.Min(resourceToConsume.Units - resourceToConsume.Processed, TimeWarp.deltaTime * adjustedProductivity);

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
            LoadMaxVolume();
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
			//GUI.skin = HighLogic.Skin;
			//GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			//GUI.skin.button.alignment = TextAnchor.MiddleCenter;

			_windowPos = GUI.Window(GetInstanceID(), _windowPos, DrawWindowContents, "Workbench (" + _maxVolume + " litres - " + _filters[_activeFilterId] + ") v." + modVersion.ToString());
		}

		private void DrawWindowContents(int windowId)
        {
            _selectedFilterId = GUI.Toolbar(new Rect(15, 35, 615, 30), _selectedFilterId, _filterTextures);

            WorkshopItem mouseOverItem = null;

            mouseOverItem = DrawAvailableItems(mouseOverItem);
            mouseOverItem = DrawQueue(mouseOverItem);
            DrawMouseOverItem(mouseOverItem);

            DrawPrintProgress();

            if (GUI.Button(new Rect(_windowPos.width - 25, 5, 20, 20), "X"))
            {
                ContextMenuOpenWorkbench();
            }
            GUI.DragWindow();
        }

        private void DrawPrintProgress()
        {
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
            Texture2D buttonTexture = _pauseTexture;
            if (manufacturingPaused || _processedItem == null)
                buttonTexture = _playTexture;
            if (GUI.Button(new Rect(530, 620, 50, 50), buttonTexture) && _processedItem != null)
            {
                manufacturingPaused = !manufacturingPaused;
            }

            //Cancel production
            if (GUI.Button(new Rect(580, 620, 50, 50), _binTexture))
            {
                if (_confirmDelete)
                {
                    _processedItem.DisableIcon();
                    _processedItem = null;
                    _processedBlueprint = null;
                    progress = 0;
                    manufacturingPaused = false;
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
        }

        private WorkshopItem DrawAvailableItems(WorkshopItem mouseOverItem)
        {
             

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
                if (GUI.Button(new Rect(15, 615, 75, 25), "Prev"))
                {
                    _selectedPage = _activePage - 1;
                }
            }

            if (_activePage < _filteredItems.MaxPages)
            {
                if (GUI.Button(new Rect(100, 615, 75, 25), "Next"))
                {
                    _selectedPage = _activePage + 1;
                }
            }

            // search box
            _oldSsearchText = _searchFilter.FilterText;
            GUI.Label(new Rect(15, 645, 65, 25), "Find: ", UI.UIStyles.StatsStyle);
            _searchFilter.FilterText = GUI.TextField(new Rect(75, 645, 100, 25), _searchFilter.FilterText);

            return mouseOverItem;
        }

        private WorkshopItem DrawQueue(WorkshopItem mouseOverItem)
        {
            const int queueRows = 4;
            const int queueColumns = 7;

            GUI.Box(new Rect(190, 345, 440, 270), "Queue", UI.UIStyles.QueueSkin);
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

            return mouseOverItem;
            // Tooltip
        }

        private void DrawMouseOverItem(WorkshopItem mouseOverItem)
        {
            GUI.Box(new Rect(190, 70, 440, 270), "");
            if (mouseOverItem != null)
            {
                var blueprint = WorkshopRecipeDatabase.ProcessPart(mouseOverItem.Part);
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), WorkshopUtils.GetKisStats(mouseOverItem.Part), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), blueprint.Print(adjustedProductivity), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(200, 190, 420, 25), mouseOverItem.Part.title, UI.UIStyles.TitleDescriptionStyle);
                GUI.Box(new Rect(200, 220, 420, 110), mouseOverItem.Part.description, UI.UIStyles.TooltipDescriptionStyle);
            }
        }
    }
}
