using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CampusNightMarket.Map
{
    // 玩家相机控制器：以棋子为中心跟随，并通过鼠标滚轮平滑缩放。
    [RequireComponent(typeof(Camera))]
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("跟随目标")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);

        [Header("缩放设置")]
        [SerializeField, Range(0.1f, 1f)] private float minimumZoomRatio = 0.40f;
        [SerializeField, Range(1f, 3f)] private float maximumZoomRatio = 1.25f;
        [SerializeField, Min(0.01f)] private float scrollSensitivity = 0.12f;
        [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.12f;

        [Header("跟随设置")]
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.08f;
        [SerializeField] private bool ignoreScrollWhenPointerOverScrollableUI = true;

        [Header("构图设置")]
        [SerializeField] private bool alignViewToNearestWorldAxis = true;

        // 初始镜头到棋子的方向和距离，用作缩放基准。
        private Vector3 baseViewDirection;
        private float baseDistance;
        private float currentZoomRatio = 1f;
        private float targetZoomRatio = 1f;
        private float zoomVelocity;
        private Vector3 followVelocity;
        private bool isInitialized;

        // 复用UI射线检测结果，避免滚动时反复创建列表。
        private readonly List<RaycastResult> uiRaycastResults =
            new List<RaycastResult>();

        private void Update()
        {
            if (!isInitialized || target == null)
            {
                return;
            }

            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) < 0.01f)
            {
                return;
            }

            if (ShouldIgnoreScrollInput())
            {
                return;
            }

            // 滚轮向上拉近，向下拉远，并限制在安全比例范围内。
            targetZoomRatio = Mathf.Clamp(
                targetZoomRatio - scrollDelta * scrollSensitivity,
                minimumZoomRatio,
                maximumZoomRatio);
        }

        private void LateUpdate()
        {
            if (!isInitialized || target == null)
            {
                return;
            }

            currentZoomRatio = Mathf.SmoothDamp(
                currentZoomRatio,
                targetZoomRatio,
                ref zoomVelocity,
                zoomSmoothTime);

            Vector3 focusPoint = GetFocusPoint();
            Vector3 desiredPosition =
                focusPoint + baseViewDirection * baseDistance * currentZoomRatio;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                followSmoothTime);

            // 相机始终朝向棋子，使缩放和移动时棋子保持在画面中心。
            Vector3 lookDirection = focusPoint - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        // 设置相机跟随的棋子，并以当前视角作为100%缩放基准。
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            InitializeFromCurrentView();
        }

        // 恢复到场景原本的镜头距离。
        public void ResetZoom()
        {
            targetZoomRatio = 1f;
        }

        private void InitializeFromCurrentView()
        {
            isInitialized = false;
            if (target == null)
            {
                return;
            }

            Vector3 focusPoint = GetFocusPoint();
            Vector3 cameraOffset = transform.position - focusPoint;
            if (cameraOffset.sqrMagnitude < 0.0001f)
            {
                cameraOffset = -transform.forward * 10f;
            }

            // 消除原场景相机的斜向偏移，使地图道路和地块与画面边缘对齐。
            if (alignViewToNearestWorldAxis)
            {
                cameraOffset = AlignOffsetToNearestWorldAxis(cameraOffset);
                transform.position = focusPoint + cameraOffset;
            }

            baseDistance = cameraOffset.magnitude;
            baseViewDirection = cameraOffset / baseDistance;
            currentZoomRatio = 1f;
            targetZoomRatio = 1f;
            zoomVelocity = 0f;
            followVelocity = Vector3.zero;
            isInitialized = true;

            Vector3 lookDirection = focusPoint - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private Vector3 GetFocusPoint()
        {
            return target.position + targetOffset;
        }

        private Vector3 AlignOffsetToNearestWorldAxis(Vector3 cameraOffset)
        {
            float horizontalDistance = new Vector2(cameraOffset.x, cameraOffset.z).magnitude;
            if (horizontalDistance < 0.0001f)
            {
                return cameraOffset;
            }

            // 保留原有俯视高度和水平距离，只移除较小的横向分量。
            if (Mathf.Abs(cameraOffset.x) > Mathf.Abs(cameraOffset.z))
            {
                return new Vector3(
                    Mathf.Sign(cameraOffset.x) * horizontalDistance,
                    cameraOffset.y,
                    0f);
            }

            return new Vector3(
                0f,
                cameraOffset.y,
                Mathf.Sign(cameraOffset.z) * horizontalDistance);
        }

        private bool ShouldIgnoreScrollInput()
        {
            if (!ignoreScrollWhenPointerOverScrollableUI || EventSystem.current == null)
            {
                return false;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                Transform hoveredTransform = uiRaycastResults[i].gameObject.transform;
                if (hoveredTransform.GetComponentInParent<ScrollRect>() != null ||
                    hoveredTransform.GetComponentInParent<Scrollbar>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            minimumZoomRatio = Mathf.Clamp(minimumZoomRatio, 0.1f, 1f);
            maximumZoomRatio = Mathf.Max(1f, maximumZoomRatio);
            if (maximumZoomRatio < minimumZoomRatio)
            {
                maximumZoomRatio = minimumZoomRatio;
            }

            scrollSensitivity = Mathf.Max(0.01f, scrollSensitivity);
            zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
            followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
        }
    }
}
