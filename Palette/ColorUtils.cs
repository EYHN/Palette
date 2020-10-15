using System;
using System.Collections.Generic;
using System.Text;

namespace Palette
{
    public class ColorUtils
    {
        [ThreadStatic]
        private static double[] TEMP_ARRAY;

        public const int BLACK = unchecked((int)0xFF000000);
        public const int DKGRAY = unchecked((int)0xFF444444);
        public const int GRAY = unchecked((int)0xFF888888);
        public const int LTGRAY = unchecked((int)0xFFCCCCCC);
        public const int WHITE = unchecked((int)0xFFFFFFFF);
        public const int TRANSPARENT = 0;

        private const int MIN_ALPHA_SEARCH_MAX_ITERATIONS = 10;
        private const int MIN_ALPHA_SEARCH_PRECISION = 1;

        /// <summary>
        /// Return the alpha component of a color int. This is the same as saying
        /// (color >> 24) & 0xFF
        /// </summary>
        public static int Alpha(int color)
        {
            return (color >> 24) & 0xFF;
        }

        /// <summary>
        /// Return the red component of a color int. This is the same as saying
        /// (color >> 16) & 0xFF
        /// </summary>
        public static int Red(int color)
        {
            return (color >> 16) & 0xFF;
        }

        /// <summary>
        /// Return the green component of a color int. This is the same as saying
        /// (color >> 8) & 0xFF
        /// </summary>
        public static int Green(int color)
        {
            return (color >> 8) & 0xFF;
        }

        /// <summary>
        /// Return the blue component of a color int. This is the same as saying
        /// color & 0xFF
        /// </summary>
        public static int Blue(int color)
        {
            return color & 0xFF;
        }

