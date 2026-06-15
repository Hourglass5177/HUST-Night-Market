using System;
using System.Collections.Generic;
using CampusNightMarket.Data;
using CampusNightMarket.Tiles;
using UnityEngine;

namespace CampusNightMarket.Map
{
    // 地图管理器：加载地图配置，并提供地块查询、可达范围和最短路径。
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private MapConfig currentMap;

        private readonly Dictionary<string, TileConfig> tileConfigs =
            new Dictionary<string, TileConfig>();
        private readonly Dictionary<string, TileView> tileViews =
            new Dictionary<string, TileView>();

        public MapConfig CurrentMap
        {
            get { return currentMap; }
        }

        public event Action<MapConfig> MapInitialized;

        // 初始化地图并建立地块ID查询表。
        public bool InitializeMap(MapConfig mapConfig, out string reason)
        {
            reason = string.Empty;
            tileConfigs.Clear();

            if (mapConfig == null)
            {
                reason = "地图配置为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mapConfig.mapId))
            {
                reason = "地图ID为空。";
                return false;
            }

            if (mapConfig.tileList == null || mapConfig.tileList.Count == 0)
            {
                reason = "地图没有配置任何地块。";
                return false;
            }

            for (int i = 0; i < mapConfig.tileList.Count; i++)
            {
                TileConfig tileConfig = mapConfig.tileList[i];
                if (tileConfig == null)
                {
                    reason = "地图地块列表中存在空配置，索引：" + i;
                    tileConfigs.Clear();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(tileConfig.tileId))
                {
                    reason = "存在地块ID为空的配置：" + tileConfig.name;
                    tileConfigs.Clear();
                    return false;
                }

                if (tileConfigs.ContainsKey(tileConfig.tileId))
                {
                    reason = "地块ID重复：" + tileConfig.tileId;
                    tileConfigs.Clear();
                    return false;
                }

                tileConfigs.Add(tileConfig.tileId, tileConfig);
            }

            if (!tileConfigs.ContainsKey(mapConfig.startTileId))
            {
                reason = "起点地块不存在：" + mapConfig.startTileId;
                tileConfigs.Clear();
                return false;
            }

            foreach (TileConfig tileConfig in tileConfigs.Values)
            {
                if (tileConfig.nextTileIds == null)
                {
                    continue;
                }

                for (int i = 0; i < tileConfig.nextTileIds.Count; i++)
                {
                    string nextTileId = tileConfig.nextTileIds[i];
                    if (!tileConfigs.ContainsKey(nextTileId))
                    {
                        reason = tileConfig.tileId + " 引用了不存在的相邻地块：" + nextTileId;
                        tileConfigs.Clear();
                        return false;
                    }
                }
            }

            currentMap = mapConfig;
            WarnAboutMapConnections();
            MapInitialized?.Invoke(currentMap);
            return true;
        }

        public bool ContainsTile(string tileId)
        {
            return !string.IsNullOrEmpty(tileId) && tileConfigs.ContainsKey(tileId);
        }

        public TileConfig GetTileConfig(string tileId)
        {
            if (string.IsNullOrEmpty(tileId))
            {
                return null;
            }

            tileConfigs.TryGetValue(tileId, out TileConfig tileConfig);
            return tileConfig;
        }

        public List<TileConfig> GetAllTiles()
        {
            return currentMap == null || currentMap.tileList == null
                ? new List<TileConfig>()
                : new List<TileConfig>(currentMap.tileList);
        }

        public List<TileConfig> GetNeighborTiles(string tileId)
        {
            List<TileConfig> neighbors = new List<TileConfig>();
            TileConfig tileConfig = GetTileConfig(tileId);
            if (tileConfig == null || tileConfig.nextTileIds == null)
            {
                return neighbors;
            }

            for (int i = 0; i < tileConfig.nextTileIds.Count; i++)
            {
                TileConfig neighbor = GetTileConfig(tileConfig.nextTileIds[i]);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        // 查询骰子和体力范围内的全部地块。
        public List<ReachableTileResult> GetReachableTiles(
            string startTileId,
            int maxSteps,
            int availableEnergy)
        {
            List<ReachableTileResult> results = new List<ReachableTileResult>();
            if (!ContainsTile(startTileId) || maxSteps < 1 || availableEnergy < 1)
            {
                return results;
            }

            int allowedSteps = Mathf.Min(maxSteps, availableEnergy);
            Queue<string> queue = new Queue<string>();
            Dictionary<string, int> distances = new Dictionary<string, int>();
            Dictionary<string, string> previousTiles = new Dictionary<string, string>();

            queue.Enqueue(startTileId);
            distances[startTileId] = 0;

            while (queue.Count > 0)
            {
                string currentTileId = queue.Dequeue();
                int currentDistance = distances[currentTileId];
                if (currentDistance >= allowedSteps)
                {
                    continue;
                }

                TileConfig currentTile = GetTileConfig(currentTileId);
                if (currentTile == null || currentTile.nextTileIds == null)
                {
                    continue;
                }

                for (int i = 0; i < currentTile.nextTileIds.Count; i++)
                {
                    string nextTileId = currentTile.nextTileIds[i];
                    if (!ContainsTile(nextTileId) || distances.ContainsKey(nextTileId))
                    {
                        continue;
                    }

                    distances[nextTileId] = currentDistance + 1;
                    previousTiles[nextTileId] = currentTileId;
                    queue.Enqueue(nextTileId);
                }
            }

            foreach (KeyValuePair<string, int> entry in distances)
            {
                if (entry.Key == startTileId || entry.Value < 1 || entry.Value > allowedSteps)
                {
                    continue;
                }

                results.Add(new ReachableTileResult(
                    entry.Key,
                    entry.Value,
                    BuildPath(startTileId, entry.Key, previousTiles)));
            }

            results.Sort((left, right) =>
            {
                int stepComparison = left.requiredSteps.CompareTo(right.requiredSteps);
                return stepComparison != 0
                    ? stepComparison
                    : string.CompareOrdinal(left.tileId, right.tileId);
            });

            return results;
        }

        public bool TryGetPath(
            string startTileId,
            string targetTileId,
            int maxSteps,
            out List<string> pathTileIds)
        {
            pathTileIds = new List<string>();
            List<ReachableTileResult> results =
                GetReachableTiles(startTileId, maxSteps, maxSteps);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].tileId == targetTileId)
                {
                    pathTileIds = new List<string>(results[i].pathTileIds);
                    return true;
                }
            }

            return false;
        }

