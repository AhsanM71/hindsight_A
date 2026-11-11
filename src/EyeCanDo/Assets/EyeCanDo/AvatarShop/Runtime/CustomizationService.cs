using System;
using System.Collections.Generic;
using System.Linq;

namespace EyeCanDo
{
    public enum CosmeticSlot { Face, Skin, Hair, HairColor, Outfit, Hat, Glasses, Backpack }
    public enum CosmeticCategory { Looks, Hair, Clothes, Hats, Extras }
    public enum CustomizationResult { Success, AlreadyOwned, AlreadyEquipped, InvalidItem, NotOwned, InsufficientCoins, InvalidAmount, SaveFailed }
    public interface ICosmeticRules
    {
        IEnumerable<string> Ids { get; }
        bool TryGetRule(string id, out CosmeticSlot slot, out int price, out bool free);
        string Default(CosmeticSlot slot);
    }
    // Store must either persist the complete record + grant marker, or leave its old values intact.
    public interface ICustomizationStore
    {
        bool HasRecord { get; }
        bool HasGrant { get; }
        string Read(bool backup);
        void Save(string completeRecord);
    }
    public interface ICustomizationCodec { string Encode(CustomizationState state); CustomizationState Decode(string json); }
    [Serializable] public sealed class CustomizationState
    {
        public int version;
        public int coins;
        public List<string> owned = new();
        public string[] equipped = new string[8];
        public CustomizationState Copy() => new CustomizationState {version=version,coins=coins,owned=new List<string>(owned),equipped=(string[])equipped.Clone()};
        public string Get(CosmeticSlot slot) => equipped[(int)slot];
        public void Set(CosmeticSlot slot,string id) => equipped[(int)slot]=id;
    }
    // No scene, animation, Unity object, or PlayerPrefs dependencies. All mutations pass through Commit.
    public sealed class CustomizationService
    {
        public const int Version=1, StartingCoins=200, MaximumCoins=1000000;
        readonly ICosmeticRules catalog; readonly ICustomizationStore store; readonly ICustomizationCodec codec;
        CustomizationState state;
        bool initialized;
        public event Action<CustomizationState> Changed;
        public CustomizationState Snapshot => state.Copy();
        public bool Ready => initialized;
        public CustomizationService(ICosmeticRules catalog,ICustomizationStore store,ICustomizationCodec codec)
        {
            this.catalog=catalog;this.store=store;this.codec=codec;
            state=Defaults(0);
            Initialize();
        }
        CustomizationState Defaults(int coins)
        {
            var s=new CustomizationState {version=Version,coins=coins};
            foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))s.Set(slot,catalog.Default(slot));
            return s;
        }
        bool Valid(CustomizationState s) => s!=null && s.version==Version && s.coins>=0 && s.coins<=MaximumCoins && s.owned!=null && s.equipped!=null && s.equipped.Length==8 && s.owned.All(id=>!string.IsNullOrWhiteSpace(id));
        CustomizationState Read(bool backup)
        {try{var s=codec.Decode(store.Read(backup));return Valid(s)?s:null;}catch{return null;}}
        public CustomizationResult Initialize()
        {
            if(initialized)return CustomizationResult.Success;
            try
            {
                var loaded=Read(false)??Read(true);
                var candidate=loaded??Defaults(!store.HasRecord&&!store.HasGrant?StartingCoins:0);
                candidate.owned=candidate.owned.Distinct(StringComparer.Ordinal).ToList(); // Unknown purchased IDs are deliberately retained.
                foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
                {
                    string id=candidate.Get(slot);
                    if(!catalog.TryGetRule(id,out var actual,out _,out var free)||actual!=slot||(!free&&!candidate.owned.Contains(id)))candidate.Set(slot,catalog.Default(slot));
                }
                return Commit(candidate);
            }
            catch{return CustomizationResult.SaveFailed;}
        }
        public bool IsOwned(string id) => catalog.TryGetRule(id,out _,out _,out var free)&&(free||state.owned.Contains(id));
        public CustomizationResult TryPurchaseAndEquip(string id)
        {
            if(!initialized)return CustomizationResult.SaveFailed;
            if(!catalog.TryGetRule(id,out var slot,out var price,out var free)||price<0)return CustomizationResult.InvalidItem;
            if(free||state.owned.Contains(id))return CustomizationResult.AlreadyOwned;
            if(price>state.coins)return CustomizationResult.InsufficientCoins;
            var next=state.Copy();next.coins-=price;next.owned.Add(id);next.Set(slot,id);return Commit(next);
        }
        public CustomizationResult TryEquip(string id)
        {
            if(!initialized)return CustomizationResult.SaveFailed;
            if(!catalog.TryGetRule(id,out var slot,out _,out _))return CustomizationResult.InvalidItem;
            if(!IsOwned(id))return CustomizationResult.NotOwned;
            if(state.Get(slot)==id)return CustomizationResult.AlreadyEquipped;
            var next=state.Copy();next.Set(slot,id);return Commit(next);
        }
        public CustomizationResult TryAddCoins(int amount)
        {
            if(!initialized)return CustomizationResult.SaveFailed;
            if(amount<=0||amount>MaximumCoins-state.coins)return CustomizationResult.InvalidAmount;
            var next=state.Copy();next.coins+=amount;return Commit(next);
        }
        CustomizationResult Commit(CustomizationState candidate)
        {
            try{store.Save(codec.Encode(candidate));}catch{return CustomizationResult.SaveFailed;}
            state=candidate.Copy();initialized=true;Changed?.Invoke(Snapshot);return CustomizationResult.Success;
        }
    }
}
