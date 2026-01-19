using UnityEngine;
using UnityEngine.UI;

public class WindowIcon : MonoBehaviour
{
    public WindowBase window;
    public Image iconImage;
    public Sprite activeSprite;
    public Sprite inactiveSprite;

    public void OnClick()
    {
        if (window.IsVisible())
            window.Hide();
        else
            window.Show();
    }

    public void SetActive(bool active)
    {
        iconImage.sprite = active ? activeSprite : inactiveSprite;
    }
}
