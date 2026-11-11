using System.Collections.Generic;
using UnityEngine;
namespace EyeCanDo
{
    public sealed class AvatarView : MonoBehaviour
    {
        public CosmeticCatalog catalog;
        public Transform body,headAttachment,faceAttachment,hairAttachment,hatAttachment,glassesAttachment,backAttachment,leftHandAttachment,rightHandAttachment,rightArm,leftArm;
        public bool committed=true,waveOnEntry;
        readonly Dictionary<CosmeticSlot,GameObject> visuals=new();
        Transform[] eyes=System.Array.Empty<Transform>();
        CustomizationService service;float gesture=-10;Vector3 bodyRest;
        void Awake(){bodyRest=body.localPosition;}
        void Start()
        {
            if(committed&&AppRoot.Instance!=null){service=AppRoot.Instance.Customization;service.Changed+=Apply;Apply(service.Snapshot);}
            if(waveOnEntry)Celebrate();
        }
        void OnDestroy(){if(service!=null)service.Changed-=Apply;}
        public void Celebrate()=>gesture=Time.unscaledTime;
        Transform Mount(CosmeticSlot slot)=>slot switch {CosmeticSlot.Face=>faceAttachment,CosmeticSlot.Hair=>hairAttachment,CosmeticSlot.Hat=>hatAttachment,CosmeticSlot.Glasses=>glassesAttachment,CosmeticSlot.Backpack=>backAttachment,_=>null};
        public void Apply(CustomizationState state)
        {
            foreach(var slot in new[]{CosmeticSlot.Face,CosmeticSlot.Hair,CosmeticSlot.Hat,CosmeticSlot.Glasses,CosmeticSlot.Backpack})
            {
                if(visuals.TryGetValue(slot,out var old)&&old!=null){old.SetActive(false);if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);}
                var def=catalog.Find(state.Get(slot));var mount=Mount(slot);
                // Authoring previews may already have children; clear before replacing.
                foreach(Transform child in mount){if(!child.gameObject.activeSelf)continue;child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
                if(def?.visual!=null){var instance=Instantiate(def.visual,mount,false);instance.name=def.id;visuals[slot]=instance;}
                else visuals.Remove(slot);
            }
            bool hat=catalog.Find(state.Get(CosmeticSlot.Hat))?.visual!=null;
            if(visuals.TryGetValue(CosmeticSlot.Hair,out var hair)){var crown=hair.transform.Find("Crown");if(crown!=null)crown.gameObject.SetActive(!hat);}
            var skin=catalog.Find(state.Get(CosmeticSlot.Skin));var color=catalog.Find(state.Get(CosmeticSlot.HairColor));var outfit=catalog.Find(state.Get(CosmeticSlot.Outfit));
            foreach(var tint in GetComponentsInChildren<AvatarTint>(true))tint.Apply(tint.channel switch {AvatarColorChannel.Skin=>skin.color,AvatarColorChannel.Hair=>color.color,AvatarColorChannel.Shirt=>outfit.color,_=>outfit.secondary});
            var found=new List<Transform>();foreach(var t in faceAttachment.GetComponentsInChildren<Transform>())if(t.name=="Blink eye")found.Add(t);eyes=found.ToArray();
        }
        void Update()
        {
            float t=Time.unscaledTime;body.localPosition=bodyRest+Vector3.up*(Mathf.Sin(t*1.8f)*.024f);
            headAttachment.localRotation=Quaternion.Euler(0,Mathf.Sin(t*.7f)*3,Mathf.Sin(t*.9f)*1.5f);
            float elapsed=t-gesture;
            float envelope=Mathf.Clamp01(elapsed/.28f)*Mathf.Clamp01((2.2f-elapsed)/.35f);
            if(elapsed<0||elapsed>2.2f)envelope=0;
            rightArm.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(8,145+Mathf.Sin(elapsed*14)*13,envelope));
            leftArm.localRotation=Quaternion.Euler(0,0,-8+Mathf.Sin(t)*2);
            float blink=t%4.6f;float scale=blink<.16f?Mathf.Max(.07f,Mathf.Abs(blink-.08f)/.08f):1;
            foreach(var eye in eyes)if(eye!=null)eye.localScale=new Vector3(1,scale,1);
        }
    }
}