        /// <summary>
        /// Return a color-int from alpha, red, green, blue components.
        /// These component values should be ([0..255]), but there is no
        /// range check performed, so if they are out of range, the
        /// returned color is undefined.
        /// </summary>
        /// <param name="alpha">Alpha component ([0..255]) of the color</param>
        /// <param name="red">Red component ([0..255]) of the color</param>
        /// <param name="green">Green component ([0..255]) of the color</param>
        /// <param name="blue">Blue component ([0..255]) of the color</param>
        public static int Argb(
                int alpha,
                int red,
                int green,
                int blue)
        {
            return (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        /// <summary>
        /// Return a color-int from red, green, blue components.
        /// The alpha component is implicitly 255 (fully opaque).
        /// These component values should be ([0..255]), but there is no
        /// range check performed, so if they are out of range, the
        /// returned color is undefined.
        /// </summary>
        /// <param name="red">Red component ([0..255]) of the color</param>
        /// <param name="green">Green component ([0..255]) of the color</param>
        /// <param name="blue">Blue component ([0..255]) of the color</param>
        public static int Rgb(
            int red,
            int green,
            int blue)
        {
            return unchecked((int)0xff000000) | (red << 16) | (green << 8) | blue;
        }

        /// <summary>
        /// Composite two potentially translucent colors over each other and returns the result.
        /// </summary>
        public static int compositeColors(int foreground, int background)
        {
            int bgAlpha = Alpha(background);
            int fgAlpha = Alpha(foreground);
            int a = compositeAlpha(fgAlpha, bgAlpha);
            int r = compositeComponent(Red(foreground), fgAlpha,
                    Red(background), bgAlpha, a);
            int g = compositeComponent(Green(foreground), fgAlpha,
                    Green(background), bgAlpha, a);
            int b = compositeComponent(Blue(foreground), fgAlpha,
                    Blue(background), bgAlpha, a);
            return Argb(a, r, g, b);
        }

        private static int compositeAlpha(int foregroundAlpha, int backgroundAlpha)
        {
            return 0xFF - (((0xFF - backgroundAlpha) * (0xFF - foregroundAlpha)) / 0xFF);
        }

        private static int compositeComponent(int fgC, int fgA, int bgC, int bgA, int a)
        {
            if (a == 0) return 0;
            return ((0xFF * fgC * fgA) + (bgC * bgA * (0xFF - fgA))) / (a * 0xFF);
        }

        public static void RGBToHSL(int r,
            int g, int b, float[] outHsl)
        {
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float deltaMaxMin = max - min;
            float h, s;
            float l = (max + min) / 2f;
            if (max == min)
            {
                // Monochromatic
                h = s = 0f;
            }
            else
            {
                if (max == rf)
                {
                    h = ((gf - bf) / deltaMaxMin) % 6f;
                }
                else if (max == gf)
                {
                    h = ((bf - rf) / deltaMaxMin) + 2f;
                }
                else
                {
                    h = ((rf - gf) / deltaMaxMin) + 4f;
                }
                s = deltaMaxMin / (1f - Math.Abs(2f * l - 1f));
            }
            h = (h * 60f) % 360f;
            if (h < 0)
            {
                h += 360f;
            }
            outHsl[0] = constrain(h, 0f, 360f);
            outHsl[1] = constrain(s, 0f, 1f);
            outHsl[2] = constrain(l, 0f, 1f);
        }

        /// <summary>
        /// Convert the ARGB color to its HSL (hue-saturation-lightness) components.
        /// outHsl[0] is Hue [0 .. 360)
        /// outHsl[1] is Saturation [0...1]
        /// outHsl[2] is Lightness [0...1]
        /// </summary>
        /// <param name="color">the ARGB color to convert. The alpha component is ignored</param>
        /// <param name="outHsl">3-element array which holds the resulting HSL components</param>
        public static void colorToHSL(int color, float[] outHsl)
        {
            RGBToHSL(Red(color), Green(color), Blue(color), outHsl);
        }

        private static float constrain(float amount, float low, float high)
        {
            return amount < low ? low : (amount > high ? high : amount);
        }

        /// <summary>
        /// Calculates the minimum alpha value which can be applied to "foreground" so that would
        /// have a contrast value of at least "minContrastRatio" when compared to "background".
        /// </summary>
        /// <param name="foreground">the foreground color</param>
        /// <param name="background">the opaque background color</param>
        /// <param name="minContrastRatio">the minimum contrast ratio</param>
        /// <returns>the alpha value in the range 0-255, or -1 if no value could be calculated</returns>
        public static int calculateMinimumAlpha(int foreground, int background,
                float minContrastRatio)
        {
            if (Alpha(background) != 255)
            {
                throw new ArgumentException("background can not be translucent: #"
                        + background.ToString("X"));
            }
            // First lets check that a fully opaque foreground has sufficient contrast
            int testForeground = setAlphaComponent(foreground, 255);
            double testRatio = calculateContrast(testForeground, background);
            if (testRatio < minContrastRatio)
            {
                // Fully opaque foreground does not have sufficient contrast, return error
                return -1;
            }
            // Binary search to find a value with the minimum value which provides sufficient contrast
            int numIterations = 0;
            int minAlpha = 0;
            int maxAlpha = 255;
            while (numIterations <= MIN_ALPHA_SEARCH_MAX_ITERATIONS &&
                    (maxAlpha - minAlpha) > MIN_ALPHA_SEARCH_PRECISION)
            {
                int testAlpha = (minAlpha + maxAlpha) / 2;
                testForeground = setAlphaComponent(foreground, testAlpha);
                testRatio = calculateContrast(testForeground, background);
                if (testRatio < minContrastRatio)
                {
                    minAlpha = testAlpha;
                }
                else
                {
                    maxAlpha = testAlpha;
                }
                numIterations++;
            }
            // Conservatively return the max of the range of possible alphas, which is known to pass.
            return maxAlpha;
        }

        /// <summary>
        /// Set the alpha component of "color" to be "alpha".
        /// </summary>
        public static int setAlphaComponent(int color, int alpha)
        {
            if (alpha < 0 || alpha > 255)
            {
                throw new ArgumentException("alpha must be between 0 and 255.");
            }
            return (color & 0x00ffffff) | (alpha << 24);
        }


        /// <summary>
        /// Returns the contrast ratio between {@code foreground} and {@code background}.
        /// {@code background} must be opaque.
        /// <para>Formula defined <a href="http://www.w3.org/TR/2008/REC-WCAG20-20081211/#contrast-ratiodef">here</a>.</para>
        /// </summary>
        public static double calculateContrast(int foreground, int background)
        {
            if (Alpha(background) != 255)
            {
                throw new ArgumentException("background can not be translucent: #"
                        + background.ToString("X"));
            }
            if (Alpha(foreground) < 255)
            {
                // If the foreground is translucent, composite the foreground over the background
                foreground = compositeColors(foreground, background);
            }
            double luminance1 = calculateLuminance(foreground) + 0.05;
            double luminance2 = calculateLuminance(background) + 0.05;
            // Now return the lighter luminance divided by the darker luminance
            return Math.Max(luminance1, luminance2) / Math.Min(luminance1, luminance2);
        }

        /// <summary>
        /// Returns the luminance of a color as a float between "0.0" and "1.0".
        /// <para>Defined as the Y component in the XYZ representation of "color".</para>
        /// </summary>
        public static double calculateLuminance(int color)
        {
            double[] result = getTempDouble3Array();
            colorToXYZ(color, result);
            // Luminance is the Y component
            return result[1] / 100;
        }

        /// <summary>
        /// Convert the ARGB color to its CIE XYZ representative components.
        /// <para>The resulting XYZ representation will use the D65 illuminant and the CIE
        /// 2° Standard Observer (1931).</para>
        /// outXyz[0] is X [0 ...95.047)
        /// outXyz[1] is Y [0...100)
        /// outXyz[2] is Z [0...108.883)
        /// </summary>
        /// <param name="color">the ARGB color to convert. The alpha component is ignored</param>
        /// <param name="outXyz">3-element array which holds the resulting XYZ components</param>
        public static void colorToXYZ(int color, double[] outXyz)
        {
            RGBToXYZ(Red(color), Green(color), Blue(color), outXyz);
        }

        /// <summary>
        /// Convert RGB components to its CIE XYZ representative components.
        /// <para>The resulting XYZ representation will use the D65 illuminant and the CIE
        /// 2° Standard Observer (1931).</para>
        /// outXyz[0] is X [0 ...95.047)
        /// outXyz[1] is Y [0...100)
        /// outXyz[2] is Z [0...108.883)
        /// </summary>
        /// <param name="r">red component value [0..255]</param>
        /// <param name="g">green component value [0..255]</param>
        /// <param name="b">blue component value [0..255]</param>
        /// <param name="outXyz">3-element array which holds the resulting XYZ components</param>
        public static void RGBToXYZ(int r,
                int g, int b,
                double[] outXyz)
        {
            if (outXyz.Length != 3)
            {
                throw new ArgumentException("outXyz must have a length of 3.");
            }
            double sr = r / 255.0;
            sr = sr < 0.04045 ? sr / 12.92 : Math.Pow((sr + 0.055) / 1.055, 2.4);
            double sg = g / 255.0;
            sg = sg < 0.04045 ? sg / 12.92 : Math.Pow((sg + 0.055) / 1.055, 2.4);
            double sb = b / 255.0;
            sb = sb < 0.04045 ? sb / 12.92 : Math.Pow((sb + 0.055) / 1.055, 2.4);
            outXyz[0] = 100 * (sr * 0.4124 + sg * 0.3576 + sb * 0.1805);
            outXyz[1] = 100 * (sr * 0.2126 + sg * 0.7152 + sb * 0.0722);
            outXyz[2] = 100 * (sr * 0.0193 + sg * 0.1192 + sb * 0.9505);
        }


        private static double[] getTempDouble3Array()
        {
            double[] result = TEMP_ARRAY;
            if (result == null)
            {
                result = new double[3];
                TEMP_ARRAY = result;
            }
            return result;
        }
    }
}