        public bool IsMoveTargetValid(
            string startTileId,
            string targetTileId,
            int maxSteps,
            int availableEnergy,
            out int requiredSteps,
            out string reason)
        {
            requiredSteps = 0;
            reason = string.Empty;

            if (!ContainsTile(startTileId))
            {
                reason = "当前地块不存在：" + startTileId;
                return false;
            }

            if (!ContainsTile(targetTileId))
            {
                reason = "目标地块不存在：" + targetTileId;
                return false;
            }

            if (startTileId == targetTileId)
            {
                reason = "目标地块不能是当前位置。";
                return false;
            }

            List<ReachableTileResult> results =
                GetReachableTiles(startTileId, maxSteps, availableEnergy);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].tileId == targetTileId)
                {
                    requiredSteps = results[i].requiredSteps;
                    return true;
                }
            }

            reason = "目标地块超出骰子点数、体力或地图可达范围。";
            return false;
        }

        // 注册场景中的地块表现，用于高亮和取得移动位置。
        public bool RegisterTileView(TileView tileView)
        {
            if (tileView == null || string.IsNullOrEmpty(tileView.TileId))
            {
                return false;
            }

            if (!ContainsTile(tileView.TileId))
            {
                Debug.LogWarning("TileView未包含在当前地图中：" + tileView.TileId, tileView);
                return false;
            }

            tileViews[tileView.TileId] = tileView;
            return true;
        }

        public TileView GetTileView(string tileId)
        {
            tileViews.TryGetValue(tileId, out TileView tileView);
            return tileView;
        }

        public Vector3 GetTileWorldPosition(string tileId)
        {
            TileView tileView = GetTileView(tileId);
            if (tileView != null)
            {
                return tileView.StandPosition;
            }

            TileConfig tileConfig = GetTileConfig(tileId);
            return tileConfig == null ? Vector3.zero : tileConfig.position;
        }

        public void ClearTileHighlights()
        {
            foreach (TileView tileView in tileViews.Values)
            {
                if (tileView != null)
                {
                    tileView.SetReachable(false);
                    tileView.SetSelected(false);
                }
            }
        }

        private List<string> BuildPath(
            string startTileId,
            string targetTileId,
            Dictionary<string, string> previousTiles)
        {
            List<string> path = new List<string>();
            string currentTileId = targetTileId;
            path.Add(currentTileId);

            while (currentTileId != startTileId)
            {
                if (!previousTiles.TryGetValue(currentTileId, out string previousTileId))
                {
                    return new List<string>();
                }

                currentTileId = previousTileId;
                path.Add(currentTileId);
            }

            path.Reverse();
            return path;
        }

        private void WarnAboutMapConnections()
        {
            foreach (TileConfig tileConfig in tileConfigs.Values)
            {
                if (tileConfig.nextTileIds == null)
                {
                    continue;
                }

                for (int i = 0; i < tileConfig.nextTileIds.Count; i++)
                {
                    TileConfig neighbor = GetTileConfig(tileConfig.nextTileIds[i]);
                    if (neighbor == null ||
                        neighbor.nextTileIds == null ||
                        !neighbor.nextTileIds.Contains(tileConfig.tileId))
                    {
                        Debug.LogWarning(
                            "地块连接不是双向的：" + tileConfig.tileId + " -> " +
                            tileConfig.nextTileIds[i],
                            tileConfig);
                    }
                }
            }
        }
    }
}
