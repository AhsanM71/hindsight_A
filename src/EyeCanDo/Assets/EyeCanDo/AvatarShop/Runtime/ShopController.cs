using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EyeCanDo
{
    [Serializable] public sealed class ShopCard
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image image,background;
        public TMP_Text title,price;
        public GameObject check;
    }
    public sealed class ShopController : MonoBehaviour
    {
        public CosmeticCatalog catalog;
        public AvatarView avatar;
        public UnityEngine.UI.Button back,action,previous,next,rotateLeft,rotateRight;
        public UnityEngine.UI.Button[] categories,swatches;
        public ShopCard[] cards;
        public TMP_Text coins,actionLabel,selectionLabel,hint,pageLabel,swatchLabel;
        public Transform sparkle;
        public GameObject actionCheck;
        CustomizationService service;
        CosmeticDefinition selected;
        CosmeticCategory category;
        int page;float rotation,celebration=-10;
        public string PreviewId=>selected?.id;
        public CosmeticCategory Category=>category;
        public CustomizationState PreviewState {get;private set;}
        void Start()
        {
            service=AppRoot.Instance.Customization;service.Changed+=Committed;
            back.onClick.AddListener(Back);action.onClick.AddListener(Commit);
            previous.onClick.AddListener(()=>{page--;ClearPreview();Refresh();});next.onClick.AddListener(()=>{page++;ClearPreview();Refresh();});
            rotateLeft.onClick.AddListener(()=>Rotate(-45));rotateRight.onClick.AddListener(()=>Rotate(45));
            for(int i=0;i<categories.Length;i++){int k=i;categories[i].onClick.AddListener(()=>SetCategory((CosmeticCategory)k));}
            for(int i=0;i<cards.Length;i++){int k=i;cards[i].button.onClick.AddListener(()=>Select(Choices().Skip(page*6).ElementAtOrDefault(k)));}
            for(int i=0;i<swatches.Length;i++){int k=i;swatches[i].onClick.AddListener(()=>Select(Colors().ElementAtOrDefault(k)));}
            SetCategory(CosmeticCategory.Looks);
        }
        void OnDestroy(){if(service!=null)service.Changed-=Committed;}
        void Committed(CustomizationState state){selected=null;PreviewState=state.Copy();avatar.Apply(state);Refresh();}
        CosmeticDefinition[] Choices()=>catalog.items.Where(i=>i.category==category&&i.slot!=CosmeticSlot.Skin&&i.slot!=CosmeticSlot.HairColor).ToArray();
        CosmeticDefinition[] Colors()=>catalog.items.Where(i=>category==CosmeticCategory.Looks?i.slot==CosmeticSlot.Skin:category==CosmeticCategory.Hair&&i.slot==CosmeticSlot.HairColor).ToArray();
        public void SetCategory(CosmeticCategory value){category=value;page=0;rotation=0;ClearPreview();Refresh();}
        void ClearPreview(){selected=null;PreviewState=service.Snapshot;avatar.Apply(PreviewState);avatar.transform.localRotation=Quaternion.Euler(0,rotation,0);}
        public void Select(CosmeticDefinition definition)
        {
            if(definition==null)return;selected=definition;PreviewState=service.Snapshot;PreviewState.Set(definition.slot,definition.id);avatar.Apply(PreviewState);
            if(definition.slot==CosmeticSlot.Backpack)rotation=135;
            else rotation=0;
            avatar.transform.localRotation=Quaternion.Euler(0,rotation,0);Refresh();
        }
        public void Rotate(float degrees){rotation=(rotation+degrees)%360;avatar.transform.localRotation=Quaternion.Euler(0,rotation,0);}
        public void Back(){selected=null;AppRoot.Instance.Navigation.GoToMap();}
        public void Commit()
        {
            if(!service.Ready){if(service.Initialize()!=CustomizationResult.Success){hint.text="Couldn't save. Tap Retry.";return;}Refresh();return;}
            if(selected==null)return;
            bool buying=!service.IsOwned(selected.id);var id=selected.id;
            var result=buying?service.TryPurchaseAndEquip(id):service.TryEquip(id);
            if(result==CustomizationResult.SaveFailed){hint.text="Couldn't save. Please try again.";actionLabel.text="Retry";return;}
            if(result==CustomizationResult.Success){Select(catalog.Find(id));hint.text=buying?"Yay! It's yours!":"Looking good!";if(buying){celebration=Time.unscaledTime;avatar.Celebrate();}}
            else Refresh();
        }
        void Refresh()
        {
            if(service==null)return;var state=service.Snapshot;coins.text=state.coins.ToString();var choices=Choices();int pages=Mathf.Max(1,(choices.Length+5)/6);page=Mathf.Clamp(page,0,pages-1);
            previous.interactable=page>0;next.interactable=page+1<pages;pageLabel.text=$"{page+1} / {pages}";
            for(int i=0;i<categories.Length;i++)categories[i].GetComponent<UnityEngine.UI.Image>().color=(int)category==i?new Color(.78f,.93f,.91f):Color.white;
            for(int i=0;i<cards.Length;i++)
            {
                var d=choices.Skip(page*6+i).FirstOrDefault();var c=cards[i];c.button.gameObject.SetActive(d!=null);if(d==null)continue;
                bool equipped=state.Get(d.slot)==d.id;c.image.sprite=d.thumbnail;c.title.text=d.label;c.check.SetActive(equipped);c.price.text=equipped?"Equipped":service.IsOwned(d.id)?(d.defaultOwnership?"Free":"Owned"):"●  "+d.price;
                c.background.color=selected?.id==d.id?new Color(.78f,.93f,.91f):Color.white;
            }
            var colors=Colors();swatchLabel.gameObject.SetActive(colors.Length>0);swatchLabel.text=category==CosmeticCategory.Looks?"Skin tone":"Hair colour";
            for(int i=0;i<swatches.Length;i++)
            {
                var d=colors.ElementAtOrDefault(i);swatches[i].gameObject.SetActive(d!=null);if(d==null)continue;
                swatches[i].GetComponent<UnityEngine.UI.Image>().color=selected?.id==d.id?new Color(.38f,.78f,.72f):Color.white;
                swatches[i].transform.Find("Colour").GetComponent<UnityEngine.UI.Image>().color=d.color;
                swatches[i].transform.Find("Check").gameObject.SetActive(state.Get(d.slot)==d.id);
            }
            selectionLabel.text=selected?.label??"Make it yours";hint.text=selected==null?"Pick a look to try it on":"Preview • not saved yet";
            action.interactable=selected!=null;actionCheck.SetActive(false);
            if(!service.Ready){actionLabel.text="Retry";action.interactable=true;hint.text="Couldn't load your looks. Tap Retry.";return;}
            if(selected==null){actionLabel.text="Choose an item";return;}
            if(state.Get(selected.slot)==selected.id){actionLabel.text="Equipped";actionCheck.SetActive(true);hint.text="This is your saved look";action.interactable=false;}
            else if(service.IsOwned(selected.id))actionLabel.text="Equip";
            else if(state.coins>=selected.price)actionLabel.text="Buy   ● "+selected.price;
            else{actionLabel.text="Buy   ● "+selected.price;action.interactable=false;hint.text="●  "+(selected.price-state.coins)+" more coins needed";}
        }
        void Update()
        {
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)Back();
            float t=Time.unscaledTime-celebration;if(sparkle!=null){sparkle.gameObject.SetActive(t<1.5f);if(t<1.5f){sparkle.localRotation=Quaternion.Euler(0,0,t*35);sparkle.localScale=Vector3.one*(1+t*.15f);}}
        }
    }
}
