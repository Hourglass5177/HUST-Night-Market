using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNightMarket.Map
{
    // 玩家移动表现：按照地图返回的地块路径逐格移动。
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float arriveDistance = 0.02f;
        [SerializeField] private float heightOffset = 0.75f;

        private Coroutine moveCoroutine;

        public bool IsMoving
        {
            get { return moveCoroutine != null; }
        }

        public event Action MovementFinished;

        public void PlaceAt(MapManager mapManager, string tileId)
        {
            if (mapManager == null || string.IsNullOrEmpty(tileId))
            {
                return;
            }

            transform.position = GetPlayerPosition(mapManager.GetTileWorldPosition(tileId));
        }

        public bool MoveAlongPath(
            MapManager mapManager,
            List<string> pathTileIds,
            Action onFinished = null)
        {
            if (mapManager == null || pathTileIds == null || pathTileIds.Count < 2 || IsMoving)
            {
                return false;
            }

            moveCoroutine = StartCoroutine(MoveRoutine(mapManager, pathTileIds, onFinished));
            return true;
        }

        private IEnumerator MoveRoutine(
            MapManager mapManager,
            List<string> pathTileIds,
            Action onFinished)
        {
            for (int i = 1; i < pathTileIds.Count; i++)
            {
                Vector3 targetPosition =
                    GetPlayerPosition(mapManager.GetTileWorldPosition(pathTileIds[i]));

                while ((transform.position - targetPosition).sqrMagnitude >
                       arriveDistance * arriveDistance)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPosition;
            }

            moveCoroutine = null;
            onFinished?.Invoke();
            MovementFinished?.Invoke();
        }

        private Vector3 GetPlayerPosition(Vector3 tilePosition)
        {
            return tilePosition + Vector3.up * heightOffset;
        }
    }
}
