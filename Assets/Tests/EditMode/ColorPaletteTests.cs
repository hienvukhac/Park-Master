using NUnit.Framework;
using UnityEngine;

namespace ParkMaster.Tests
{
    public class ColorPaletteTests
    {
        [Test]
        public void GetColor_ReturnsOpaqueColorForAllNamedTypes()
        {
            ColorType[] namedTypes =
            {
                ColorType.Red, ColorType.Blue, ColorType.Yellow, ColorType.Green,
                ColorType.Orange, ColorType.Purple, ColorType.Pink, ColorType.Cyan
            };

            foreach (ColorType type in namedTypes)
            {
                Color c = ColorPalette.GetColor(type);
                Assert.AreEqual(1.0f, c.a, 0.001f, $"Alpha should be 1 for {type}");
                Assert.AreNotEqual(Color.white, c, $"Named color {type} should not fall back to white");
            }
        }

        [Test]
        public void GetColorHex_StartsWithHashAndHasSevenChars()
        {
            string hex = ColorPalette.GetColorHex(ColorType.Red);
            Assert.IsTrue(hex.StartsWith("#"));
            Assert.AreEqual(7, hex.Length);
        }
    }
}
