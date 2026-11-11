using UnityEngine;
namespace EyeCanDo
{
    public enum AvatarColorChannel { Skin, Hair, Shirt, Shorts }
    public sealed class AvatarTint : MonoBehaviour
    {
        public AvatarColorChannel channel;
        Renderer target; MaterialPropertyBlock block;
        public void Apply(Color color)
        {if(target==null)target=GetComponent<Renderer>();if(target==null)return;block??=new MaterialPropertyBlock();target.GetPropertyBlock(block);block.SetColor("_BaseColor",color);target.SetPropertyBlock(block);}
    }
}
