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
            Palette palette = Palette.From(NetVips.Image.Thumbnail(TestUtils.GetResourceFullPath("Test Image.png"), 112, 112, size: NetVips.Enums.Size.Down)).Generate();
            Console.WriteLine("Test Image.png:");
            foreach (var swatch in palette.GetSwatches())
            {
                Console.WriteLine("\t" + swatch.ToString());
            }
        }

        [TestMethod()]
        public void BigPixelTest()
        {
            Palette palette = Palette.From(NetVips.Image.Thumbnail(TestUtils.GetResourceFullPath("20000px.png"), 112, 112, size: NetVips.Enums.Size.Down)).Generate();
            Console.WriteLine(" ");
            Console.WriteLine("20000px.png:");
            foreach (var swatch in palette.GetSwatches())
            {
                Console.WriteLine("\t" + swatch.ToString());
            }
        }
    }
}