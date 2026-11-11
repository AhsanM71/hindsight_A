using UnityEngine;
namespace EyeCanDo
{
    [ExecuteAlways] public sealed class ShopSafeLayout : MonoBehaviour
    {
        void LateUpdate()
        {
            var parent=transform.parent as RectTransform;if(parent==null)return;
            float scale=Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f);transform.localScale=Vector3.one*scale;
        }
    }
}
