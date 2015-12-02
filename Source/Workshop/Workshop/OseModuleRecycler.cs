namespace Workshop
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using UnityEngine;

    using Workshop.KIS;
    using Workshop.Recipes;

    public class OseModuleRecycler : PartModule
    {
        private Blueprint _processedBlueprint;
        private WorkshopItem _processedItem;
        
        private int _addedItemKey;
        private ModuleKISInventory _addedItemInventory;
        
        private float _progress;

        private readonly ResourceBroker _broker;
        private readonly WorkshopQueue _queue;

        // GUI Properties
        private int _activePage;
        private int _selectedPage;

        private Rect _windowPos;
        private bool _showGui;

        [KSPField]
        public float ConversionRate = 0.25f;

        [KSPField]
        public float ProductivityFactor = 0.1f;

        [KSPField]
        public string UpkeepResource = "ElectricCharge";

        [KSPField]
        public int MinimumCrew = 2;

        [KSPField(guiName = "Recycler Status", guiActive = true)]
        public string Status = "Online";

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
                    if (this._processedItem != null)
                    {
                        this._processedItem.DisableIcon();
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
                this.Events["ContextMenuOnOpenRecycler"].guiActive = false;
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

        public override void OnUpdate()
        {
            try
            {
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
            if (_progress >= 100)
            {
                FinishManufacturing();
            }
            else if (this._processedItem != null)
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
            var unitsToProduce = Math.Min(resourceToProduce.Units - resourceToProduce.Processed, TimeWarp.deltaTime * ProductivityFactor);

            if (part.protoModuleCrew.Count < MinimumCrew)
            {
                Status = "Not enough Crew to operate";
            }
            else if (_broker.AmountAvailable(part, UpkeepResource, TimeWarp.deltaTime, "Both") < TimeWarp.deltaTime)
            {
                Status = "Not enough " + UpkeepResource;
            }
            else
            {
                Status = "Recycling " + _processedItem.Part.title;
                _broker.RequestResource(part, UpkeepResource, TimeWarp.deltaTime, TimeWarp.deltaTime, "Both");
                resourceToProduce.Processed += _broker.StoreResource(part, resourceToProduce.Name, unitsToProduce, TimeWarp.deltaTime, "Both");
                _progress = (float)(_processedBlueprint.GetProgress() * 100);
            }
        }
        
        private void FinishManufacturing()
        {
            ScreenMessages.PostScreenMessage("Recycling of " + _processedItem.Part.title + " finished.", 5, ScreenMessageStyle.UPPER_CENTER);
            _processedItem.DisableIcon();
            _processedItem = null;
            _processedBlueprint = null;
            _progress = 0;
            Status = "Online";
        }

        public override void OnInactive()
        {
            if (_showGui)
            {
                this.ContextMenuOnOpenRecycler();
            }
            base.OnInactive();
        }

        void OnVesselChange(Vessel v)
        {
            if (_showGui)
            {
                this.ContextMenuOnOpenRecycler();
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

            _windowPos = GUILayout.Window(GetInstanceID(), _windowPos, DrawWindowContents, "Recycler Menu");
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
                    var top = 70 + y * ItemColumns + x;
                    var itemIndex = y * ItemColumns + x;
                    if (availableItems.Length > itemIndex)
                    {
                        var item = availableItems[itemIndex];
                        if (item.Value.icon == null)
                        {
                            item.Value.EnableIcon(64);
                        }
                        if (GUI.Button(new Rect(left, top, 50, 50), item.Value.icon.texture))
                        {
                            _queue.Add(new WorkshopItem(item.Value.availablePart));
                            item.Value.Delete();
                        }
                        if (Event.current.type == EventType.Repaint && new Rect(left, top, 50, 50).Contains(Event.current.mousePosition))
                        {
                            mouseOverItem = new WorkshopItem(item.Value.availablePart);
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
            
            // Queued Items
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
                foreach (var resource in blueprint)
                {
                    resource.Units *= ConversionRate;
                }
                GUI.Box(new Rect(200, 80, 100, 100), mouseOverItem.Icon.texture);
                GUI.Box(new Rect(310, 80, 150, 100), mouseOverItem.GetKisStats(), statsStyle);
                GUI.Box(new Rect(470, 80, 150, 100), blueprint.Print(ProductivityFactor), statsStyle);
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
                this.ContextMenuOnOpenRecycler();
            }

            GUI.DragWindow();
        }

        private void Test()
        {
            var broker = new ResourceBroker();
            var res = broker.RequestResource(part, "", 5, TimeWarp.deltaTime, "None");
        }
    }
}
