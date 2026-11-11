using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace EyeCanDo.Editor
{
    public static class CustomizationChecks
    {
        sealed class Store : ICustomizationStore
        {
            public string primary,backup;public bool grant,fail;public int writes;
            public bool HasRecord=>primary!=null||backup!=null;public bool HasGrant=>grant;
            public string Read(bool b)=>b?backup:primary;
            public void Save(string record){if(fail)throw new IOException("Injected failure");primary=backup=record;grant=true;writes++;}
        }
        [MenuItem("EyeCanDo/Character & Shop/Run service checks")]
        public static void Run()
        {
            var passed=new List<string>();void Check(bool ok,string message){if(!ok)throw new Exception(message);passed.Add(message);}
            var catalog=AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CharacterArt.Root+"/Catalog/Cosmetics.asset");var codec=new UnityCustomizationCodec();
            CustomizationService Open(Store store)=>new(catalog,store,codec);
            var store=new Store();var service=Open(store);Check(service.Ready&&service.Snapshot.coins==200&&store.grant,"First initialization saves exactly 200 and marker");
            Check(Open(store).Snapshot.coins==200,"Repeated initialization does not grant again");
            int events=0;service.Changed+=_=>events++;
            Check(service.TryEquip("face.oval")==CustomizationResult.Success,"Free face equips");Check(service.Snapshot.Get(CosmeticSlot.Skin)=="skin.3"&&service.Snapshot.Get(CosmeticSlot.Hair)=="hair.curls","Face preserves skin and hair");
            Check(service.TryPurchaseAndEquip("hat.cap")==CustomizationResult.Success&&service.Snapshot.coins==160&&service.Snapshot.Get(CosmeticSlot.Hat)=="hat.cap","Cap purchase deducts 40 and equips atomically");
            int savedEvents=events;Check(service.TryPurchaseAndEquip("hat.cap")==CustomizationResult.AlreadyOwned&&service.Snapshot.coins==160&&events==savedEvents,"Repeated purchase/tap cannot deduct again or publish another event");
            Check(service.TryEquip("hat.none")==CustomizationResult.Success&&service.Snapshot.coins==160,"Unequip is free");Check(service.TryEquip("hat.cap")==CustomizationResult.Success&&service.Snapshot.coins==160,"Owned re-equip is free");
            Check(service.TryEquip("hat.star")==CustomizationResult.NotOwned,"Unowned equip rejected");
            Check(service.TryPurchaseAndEquip("glasses.fun")==CustomizationResult.Success&&service.Snapshot.coins==110,"Glasses deduct 50");
            Check(service.TryPurchaseAndEquip("backpack.small")==CustomizationResult.Success&&service.Snapshot.coins==30,"Backpack deducts 80");
            Check(service.Snapshot.Get(CosmeticSlot.Hat)=="hat.cap"&&service.Snapshot.Get(CosmeticSlot.Glasses)=="glasses.fun"&&service.Snapshot.Get(CosmeticSlot.Backpack)=="backpack.small","Hat, glasses and backpack coexist");
            Check(service.TryPurchaseAndEquip("hat.star")==CustomizationResult.InsufficientCoins&&service.Snapshot.coins==30,"Insufficient funds cannot change balance");
            Check(service.TryPurchaseAndEquip("unknown")==CustomizationResult.InvalidItem&&service.TryEquip(null)==CustomizationResult.InvalidItem,"Invalid IDs rejected");
            Check(service.TryAddCoins(-1)==CustomizationResult.InvalidAmount&&service.TryAddCoins(0)==CustomizationResult.InvalidAmount&&service.TryAddCoins(int.MaxValue)==CustomizationResult.InvalidAmount,"Negative, zero, and overflow additions rejected");
            Check(service.TryAddCoins(30)==CustomizationResult.Success&&service.TryPurchaseAndEquip("hat.star")==CustomizationResult.Success&&service.Snapshot.coins==0,"Exact balance purchase succeeds");
            Check(Open(store).Snapshot.coins==0,"Restart does not replenish spent coins");
            var copy=service.Snapshot;copy.coins=1000;copy.owned.Clear();copy.Set(CosmeticSlot.Hat,"hat.none");Check(service.Snapshot.coins==0&&service.IsOwned("hat.star"),"Snapshots cannot mutate committed state");
            savedEvents=events;string old=store.primary;store.fail=true;Check(service.TryEquip("face.round")==CustomizationResult.SaveFailed&&store.primary==old&&events==savedEvents&&service.Snapshot.Get(CosmeticSlot.Face)=="face.oval","Failed write retains committed state and emits no event");store.fail=false;Check(service.TryEquip("face.round")==CustomizationResult.Success,"Failed write can be retried");
            var failedInit=new Store {fail=true};var pending=Open(failedInit);Check(!pending.Ready&&pending.Snapshot.coins==0,"Failed initialization does not publish unsaved grant");failedInit.fail=false;Check(pending.Initialize()==CustomizationResult.Success&&pending.Snapshot.coins==200,"Initialization retry grants once");
            store.primary="malformed";var recovered=Open(store);Check(recovered.Snapshot.coins==0&&recovered.IsOwned("hat.star"),"Malformed primary recovers latest complete backup");
            store.primary="{";store.backup="{}";Check(Open(store).Snapshot.coins==0,"Unrecoverable records fall back to free defaults and zero coins");
            var markerOnly=new Store {grant=true};Check(Open(markerOnly).Snapshot.coins==0,"Missing record with grant marker cannot grant again");
            var malformedUnmarked=new Store {primary="{}"};Check(Open(malformedUnmarked).Snapshot.coins==0,"Malformed unmarked record cannot grant again");
            var unknownState=service.Snapshot;unknownState.owned.Add("retired.item");unknownState.Set(CosmeticSlot.Hat,"retired.item");var missing=new Store {primary=codec.Encode(unknownState),grant=true};var repaired=Open(missing);Check(repaired.Snapshot.owned.Contains("retired.item")&&repaired.Snapshot.Get(CosmeticSlot.Hat)=="hat.none","Unknown purchased IDs retained; unavailable equipment uses free default");
            unknownState.coins=-1;Check(Open(new Store {primary=codec.Encode(unknownState)}).Snapshot.coins==0,"Negative saved currency rejected");unknownState.coins=int.MaxValue;Check(Open(new Store {primary=codec.Encode(unknownState)}).Snapshot.coins==0,"Overflow saved currency rejected");
            var capped=Open(new Store());Check(capped.TryAddCoins(CustomizationService.MaximumCoins-200)==CustomizationResult.Success&&capped.TryAddCoins(1)==CustomizationResult.InvalidAmount,"Currency cap cannot overflow");
            var saved=capped.Snapshot;saved.version=99;Check(Open(new Store {primary=codec.Encode(saved)}).Snapshot.coins==0,"Unsupported record version fails closed");
            File.WriteAllText("Validation/CharacterShop/service-checks.json",JsonUtility.ToJson(new Report {status="passed",checks=passed.ToArray()},true));
            Debug.Log("Character Shop service checks passed: "+passed.Count);
        }
        [Serializable] class Report {public string status;public string[] checks;}
    }
}
