using NUnit.Framework;
using UnityEngine;

namespace ParkMaster.Tests
{
    public class CarDatabaseTests
    {
        private CarDatabase database;

        [SetUp]
        public void SetUp()
        {
            database = ScriptableObject.CreateInstance<CarDatabase>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(database);
        }

        [Test]
        public void EmptyDatabase_ReturnsNullSafely()
        {
            Assert.AreEqual(0, database.CarCount);
            Assert.IsNull(database.GetCarByIndex(0));
            Assert.IsNull(database.GetCarByIndex(-1));
            Assert.IsNull(database.GetCarById(1001));
        }
    }
}
