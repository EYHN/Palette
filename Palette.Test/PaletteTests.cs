using Microsoft.VisualStudio.TestTools.UnitTesting;
using Palette;
using Palette.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace Palette.Tests
{
    [TestClass()]
    public class PaletteTests
    {
        [TestMethod()]
        public void PaletteTest()
        {
            void Test(string imageName)
            {
                Palette palette = Palette.From(NetVips.Image.Thumbnail(TestUtils.GetResourceFullPath(imageName), 112, 112, size: NetVips.Enums.Size.Down)).Generate();
                Console.WriteLine(imageName);
            
                Console.WriteLine("Vibrant: " + palette.GetVibrantSwatch()?.GetHex());
                Console.WriteLine("Muted: " + palette.GetMutedSwatch()?.GetHex());
                Console.WriteLine("DarkVibrant: " + palette.GetDarkVibrantSwatch()?.GetHex());
                Console.WriteLine("DarkMuted: " + palette.GetDarkMutedSwatch()?.GetHex());
                Console.WriteLine("LightVibrant: " + palette.GetLightVibrantSwatch()?.GetHex());
                Console.WriteLine("LightMuted: " + palette.GetLightMutedSwatch()?.GetHex());

                Console.WriteLine("Swatches: ");
                foreach (var swatch in palette.GetSwatches())
                {
                    Console.WriteLine("\t" + swatch.ToString());
                }
            
                Console.WriteLine("");
            }

            Test("Test Image 1.png");
            Test("Test Image 2.jpg");
        }

        [TestMethod()]
        public void BigPixelTest()
        {
            Palette palette = Palette.From(NetVips.Image.Thumbnail(TestUtils.GetResourceFullPath("20000px.png"), 112, 112, size: NetVips.Enums.Size.Down)).Generate();
            Console.WriteLine("20000px.png:");
            foreach (var swatch in palette.GetSwatches())
            {
                Console.WriteLine("\t" + swatch.ToString());
            }
        }
    }
}