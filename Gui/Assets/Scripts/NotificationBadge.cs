using UnityEngine;

public class NotificationBadge : MonoBehaviour
{
    public static NotificationBadge Instance;

    void Awake()
    {
        // 1. 确立单例身份
        if (Instance == null)
        {
            Instance = this;
            // 确保切换场景时不被销毁（可选，看你项目需求）
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. 关键：在赋值完成后，立刻把自己藏起来
        // 这样既保证了 Awake 被调用，又保证了玩家一进来拍不到红点
        gameObject.SetActive(false);
    }

    public void ShowBadge()
    {
        gameObject.SetActive(true);
    }

    public void HideBadge()
    {
        gameObject.SetActive(false);
    }
}