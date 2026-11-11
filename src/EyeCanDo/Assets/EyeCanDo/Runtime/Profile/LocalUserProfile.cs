using System;
using UnityEngine;

namespace EyeCanDo
{
    public interface IProfileStore
    {
        string Read();
        void Write(string username);
    }

    public sealed class PlayerPrefsProfileStore : IProfileStore
    {
        public const string UsernameKey = "EyeCanDo.Profile.Username";
        public string Read() => PlayerPrefs.GetString(UsernameKey, string.Empty);
        public void Write(string username)
        {
            PlayerPrefs.SetString(UsernameKey, username);
            PlayerPrefs.Save();
        }
    }

    public sealed class LocalUserProfile
    {
        public const int MaximumLength = 20;
        readonly IProfileStore store;
        public string Username { get; private set; }
        public bool HasUsername => !string.IsNullOrEmpty(Username);
        public event Action<string> Changed;

        public LocalUserProfile(IProfileStore store)
        {
            this.store = store;
            Username = TryValidate(store.Read(), out var value, out _) ? value : string.Empty;
        }

        public static bool TryValidate(string raw, out string value, out string error)
        {
            value = (raw ?? string.Empty).Trim();
            error = string.Empty;
            if (value.Length == 0) error = "Please enter a name to continue.";
            else if (value.Length > MaximumLength) error = "Please use 20 characters or fewer.";
            else if (value.IndexOfAny(new[] { '\n', '\r', '\t' }) >= 0) error = "Please use a name on one line.";
            return error.Length == 0;
        }

        public bool TrySaveUsername(string raw, out string error)
        {
            if (!TryValidate(raw, out var value, out error)) return false;
            store.Write(value);
            Username = value;
            Changed?.Invoke(value);
            return true;
        }
    }
}
