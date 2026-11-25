using System.Collections.Generic;
using NUnit.Framework;
using ParkMaster.Drawing;
using UnityEngine;

namespace ParkMaster.Tests
{
    public class PathMathUtilityTests
    {
        [Test]
        public void SqrDistXZ_IgnoresYAxis()
        {
            var a = new Vector3(1f, 0f, 2f);
            var b = new Vector3(4f, 50f, 6f);
            Assert.AreEqual(25f, PathMathUtility.SqrDistXZ(a, b), 0.0001f);
        }

        [Test]
        public void CalculatePathLengthXZ_ReturnsSumOfSegments()
        {
            var points = new List<Vector3>
            {
                new Vector3(0f, 2f, 0f),
                new Vector3(3f, 2f, 0f),
                new Vector3(3f, 2f, 4f)
            };

            float length = PathMathUtility.CalculatePathLengthXZ(points);
            Assert.AreEqual(7f, length, 0.0001f);
        }

        [Test]
        public void CalculatePathLengthXZ_EmptyOrSinglePoint_ReturnsZero()
        {
            Assert.AreEqual(0f, PathMathUtility.CalculatePathLengthXZ(null));
            Assert.AreEqual(0f, PathMathUtility.CalculatePathLengthXZ(new List<Vector3> { Vector3.zero }));
        }

        [Test]
        public void ClampPointToBoundsXZ_ClampsWithinPaddingAndOverridesY()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f));
            var outside = new Vector3(100f, 99f, -100f);

            Vector3 clamped = PathMathUtility.ClampPointToBoundsXZ(outside, bounds, 1f, 2.5f);

            Assert.AreEqual(4f, clamped.x, 0.0001f);
            Assert.AreEqual(2.5f, clamped.y, 0.0001f);
            Assert.AreEqual(-4f, clamped.z, 0.0001f);
        }
    }
}
