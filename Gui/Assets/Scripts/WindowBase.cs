using UnityEngine;
using UnityEngine.EventSystems;

public class WindowBase : MonoBehaviour, IPointerDownHandler
{
    public WindowIcon icon; // 底部对应图标
    public GameObject closeButton; // 关闭按钮

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        closeButton.SetActive(true); // 确保关闭按钮存在
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 点击时提到最前
        transform.SetAsLastSibling();
    }

    // 显示窗口
    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 显示时提升到最前
        if (icon != null) icon.SetActive(true); // 激活图标
    }

    // 隐藏窗口
    public void Hide()
    {
        gameObject.SetActive(false); // 隐藏窗口
        if (icon != null) icon.SetActive(false); // 将图标设置为不显示
    }

    // 隐藏窗口并改变图标状态
    public void HideAndDimIcon()
    {
        gameObject.SetActive(false); // 隐藏窗口
        if (icon != null) icon.SetActive(false); // 图标设置为灰色（不显示）
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }
}
