using System.Linq;

namespace Workshop
{
    using UnityEngine;

    public class OseWorkshopWindow
    {
        private readonly OseModuleWorkshop _workshop;

        private Rect _windowPos;

        private readonly int _windowId;

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

        private Vector2 _scrollPos = Vector2.zero;

        public OseWorkshopWindow(OseModuleWorkshop workshop)
        {
            _workshop = workshop;
            _windowPos = new Rect(Screen.width / 3, 35, 10, 10);
            _windowId = new System.Random().Next(65536);
        }

        private void DrawWindow()
        {
            if (Visible && CanDraw())
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
            GUILayout.Label("- Available items -", OseGuiStyles.Heading());
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, OseGuiStyles.Databox(), GUILayout.Width(600f), GUILayout.Height(400f));

            foreach (var availablePart in _workshop.GetStorableParts().ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" " + availablePart.title, "Name"), OseGuiStyles.Center(), GUILayout.Width(320f));
                GUILayout.Label(new GUIContent(" " + availablePart.partPrefab.mass, "Mass"), OseGuiStyles.Center(), GUILayout.Width(80f));
                if (GUILayout.Button(new GUIContent("Build", "Select part for production"), OseGuiStyles.Button(), GUILayout.Width(50f)))
                {
                    _workshop.OnPartSelected(availablePart);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (GUI.Button(new Rect(_windowPos.width - 24, 4, 20, 20), "X"))
            {
                this.Visible = false;
            }

            GUI.DragWindow();
        }

        private bool CanDraw()
        {
            return _workshop.vessel == FlightGlobals.ActiveVessel;
        }
    }
}
