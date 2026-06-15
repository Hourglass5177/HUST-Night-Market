using System;
using CampusNightMarket.Common;
using CampusNightMarket.Data;
using UnityEngine;

namespace CampusNightMarket.Tiles
{
    // 场景地块表现：绑定地块ID，处理点击、可达高亮和选中状态。
    public class TileView : MonoBehaviour
    {
        [SerializeField] private TileConfig tileConfig;
        [SerializeField] private string tileId;
        [SerializeField] private Transform standPoint;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color playerOwnedColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color npcOwnedColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color reachableColor = new Color(0.35f, 1f, 0.35f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.2f);

        private Material runtimeMaterial;
        private bool isReachable;
        private bool isSelected;
        private OwnerType owner;

        public string TileId
        {
            get
            {
                return tileConfig != null && !string.IsNullOrEmpty(tileConfig.tileId)
                    ? tileConfig.tileId
                    : tileId;
            }
        }

        public TileConfig TileConfig
        {
            get { return tileConfig; }
        }

        public Vector3 StandPosition
        {
            get { return standPoint == null ? transform.position : standPoint.position; }
        }

        public bool IsReachable
        {
            get { return isReachable; }
        }

        public event Action<TileView> Clicked;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                runtimeMaterial = targetRenderer.material;
                normalColor = runtimeMaterial.color;
            }

            RefreshColor();
        }

        private void OnMouseDown()
        {
            Clicked?.Invoke(this);
        }

        public void BindConfig(TileConfig config)
        {
            tileConfig = config;
            if (config != null)
            {
                tileId = config.tileId;
            }
        }

        public void SetReachable(bool value)
        {
            isReachable = value;
            if (!value)
            {
                isSelected = false;
            }

            RefreshColor();
        }

        public void SetSelected(bool value)
        {
            isSelected = value;
            RefreshColor();
        }

        public void SetOwner(OwnerType value)
        {
            owner = value;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (isSelected)
            {
                runtimeMaterial.color = selectedColor;
                return;
            }

            if (isReachable)
            {
                runtimeMaterial.color = reachableColor;
                return;
            }

            runtimeMaterial.color = owner == OwnerType.Player
                ? playerOwnedColor
                : owner == OwnerType.NPC
                    ? npcOwnedColor
                    : normalColor;
        }

        private void OnValidate()
        {
            if (tileConfig != null)
            {
                tileId = tileConfig.tileId;
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }
    }
}
