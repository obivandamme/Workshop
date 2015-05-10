using System.Linq;
using KIS;

namespace Workshop
{
    using UnityEngine;

    public class WorkshopWindow
    {
        private readonly WorkshopQueue _queue;

        private readonly int _windowId;

        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;
        private Vector2 _scrollPosInventories = Vector2.zero;

        private bool _visible;

        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (value)
                {
                    if (!_visible)
                    {
                        RenderingManager.AddToPostDrawQueue(3, DrawWindow);
                    }
                }
                else
                {
                    if (_visible)
                    {
                        RenderingManager.RemoveFromPostDrawQueue(3, DrawWindow);
                    }
                }

                _visible = value;
            }
        }

        public WorkshopWindow(WorkshopQueue queue)
        {
            _queue = queue;
            _windowPos = new Rect(Screen.width / 3, 35, 10, 10);
            _windowId = new System.Random().Next(65536);
        }

        private void DrawWindow()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            if (Visible)
            {
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
        }

        private void DrawWindowContents(int windowId)
        {
            GUILayout.Space(15);

            DrawAvailableItems();
            DrawQueuedItems();
            DrawAvailableInventories();

            if (GUI.Button(new Rect(_windowPos.width - 24, 4, 20, 20), "X"))
            {
                Visible = false;
            }

            GUI.DragWindow();
        }

        private void DrawAvailableItems()
        {
            GUILayout.Label("- Available items -", GuiStyles.Heading());
            _scrollPosItems = GUILayout.BeginScrollView(_scrollPosItems, GuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(250f));
            foreach (var availablePart in PartLoader.LoadedPartsList.Where(availablePart => availablePart.HasRecipeModule()).ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + availablePart.title, GuiStyles.Center(), GUILayout.Width(320f));
                GUILayout.Label(" " + availablePart.partPrefab.mass, GuiStyles.Center(), GUILayout.Width(80f));
                if (GUILayout.Button("Queue", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    _queue.Add(availablePart);
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
                    inventory.part.SetHighlight(true,false);
                }
                if (GUILayout.Button("Unhighlight", GuiStyles.Button(), GUILayout.Width(80f)))
                {
                    inventory.part.SetHighlight(false, false);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}
