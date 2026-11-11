using UnityEngine;
namespace EyeCanDo
{
    public sealed class AdventureBackdrop : MonoBehaviour
    {
        public Camera targetCamera;
        float size, aspect;
        Vector3 position;
        Quaternion rotation;
        void LateUpdate()
        {
            if(targetCamera == null) targetCamera = Camera.main;
            if(targetCamera == null) return;
            if(size==targetCamera.orthographicSize && aspect==targetCamera.aspect && position==targetCamera.transform.position && rotation==targetCamera.transform.rotation) return;
            size=targetCamera.orthographicSize;aspect=targetCamera.aspect;position=targetCamera.transform.position;rotation=targetCamera.transform.rotation;
            transform.SetPositionAndRotation(position+targetCamera.transform.forward*75,rotation);
            transform.localScale=new Vector3(size*2*aspect*1.04f,size*2*1.04f,1);
        }
    }
}
