﻿namespace Workshop
{
    using System;
    using System.Linq;

    using UnityEngine;

    using KIS;
    using Recipes;
    using System.Text;

    public class OseModuleRecycler : PartModule
    {
        private Blueprint _processedBlueprint;
        private WorkshopItem _processedItem;

        private readonly ResourceBroker _broker;
        private readonly WorkshopQueue _queue;

        // GUI Properties
        private int _activePage;
        private int _selectedPage;

        private Rect _windowPos = new Rect(50, 50, 640, 680);
        private bool _showGui;

        private Texture2D _pauseTexture;
        private Texture2D _playTexture;
        private Texture2D _binTexture;


        [KSPField(isPersistant = true)]
        public bool recyclingPaused;

        [KSPField(isPersistant = true)]
        public float progress;

        [KSPField]
        public float ConversionRate = 0.25f;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string UpkeepResource = "ElectricCharge";

        [KSPField]
        public float UpkeepAmount = 1.0f;

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField()]
        public bool UseSpecializationBonus = true;

        [KSPField()]
        public string ExperienceEffect = "RepairSkill";

        [KSPField()]
        public float SpecialistEfficiencyFactor = 0.02f;

        [KSPField(guiName = "Recycler Status", guiActive = true)]
        public string Status = "Online";

        protected float adjustedProductivity = 1.0f;

        [KSPEvent(guiActive = true, guiName = "Open Recycler")]
        public void ContextMenuOnOpenRecycler()
        {
            if (_showGui)
            {
                foreach (var inventory in KISWrapper.GetInventories(vessel).Where(i => i.showGui == false).ToList())
                {
                    foreach (var item in inventory.items)
                    {
                        item.Value.DisableIcon();
                    }
                    foreach (var item in _queue)
                    {
                        item.DisableIcon();
                    }
                    if (_processedItem != null)
                    {
                        _processedItem.DisableIcon();
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
            _queue = new WorkshopQueue();
            _broker = new ResourceBroker();
            _pauseTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_pause");
            _playTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_play");
            _binTexture = WorkshopUtils.LoadTexture("Workshop/Assets/Icons/icon_bin");
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder("<color=#8dffec>KIS Part Recycker</color>");

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
                GameEvents.onVesselChange.Add(OnVesselChange);
            }
            else
            {
                Fields["Status"].guiActive = false;
                Events["ContextMenuOnOpenRecycler"].guiActive = false;
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

        private void UpdateProductivity()
        {
            if (_processedItem != null && UseSpecializationBonus)
                adjustedProductivity = WorkshopUtils.GetProductivityBonus(part, ExperienceEffect, SpecialistEfficiencyFactor, ProductivityFactor);
        }

        public override void OnUpdate()
        {
            try
            {
                UpdateProductivity();
                ApplyPaging();
                ProcessItem();
            }
            catch (Exception ex)
            {
                WorkshopUtils.LogError("OseModuleWorkshop_OnUpdate", ex);
            }
            base.OnUpdate();
        }

        private void ProcessItem()
        {
            if (recyclingPaused)
            {
                Status = "Paused";
            }
            else if (progress >= 100)
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

        private void ApplyPaging()
        {
            if (_activePage != _selectedPage)
            {
                foreach (var inventory in KISWrapper.GetInventories(vessel).Where(i => i.showGui == false).ToList())
                {
                    foreach (var item in inventory.items)
                    {
                        item.Value.DisableIcon();
                    }
                }
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
                foreach (var resource in _processedBlueprint)
                {
                    resource.Units *= ConversionRate;
                }
            }
        }

        private void ExecuteManufacturing()
        {
            var resourceToProduce = _processedBlueprint.First(r => r.Processed < r.Units);
            var unitsToProduce = Math.Min(resourceToProduce.Units - resourceToProduce.Processed, TimeWarp.deltaTime * adjustedProductivity);

            if (part.protoModuleCrew.Count < MinimumCrew)
            {
                Status = "Not enough Crew to operate";
            }
            else if (_broker.AmountAvailable(part, UpkeepResource, TimeWarp.deltaTime, ResourceFlowMode.ALL_VESSEL) < TimeWarp.deltaTime)
            {
                Status = "Not enough " + UpkeepResource;
            }
            else
            {
                Status = "Recycling " + _processedItem.Part.title;
                _broker.RequestResource(part, UpkeepResource, UpkeepAmount, TimeWarp.deltaTime, ResourceFlowMode.ALL_VESSEL);
                _broker.StoreResource(part, resourceToProduce.Name, unitsToProduce, TimeWarp.deltaTime, ResourceFlowMode.ALL_VESSEL);
                resourceToProduce.Processed += unitsToProduce;
                progress = (float)(_processedBlueprint.GetProgress() * 100);
            }
        }

        private void FinishManufacturing()
        {
            ScreenMessages.PostScreenMessage("Recycling of " + _processedItem.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
            CleanupRecycler();
        }

        private void CancelManufacturing()
        {
            ScreenMessages.PostScreenMessage("Recycling of " + _processedItem.Part.title + " canceled.", 5, ScreenMessageStyle.UPPER_CENTER);
            CleanupRecycler();
            recyclingPaused = false;
        }

        private void CleanupRecycler()
        {
            _processedItem.DisableIcon();
            _processedItem = null;
            _processedBlueprint = null;
            progress = 0;
            Status = "Online";
        }

        public override void OnInactive()
        {
            if (_showGui)
            {
                ContextMenuOnOpenRecycler();
            }
            base.OnInactive();
        }

        void OnVesselChange(Vessel v)
        {
            if (_showGui)
            {
                ContextMenuOnOpenRecycler();
            }
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

            _windowPos = GUI.Window(GetInstanceID(), _windowPos, DrawWindowContents, "Recycler Menu");
        }

        private void DrawWindowContents(int windowId)
        {
            WorkshopItem mouseOverItem = null;
            KIS_Item mouseOverItemKIS = null;

            mouseOverItemKIS = DrawInventoryItems(mouseOverItemKIS);
            mouseOverItem = DrawQueue(mouseOverItem);
            DrawMouseOverItem(mouseOverItem, mouseOverItemKIS);
            DrawRecyclingProgress();

            if (GUI.Button(new Rect(_windowPos.width - 25, 5, 20, 20), "X"))
            {
                ContextMenuOnOpenRecycler();
            }

            GUI.DragWindow();
        }

        KIS_Item DrawInventoryItems(KIS_Item mouseOverItem)
        {
            // AvailableItems
            const int ItemRows = 10;
            const int ItemColumns = 3;
            var availableItems = KISWrapper.GetInventories(vessel).SelectMany(i => i.items).ToArray();
            var maxPage = availableItems.Length / 30;

            for (var y = 0; y < ItemRows; y++)
            {
                for (var x = 0; x < ItemColumns; x++)
                {
                    var left = 15 + x * 55;
                    var top = 70 + y * 55;
                    var itemIndex = y * ItemColumns + x;
                    if (availableItems.Length > itemIndex)
                    {
                        var item = availableItems[itemIndex];
                        if (item.Value.Icon == null)
                        {
                            item.Value.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Value.Icon.texture))
                        {
                            _queue.Add(new WorkshopItem(item.Value.availablePart));
                            item.Value.StackRemove(1);
                        }
                        if (item.Value.stackable)
                        {
                            GUI.Label(new Rect(left, top, 50, 50), item.Value.quantity.ToString("x#"), UI.UIStyles.lowerRightStyle);
                        }
                        if (Event.current.type == EventType.Repaint && new Rect(left, top, 50, 50).Contains(Event.current.mousePosition))
                        {
                            mouseOverItem = item.Value;
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

            if (_activePage < maxPage)
            {
                if (GUI.Button(new Rect(100, 645, 75, 25), "Next"))
                {
                    _selectedPage = _activePage + 1;
                }
            }
            return mouseOverItem;
        }

        WorkshopItem DrawQueue(WorkshopItem mouseOverItem)
        {
            // Queued Items
            const int QueueRows = 4;
            const int QueueColumns = 7;
            GUI.Box(new Rect(190, 345, 440, 270), "Queue", UI.UIStyles.QueueSkin);
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
        }

        void DrawMouseOverItem(WorkshopItem mouseOverItem, KIS_Item mouseOverItemKIS)
        {
            // Tooltip
            GUI.Box(new Rect(190, 70, 440, 270), "");
            if (mouseOverItem != null)
            {
                var blueprint = WorkshopRecipeDatabase.ProcessPart(mouseOverItem.Part);
                foreach (var resource in blueprint)
                {
                    resource.Units *= ConversionRate;
                }
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), WorkshopUtils.GetKisStats(mouseOverItem.Part), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), blueprint.Print(adjustedProductivity), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(200, 190, 420, 25), mouseOverItem.Part.title, UI.UIStyles.TitleDescriptionStyle);
                GUI.Box(new Rect(200, 220, 420, 110), mouseOverItem.Part.description, UI.UIStyles.TooltipDescriptionStyle);
            }
            else if (mouseOverItemKIS != null)
            {
                var blueprint = WorkshopRecipeDatabase.ProcessPart(mouseOverItemKIS.availablePart);
                foreach (var resource in blueprint)
                {
                    resource.Units *= ConversionRate;
                }
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItemKIS.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), WorkshopUtils.GetKisStats(mouseOverItemKIS.availablePart), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), blueprint.Print(adjustedProductivity), UI.UIStyles.StatsStyle);
                GUI.Box(new Rect(200, 190, 420, 25), mouseOverItemKIS.availablePart.title, UI.UIStyles.TitleDescriptionStyle);
                GUI.Box(new Rect(200, 220, 420, 110), mouseOverItemKIS.availablePart.description, UI.UIStyles.TooltipDescriptionStyle);
            }

        }

        void DrawRecyclingProgress()
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
            GUI.Box(new Rect(250, 620, 260, 50), "");
            if (progress >= 1)
            {
                var color = GUI.color;
                GUI.color = new Color(0, 1, 0, 1);
                GUI.Box(new Rect(250, 620, 260 * progress / 100, 50), "");
                GUI.color = color;
            }
            GUI.Label(new Rect(250, 620, 260, 50), " " + progress.ToString("0.0") + " / 100");

            // Toolbar
            if (recyclingPaused)
            {
                if (GUI.Button(new Rect(520, 620, 50, 50), _playTexture))
                {
                    recyclingPaused = false;
                }
            }
            else
            {
                if (GUI.Button(new Rect(520, 620, 50, 50), _pauseTexture))
                {
                    recyclingPaused = true;
                }
            }

            if (GUI.Button(new Rect(580, 620, 50, 50), _binTexture))
            {
                CancelManufacturing();
            }
        }
    }

    
}
