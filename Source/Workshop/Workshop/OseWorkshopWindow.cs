using System.Linq;

namespace Workshop
{
    using UnityEngine;

    public class OseWorkshopWindow
    {
        private readonly OseWorkshopQueue _queue;

        private readonly int _windowId;

        private Rect _windowPos;
        private Vector2 _scrollPosItems = Vector2.zero;
        private Vector2 _scrollPosQueue = Vector2.zero;

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

        public OseWorkshopWindow(OseWorkshopQueue queue)
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

            if (GUI.Button(new Rect(_windowPos.width - 24, 4, 20, 20), "X"))
            {
                Visible = false;
            }

            GUI.DragWindow();
        }

        private void DrawAvailableItems()
        {
            GUILayout.Label("- Available items -", OseGuiStyles.Heading());
            _scrollPosItems = GUILayout.BeginScrollView(_scrollPosItems, OseGuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(250f));
            foreach (var availablePart in PartLoader.LoadedPartsList.Where(availablePart => availablePart.HasStorableKasModule()).ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + availablePart.title, OseGuiStyles.Center(), GUILayout.Width(320f));
                GUILayout.Label(" " + availablePart.partPrefab.mass, OseGuiStyles.Center(), GUILayout.Width(80f));
                if (GUILayout.Button("Queue", OseGuiStyles.Button(), GUILayout.Width(80f)))
                {
                    _queue.Add(availablePart);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawQueuedItems()
        {
            GUILayout.Label("- Queued items -", OseGuiStyles.Heading());
            _scrollPosQueue = GUILayout.BeginScrollView(_scrollPosQueue, OseGuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(150f));
            foreach (var availablePart in _queue)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + availablePart.title, OseGuiStyles.Center(), GUILayout.Width(400f));
                if (GUILayout.Button("Remove", OseGuiStyles.Button(), GUILayout.Width(80f)))
                {
                    _queue.Remove(availablePart);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}
