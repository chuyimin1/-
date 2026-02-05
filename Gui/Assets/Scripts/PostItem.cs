using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostItem : MonoBehaviour
{
    public Image avatar;
    public TMP_Text nickname;
    public TMP_Text title;
    public TMP_Text content;
    public Image postImage;

    public void Setup(SocialPostData data)
    {
        avatar.sprite = data.avatar;
        nickname.text = data.nickname;
        title.text = data.title;
        content.text = data.content;

        // 如果没有图片，隐藏图片组件以节省空间
        if (data.postImage != null)
        {
            postImage.gameObject.SetActive(true);
            postImage.sprite = data.postImage;
        }
        else
        {
            postImage.gameObject.SetActive(false);
        }
    }
}