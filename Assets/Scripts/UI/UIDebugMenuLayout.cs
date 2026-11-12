using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Keeps the AR debug menu where the scene places it and makes its session table fit its panel.
    /// </summary>
    // Runs after ARDebugMenu, so Start and LateUpdate here come after it has moved things and built its table.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(ARDebugMenu))]
    public class UIDebugMenuLayout : MonoBehaviour
    {
        private struct Layout
        {
            public RectTransform Rect;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 SizeDelta;
            public Vector2 Position;
            public Quaternion Rotation;
        }

        private const string ColumnNumberName = "ConfigurationLabel";
        private const string ColumnStripeName = "ConfigurationColBackground";
        private const int FeatureFontSize = 16;
        private const float TableShiftRight = 54f;
        private const float FirstColumnX = 228f;
        private const float ColumnSpacing = 30f;
        private const float RowSpacing = 30f;
        private const float RightPadding = 30f;

        private readonly List<Layout> _layouts = new List<Layout>();
        private ARDebugMenu _menu;

        private void Awake()
        {
            _menu = GetComponent<ARDebugMenu>();

            Remember(_menu.toolbar);
            Remember(_menu.displayInfoMenuButton.gameObject);
            Remember(_menu.displayConfigurationsMenuButton.gameObject);
            Remember(_menu.displayCameraConfigurationsMenuButton.gameObject);
            Remember(_menu.displayDebugOptionsMenuButton.gameObject);
            Remember(_menu.infoMenu);
            Remember(_menu.configurationMenu);
            Remember(_menu.cameraConfigurationMenu);
            Remember(_menu.debugOptionsMenu);
        }

        private void Start()
        {
            RestoreLayouts();
        }

        // ARDebugMenu builds the table once, in its first LateUpdate.
        private void LateUpdate()
        {
            FitSessionTable();
            enabled = false;
        }

        // Unity hard-codes this table: its labels start outside the panel and its column stripes are far too long.
        private void FitSessionTable()
        {
            var table = (RectTransform)_menu.configurationMenuRoot.transform;
            int rows = 0;
            int columns = 0;

            foreach (Text label in table.GetComponentsInChildren<Text>(true))
            {
                if (label.name == ColumnNumberName)
                {
                    columns++;
                }
                else
                {
                    rows++;
                    label.fontSize = FeatureFontSize;
                }
            }

            if (rows == 0)
            {
                return;
            }

            table.anchoredPosition += new Vector2(TableShiftRight, 0f);

            var panel = (RectTransform)_menu.configurationMenu.transform;
            float panelWidth = table.anchoredPosition.x + FirstColumnX + (columns - 1) * ColumnSpacing + RightPadding;
            panel.sizeDelta = new Vector2(panelWidth, panel.sizeDelta.y);

            foreach (RectTransform child in table)
            {
                if (child.name == ColumnStripeName)
                {
                    child.sizeDelta = new Vector2(child.sizeDelta.x, 40f + rows * RowSpacing);
                }
            }
        }

        private void Remember(GameObject element)
        {
            RectTransform rect = element.GetComponent<RectTransform>();
            _layouts.Add(new Layout
            {
                Rect = rect,
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                Pivot = rect.pivot,
                SizeDelta = rect.sizeDelta,
                Position = rect.anchoredPosition,
                Rotation = rect.localRotation
            });
        }

        private void RestoreLayouts()
        {
            foreach (Layout layout in _layouts)
            {
                layout.Rect.anchorMin = layout.AnchorMin;
                layout.Rect.anchorMax = layout.AnchorMax;
                layout.Rect.pivot = layout.Pivot;
                layout.Rect.sizeDelta = layout.SizeDelta;
                layout.Rect.anchoredPosition = layout.Position;
                layout.Rect.localRotation = layout.Rotation;
            }
        }
    }
}
