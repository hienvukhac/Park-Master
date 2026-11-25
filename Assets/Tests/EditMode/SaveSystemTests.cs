using System.Collections.Generic;
using NUnit.Framework;
using ParkMaster.Data;

namespace ParkMaster.Tests
{
    public class SaveSystemTests
    {
        private sealed class InMemorySaveService : ISaveService
        {
            private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
            private readonly Dictionary<string, float> floats = new Dictionary<string, float>();
            private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
            public int SaveCallCount { get; private set; }

            public int GetInt(string key, int defaultValue = 0) => ints.TryGetValue(key, out int v) ? v : defaultValue;
            public void SetInt(string key, int value) => ints[key] = value;

            public float GetFloat(string key, float defaultValue = 0f) => floats.TryGetValue(key, out float v) ? v : defaultValue;
            public void SetFloat(string key, float value) => floats[key] = value;

            public string GetString(string key, string defaultValue = "") => strings.TryGetValue(key, out string v) ? v : defaultValue;
            public void SetString(string key, string value) => strings[key] = value;

            public bool HasKey(string key) => ints.ContainsKey(key) || floats.ContainsKey(key) || strings.ContainsKey(key);
            public void DeleteKey(string key)
            {
                ints.Remove(key);
                floats.Remove(key);
                strings.Remove(key);
            }
            public void Save() => SaveCallCount++;
        }

        private InMemorySaveService mockService;

        [SetUp]
        public void SetUp()
        {
            mockService = new InMemorySaveService();
            SaveSystem.SetService(mockService);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.ResetToDefault();
        }

        [Test]
        public void SaveKeys_FormatMatchesExpectedLegacyKeys()
        {
            Assert.AreEqual("ParkMaster_Stars_L3", SaveKeys.LevelStars(3));
            Assert.AreEqual("ParkMaster_Len_L3", SaveKeys.LevelLength(3));
            Assert.AreEqual("Car_Unlocked1002", SaveKeys.CarUnlocked(1002));
        }

        [Test]
        public void CarData_UnlockPersistsViaSaveSystem()
        {
            var car = new CarData();
            Assert.IsFalse(car.IsUnlocked());

            car.Unlock();

            Assert.IsTrue(car.IsUnlocked());
            Assert.AreEqual(1, mockService.SaveCallCount);
        }
    }
}
