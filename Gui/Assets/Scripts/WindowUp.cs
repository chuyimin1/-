using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowFocusManager : MonoBehaviour, IPointerDownHandler
{
    // 每一个窗口根物体都需要有一个 Graphic 组件（如 Image）才能接收点击
    private Image bgImage;

    void Awake()
    {
        bgImage = GetComponent<Image>();
        // 即使是完全透明的背景，也要开启 Raycast Target，否则点不到窗口
        if (bgImage != null) bgImage.raycastTarget = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 核心：点击窗口任何地方，将其移到 Hierarchy 同级最下方（即显示最前方）
        transform.SetAsLastSibling();
    }
}
