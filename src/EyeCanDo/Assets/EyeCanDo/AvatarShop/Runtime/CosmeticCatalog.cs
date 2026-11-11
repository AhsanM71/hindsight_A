using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EyeCanDo
{
    [Serializable] public sealed class CosmeticDefinition
    {
        public string id,label;
        public CosmeticCategory category;
        public CosmeticSlot slot;
        public int price;
        public bool defaultOwnership;
        public Sprite thumbnail;
        public GameObject visual;
        public Color color=Color.white,secondary=Color.white;
    }
    [CreateAssetMenu(menuName="EyeCanDo/Cosmetic catalog")]
    public sealed class CosmeticCatalog : ScriptableObject,ICosmeticRules
    {
        public CosmeticDefinition[] items=Array.Empty<CosmeticDefinition>();
        public string[] defaults={"face.round","skin.3","hair.curls","color.0","outfit.0","hat.none","glasses.none","backpack.none"};
        public IEnumerable<string> Ids=>items.Select(i=>i.id);
        public CosmeticDefinition Find(string id)=>items.FirstOrDefault(i=>i.id==id);
        public string Default(CosmeticSlot slot)=>defaults[(int)slot];
        public bool TryGetRule(string id,out CosmeticSlot slot,out int price,out bool free)
        {var i=Find(id);slot=i?.slot??default;price=i?.price??0;free=i!=null&&i.defaultOwnership;return i!=null;}
    }
}
