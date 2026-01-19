using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    private RectTransform windowTransform; // 整个窗口
    private RectTransform canvasTransform; // 画布
    private Vector2 dragOffset;

    void Awake()
    {
        // 脚本挂在 Header 上，移动的是父级（整个窗口）
        windowTransform = transform.parent.GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasTransform = canvas.transform as RectTransform;
        }
    }

    // 功能一：点击瞬间置顶
    public void OnPointerDown(PointerEventData eventData)
    {
        windowTransform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 开始拖拽也置顶
        windowTransform.SetAsLastSibling();

        // 计算鼠标相对窗口位置的偏移
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePos
        );
        dragOffset = windowTransform.anchoredPosition - mousePos;
    }

    // 功能二：拖拽并限制边界
    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePos))
        {
            Vector2 targetPos = mousePos + dragOffset;

            // --- 边界限制逻辑 ---
            // 1. 计算窗口在当前缩放下的宽高
            float winW = windowTransform.rect.width;
            float winH = windowTransform.rect.height;

            // 2. 计算 Canvas 的宽高
            float canvasW = canvasTransform.rect.width;
            float canvasH = canvasTransform.rect.height;

            // 3. 假设窗口锚点在中心 (0.5, 0.5)
            // 计算 X 和 Y 的活动范围 (Canvas中心为0,0)
            float minX = -canvasW / 2f + winW / 2f;
            float maxX = canvasW / 2f - winW / 2f;
            float minY = -canvasH / 2f + winH / 2f;
            float maxY = canvasH / 2f - winH / 2f;

            // 应用限制
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

            windowTransform.anchoredPosition = targetPos;
        }
    }
}