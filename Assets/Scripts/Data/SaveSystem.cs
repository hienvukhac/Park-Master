using UnityEngine;

namespace ParkMaster.Data
{

    public interface ISaveService
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);

        float GetFloat(string key, float defaultValue = 0f);
        void SetFloat(string key, float value);

        string GetString(string key, string defaultValue = "");
        void SetString(string key, string value);

        bool HasKey(string key);
        void DeleteKey(string key);
        void Save();
    }

    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }

    public static class SaveKeys
    {
        public const string CurrentLevel = "ParkMaster_CurrentLevel";
        public const string MaxUnlockedLevel = "ParkMaster_MaxUnlockedLevel";
        public const string LevelStarsPrefix = "ParkMaster_Stars_L";
        public const string LevelLengthPrefix = "ParkMaster_Len_L";
        public const string SelectedCarId = "SelectedCarID";
        public const string CarUnlockedPrefix = "Car_Unlocked";
        public const string DefaultTotalCoins = "PlayerCoins";

        public static string LevelStars(int levelIndex) => $"{LevelStarsPrefix}{levelIndex}";
        public static string LevelLength(int levelIndex) => $"{LevelLengthPrefix}{levelIndex}";
        public static string CarUnlocked(int carId) => $"{CarUnlockedPrefix}{carId}";
    }

    public static class SaveSystem
    {
        private static ISaveService current = new PlayerPrefsSaveService();

        public static ISaveService Current => current ??= new PlayerPrefsSaveService();

        public static void SetService(ISaveService service)
        {
            current = service ?? new PlayerPrefsSaveService();
        }

        public static void ResetToDefault()
        {
            current = new PlayerPrefsSaveService();
        }
    }
}
