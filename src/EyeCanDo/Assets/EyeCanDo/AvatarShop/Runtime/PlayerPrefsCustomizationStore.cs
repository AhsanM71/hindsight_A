using System;
using UnityEngine;

namespace EyeCanDo
{
    public sealed class UnityCustomizationCodec : ICustomizationCodec
    {
        public string Encode(CustomizationState state)=>JsonUtility.ToJson(state);
        public CustomizationState Decode(string json)=>string.IsNullOrWhiteSpace(json)?null:JsonUtility.FromJson<CustomizationState>(json);
    }
    public sealed class PlayerPrefsCustomizationStore : ICustomizationStore
    {
        public const string StateKey="EyeCanDo.Customization.State", BackupKey="EyeCanDo.Customization.Backup", GrantKey="EyeCanDo.Customization.StartingGrant";
        public static readonly string[] Keys={StateKey,BackupKey,GrantKey};
        public bool HasRecord=>PlayerPrefs.HasKey(StateKey)||PlayerPrefs.HasKey(BackupKey);
        public bool HasGrant=>PlayerPrefs.HasKey(GrantKey);
        public string Read(bool backup)=>PlayerPrefs.GetString(backup?BackupKey:StateKey,string.Empty);
        public void Save(string completeRecord)
        {
            var existed=new bool[3];var previous=new string[3];
            for(int i=0;i<3;i++){existed[i]=PlayerPrefs.HasKey(Keys[i]);previous[i]=PlayerPrefs.GetString(Keys[i]);}
            try
            {
                PlayerPrefs.SetString(StateKey,completeRecord);
                // The backup is the latest complete successful state, including the balance.
                PlayerPrefs.SetString(BackupKey,completeRecord);
                PlayerPrefs.SetString(GrantKey,"1");PlayerPrefs.Save();
            }
            catch
            {
                for(int i=0;i<3;i++){if(existed[i])PlayerPrefs.SetString(Keys[i],previous[i]);else PlayerPrefs.DeleteKey(Keys[i]);}
                try{PlayerPrefs.Save();}catch{/* Retain the prior in-memory record; surface failure to the caller. */}
                throw;
            }
        }
    }
}
