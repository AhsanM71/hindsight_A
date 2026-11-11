using UnityEngine;

namespace EyeCanDo
{
    // Camera-aligned scenic layers cover an orthographic viewport without participating in its fit.
    [ExecuteAlways]
    public sealed class GardenBackdrop : MonoBehaviour
    {
        public Camera mapCamera;
        void LateUpdate() => Fit();
        public void Fit()
        {
            if(mapCamera==null)return;
            transform.SetPositionAndRotation(mapCamera.transform.position+mapCamera.transform.forward*75,mapCamera.transform.rotation);
            transform.localScale=new Vector3(mapCamera.orthographicSize*2*mapCamera.aspect*1.04f,mapCamera.orthographicSize*2*1.04f,1);
        }
    }
}
