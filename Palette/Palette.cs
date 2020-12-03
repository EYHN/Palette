using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;

namespace Palette
{
    public class Palette
    {
        // https://android.googlesource.com/platform/frameworks/support/+/refs/heads/androidx-master-dev/palette/palette/src/main/java/androidx/palette/graphics/Palette.java?source=post_page---------------------------%2F&autodive=0%2F%2F%2F%2F%2F
        static readonly int DEFAULT_RESIZE_BITMAP_AREA = 112 * 112;
        static readonly int DEFAULT_CALCULATE_NUMBER_COLORS = 16;
        static readonly float MIN_CONTRAST_TITLE_TEXT = 3.0f;
        static readonly float MIN_CONTRAST_BODY_TEXT = 4.5f;

        /// <summary>
        /// Start generating a <see cref="Palette"/> with the returned <see cref="Builder"/> instance.
        /// </summary>
        public static Builder From(NetVips.Image image)
        {
            return new Builder(image);
        }

        /// <summary> 
        /// Generate a <see cref="Palette"/> from the pre-generated list of <see cref="Swatch"/> swatches.
        /// This is useful for testing, or if you want to resurrect a <see cref="Palette"/> instance from a
        /// list of swatches. Will return null if the "swatches" is null.
        /// </summary>
        public static Palette From(List<Swatch> swatches)
        {
            return new Builder(swatches).Generate();
        }

        private readonly List<Swatch> mSwatches;
        private readonly List<Target> mTargets;
        private readonly Dictionary<Target, Swatch?> mSelectedSwatches;
        private readonly Dictionary<int, bool> mUsedColors;
        private readonly Swatch? mDominantSwatch;

        Palette(List<Swatch> swatches, List<Target> targets)
        {
            mSwatches = swatches;
            mTargets = targets;
            mUsedColors = new Dictionary<int, bool>();
            mSelectedSwatches = new Dictionary<Target, Swatch?>();
            mDominantSwatch = FindDominantSwatch();
        }

        /// <summary>
        /// Returns all of the swatches which make up the palette.
        /// </summary>
        public ICollection<Swatch> GetSwatches()
        {
            return new ReadOnlyCollection<Swatch>(mSwatches);
        }

        /// <summary>
        /// Returns the targets used to generate this palette.
        /// </summary>
        public ICollection<Target> GetTargets()
        {
            return new ReadOnlyCollection<Target>(mTargets);
        }

        public Swatch? GetVibrantSwatch()
        {
            return GetSwatchForTarget(Target.VIBRANT);
        }

        public Swatch? GetLightVibrantSwatch()
        {
            return GetSwatchForTarget(Target.LIGHT_VIBRANT);
        }

        public Swatch? GetDarkVibrantSwatch()
        {
            return GetSwatchForTarget(Target.DARK_VIBRANT);
        }

        public Swatch? GetMutedSwatch()
        {
            return GetSwatchForTarget(Target.MUTED);
        }

        public Swatch? GetLightMutedSwatch()
        {
            return GetSwatchForTarget(Target.LIGHT_MUTED);
        }

        public Swatch? GetDarkMutedSwatch()
        {
            return GetSwatchForTarget(Target.DARK_MUTED);
        }

        public int? GetVibrantColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.VIBRANT, defaultColor);
        }

        public int? GetLightVibrantColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.LIGHT_VIBRANT, defaultColor);
        }

        public int? GetDarkVibrantColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.DARK_VIBRANT, defaultColor);
        }

        public int? GetMutedColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.MUTED, defaultColor);
        }

        public int? GetLightMutedColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.LIGHT_MUTED, defaultColor);
        }

        public int? GetDarkMutedColor(int? defaultColor = null)
        {
            return GetColorForTarget(Target.DARK_MUTED, defaultColor);
        }

        /// <summary>
        /// Returns the selected swatch for the given target from the palette, or null if one
        /// could not be found.
        /// </summary>
        public Swatch? GetSwatchForTarget(Target target)
        {
            mSelectedSwatches.TryGetValue(target, out var swatch);
            return swatch;
        }

        /// <summary>
        /// Returns the selected color for the given target from the palette as an RGB packed int.
        /// </summary>
        /// <param name="defaultColor">value to return if the swatch isn't available</param>
        public int? GetColorForTarget(Target target, int? defaultColor = null)
        {
            Swatch? swatch = GetSwatchForTarget(target);
            return swatch != null ? swatch.GetRgb() : defaultColor;
        }

        /// <summary>
        /// Returns the dominant swatch from the palette.
        /// <para>The dominant swatch is defined as the swatch with the greatest population (frequency)
        /// within the palette.</para>
        /// </summary>
        public Swatch? GetDominantSwatch()
        {
            return mDominantSwatch;
        }

        /// <summary>
        /// Returns the color of the dominant swatch from the palette, as an RGB packed int.
        /// <para><seealso cref="GetDominantSwatch"/></para>
        /// </summary>
        /// <param name="defaultColor">value to return if the swatch isn't available</param>
        public int? GetDominantColor(int? defaultColor = null)
        {
            return mDominantSwatch?.GetRgb() ?? defaultColor;
        }

        void Generate()
        {
            // We need to make sure that the scored targets are generated first. This is so that
            // inherited targets have something to inherit from
            for (int i = 0, count = mTargets.Count; i < count; i++)
            {
                Target target = mTargets[i];
                target.NormalizeWeights();
                mSelectedSwatches.Add(target, GenerateScoredTarget(target));
            }
            // We now clear out the used colors
            mUsedColors.Clear();
        }

        private Swatch? GenerateScoredTarget(Target target)
        {
            Swatch? maxScoreSwatch = GetMaxScoredSwatchForTarget(target);
            if (maxScoreSwatch != null && target.IsExclusive)
            {
                // If we have a swatch, and the target is exclusive, add the color to the used list
                mUsedColors.Add(maxScoreSwatch.GetRgb(), true);
            }
            return maxScoreSwatch;
        }

        private Swatch? GetMaxScoredSwatchForTarget(Target target)
        {
            float maxScore = 0;
            Swatch? maxScoreSwatch = null;
            for (int i = 0, count = mSwatches.Count; i < count; i++)
            {
                Swatch swatch = mSwatches[i];
                if (ShouldBeScoredForTarget(swatch, target))
                {
                    float score = GenerateScore(swatch, target);
                    if (maxScoreSwatch == null || score > maxScore)
                    {
                        maxScoreSwatch = swatch;
                        maxScore = score;
                    }
                }
            }
            return maxScoreSwatch;
        }

        private bool ShouldBeScoredForTarget(Swatch swatch, Target target)
        {
            // Check whether the HSL values are within the correct ranges, and this color hasn't
            // been used yet.
            float[] hsl = swatch.GetHsl();
            mUsedColors.TryGetValue(swatch.GetRgb(), out bool used);
            return hsl[1] >= target.GetMinimumSaturation() && hsl[1] <= target.GetMaximumSaturation()
                    && hsl[2] >= target.GetMinimumLightness() && hsl[2] <= target.GetMaximumLightness()
                    && !used;
        }

        private float GenerateScore(Swatch swatch, Target target)
        {
            float[] hsl = swatch.GetHsl();
            float saturationScore = 0;
            float luminanceScore = 0;
            float populationScore = 0;
            int maxPopulation = mDominantSwatch != null ? mDominantSwatch.GetPopulation() : 1;
            if (target.GetSaturationWeight() > 0)
            {
                saturationScore = target.GetSaturationWeight()
                        * (1f - Math.Abs(hsl[1] - target.GetTargetSaturation()));
            }
            if (target.GetLightnessWeight() > 0)
            {
                luminanceScore = target.GetLightnessWeight()
                        * (1f - Math.Abs(hsl[2] - target.GetTargetLightness()));
            }
            if (target.GetPopulationWeight() > 0)
            {
                populationScore = target.GetPopulationWeight()
                        * (swatch.GetPopulation() / (float)maxPopulation);
            }
            return saturationScore + luminanceScore + populationScore;
        }

        private Swatch? FindDominantSwatch()
        {
            int maxPop = int.MinValue;
            Swatch? maxSwatch = null;
            for (int i = 0, count = mSwatches.Count; i < count; i++)
            {
                Swatch swatch = mSwatches[i];
                if (swatch.GetPopulation() > maxPop)
                {
                    maxSwatch = swatch;
                    maxPop = swatch.GetPopulation();
                }
            }
            return maxSwatch;
        }

        /// <summary>
        /// Represents a color swatch generated from an image's palette. The RGB color can be retrieved
        /// by calling <see cref="GetRgb()"/>.
        /// </summary>
        public sealed class Swatch
        {
            private readonly int Red, Green, Blue;
            private readonly int Rgb;
            private readonly int Population;
            private bool GeneratedTextColors;
            private int TitleTextColor;
            private int BodyTextColor;
            private float[]? Hsl;
            public Swatch(int color, int population)
            {
                Red = ColorUtils.Red(color);
                Green = ColorUtils.Green(color);
                Blue = ColorUtils.Blue(color);
                Rgb = color;
                Population = population;
            }
            /**
             * @return this swatch's RGB color value
             */
            public int GetRgb()
            {
                return Rgb;
            }

            public string GetHex()
            {
                return "#" + GetRgb().ToString("X").Substring(2);
            }
            
            /**
             * Return this swatch's HSL values.
             *     hsv[0] is Hue [0 .. 360)
             *     hsv[1] is Saturation [0...1]
             *     hsv[2] is Lightness [0...1]
             */
            public float[] GetHsl()
            {
                if (Hsl == null)
                {
                    Hsl = new float[3];
                }
                ColorUtils.RGBToHSL(Red, Green, Blue, Hsl);
                return Hsl;
            }
            /**
             * @return the number of pixels represented by this swatch
             */
            public int GetPopulation()
            {
                return Population;
            }
            /**
             * Returns an appropriate color to use for any 'title' text which is displayed over this
             * {@link Swatch}'s color. This color is guaranteed to have sufficient contrast.
             */
            public int GetTitleTextColor()
            {
                EnsureTextColorsGenerated();
                return TitleTextColor;
            }
            /**
             * Returns an appropriate color to use for any 'body' text which is displayed over this
             * {@link Swatch}'s color. This color is guaranteed to have sufficient contrast.
             */
            public int GetBodyTextColor()
            {
                EnsureTextColorsGenerated();
                return BodyTextColor;
            }

            private void EnsureTextColorsGenerated()
            {
                if (!GeneratedTextColors)
                {
                    // First check white, as most colors will be dark
                    int lightBodyAlpha = ColorUtils.calculateMinimumAlpha(
                            ColorUtils.WHITE, Rgb, MIN_CONTRAST_BODY_TEXT);
                    int lightTitleAlpha = ColorUtils.calculateMinimumAlpha(
                            ColorUtils.WHITE, Rgb, MIN_CONTRAST_TITLE_TEXT);
                    if (lightBodyAlpha != -1 && lightTitleAlpha != -1)
                    {
                        // If we found valid light values, use them and return
                        BodyTextColor = ColorUtils.setAlphaComponent(ColorUtils.WHITE, lightBodyAlpha);
                        TitleTextColor = ColorUtils.setAlphaComponent(ColorUtils.WHITE, lightTitleAlpha);
                        GeneratedTextColors = true;
                        return;
                    }
                    int darkBodyAlpha = ColorUtils.calculateMinimumAlpha(
                            ColorUtils.BLACK, Rgb, MIN_CONTRAST_BODY_TEXT);
                    int darkTitleAlpha = ColorUtils.calculateMinimumAlpha(
                            ColorUtils.BLACK, Rgb, MIN_CONTRAST_TITLE_TEXT);
                    if (darkBodyAlpha != -1 && darkTitleAlpha != -1)
                    {
                        // If we found valid dark values, use them and return
                        BodyTextColor = ColorUtils.setAlphaComponent(ColorUtils.BLACK, darkBodyAlpha);
                        TitleTextColor = ColorUtils.setAlphaComponent(ColorUtils.BLACK, darkTitleAlpha);
                        GeneratedTextColors = true;
                        return;
                    }
                    // If we reach here then we can not find title and body values which use the same
                    // lightness, we need to use mismatched values
                    BodyTextColor = lightBodyAlpha != -1
                            ? ColorUtils.setAlphaComponent(ColorUtils.WHITE, lightBodyAlpha)
                            : ColorUtils.setAlphaComponent(ColorUtils.BLACK, darkBodyAlpha);
                    TitleTextColor = lightTitleAlpha != -1
                            ? ColorUtils.setAlphaComponent(ColorUtils.WHITE, lightTitleAlpha)
                            : ColorUtils.setAlphaComponent(ColorUtils.BLACK, darkTitleAlpha);
                    GeneratedTextColors = true;
                }
            }

            public override string ToString()
            {
                return new StringBuilder(GetType().Name)
                        .Append(" [RGB: #").Append(GetRgb().ToString("X")).Append(']')
                        .Append(" [HSL: ").Append(string.Join(",", GetHsl())).Append(']')
                        .Append(" [Population: ").Append(Population).Append(']')
                        .Append(" [Title Text: #").Append(GetTitleTextColor().ToString("X"))
                        .Append(']')
                        .Append(" [Body Text: #").Append(GetBodyTextColor().ToString("X"))
                        .Append(']').ToString();
            }

            public override bool Equals(object o)
            {
                if (this == o)
                {
                    return true;
                }
                if (o == null || GetType() != o.GetType())
                {
                    return false;
                }
                Swatch swatch = (Swatch)o;
                return Population == swatch.Population && Rgb == swatch.Rgb;
            }

            public override int GetHashCode()
            {
                return 31 * Rgb + Population;
            }
        }


        public sealed class Builder
        {
            private readonly List<Swatch>? swatches;
            private readonly NetVips.Image? image;
            private readonly List<Target> targets = new List<Target>();

            private int maxColors = DEFAULT_CALCULATE_NUMBER_COLORS;
            private int resizeArea = DEFAULT_RESIZE_BITMAP_AREA;
            private int resizeMaxDimension = -1;

            private readonly List<Filter> filters = new List<Filter>();

            /// <summary>
            /// Construct a new <see cref="Builder"/> using a source <see cref="NetVips.Image"/>
            /// </summary>
            public Builder(NetVips.Image Image)
            {
                if (Image == null || Image.IsClosed)
                {
                    throw new ArgumentException("Image is not valid");
                }
                filters.Add(DEFAULT_FILTER);
                image = Image;
                swatches = null;
                // Add the default targets
                targets.Add(Target.LIGHT_VIBRANT);
                targets.Add(Target.VIBRANT);
                targets.Add(Target.DARK_VIBRANT);
                targets.Add(Target.LIGHT_MUTED);
                targets.Add(Target.MUTED);
                targets.Add(Target.DARK_MUTED);
            }

            /// <summary>
            /// Construct a new <see cref="Builder"/> using a list of <see cref="Swatch"/> instances.
            /// Typically only used for testing.
            /// </summary>
            public Builder(List<Swatch> swatches)
            {
                if (swatches == null || swatches.Count == 0)
                {
                    throw new ArgumentException("List of Swatches is not valid");
                }
                filters.Add(DEFAULT_FILTER);
                this.swatches = swatches;
                image = null;
            }

            /// <summary>
            /// Set the maximum number of colors to use in the quantization step when using a
            /// <see cref="NetVips.Image"/> as the source.
            /// <para>
            /// Good values for depend on the source image type. For landscapes, good values are in
            /// the range 10-16. For images which are largely made up of people's faces then this
            /// value should be increased to ~24.
            /// </para>
            /// </summary>
            public Builder MaximumColorCount(int colors)
            {
                maxColors = colors;
                return this;
            }

            /// <summary>
            /// Set the resize value when using a {@link android.graphics.Bitmap} as the source.
            /// If the bitmap's area is greater than the value specified, then the bitmap
            /// will be resized so that its area matches {@code area}. If the
            /// bitmap is smaller or equal, the original is used as-is.
            /// <para>
            /// This value has a large effect on the processing time. The larger the resized image is,
            /// the greater time it will take to generate the palette. The smaller the image is, the
            /// more detail is lost in the resulting image and thus less precision for color selection.
            /// </para>
            /// </summary>
            /// <param name="area">the number of pixels that the intermediary scaled down Bitmap should cover,
            /// or any value <= 0 to disable resizing.</param>
            public Builder ResizeBitmapArea(int area)
            {
                resizeArea = area;
                resizeMaxDimension = -1;
                return this;
            }

            /// <summary>
            /// Clear all added filters. This includes any default filters added automatically by
            /// <see cref="Palette"/>
            /// </summary>
            public Builder ClearFilters()
            {
                filters.Clear();
                return this;
            }

            /// <summary>
            /// Add a filter to be able to have fine grained control over which colors are
            /// allowed in the resulting palette.
            /// </summary>
            /// <param name="Filter">filter to add.</param>
            public Builder AddFilter(Filter filter)
            {
                if (filter != null)
                {
                    filters.Add(filter);
                }
                return this;
            }

            /// <summary>
            /// Add a target profile to be generated in the palette.
            /// <para>You can retrieve the result via <see cref="Palette.GetSwatchForTarget"/>.</para>
            /// </summary>
            public Builder AddTarget(Target target)
            {
                if (!targets.Contains(target))
                {
                    targets.Add(target);
                }
                return this;
            }

            /// <summary>
            /// Clear all added targets. This includes any default targets added automatically by
            /// <see cref="Palette"/>
            /// </summary>
            public Builder ClearTargets()
            {
                if (targets != null)
                {
                    targets.Clear();
                }
                return this;
            }

            /// <summary>
            /// Generate and return the <see cref="Palette"/> synchronously.
            /// </summary>
            public Palette Generate()
            {
                List<Swatch> swatches;
                if (image != null)
                {
                    // We have a Bitmap so we need to use quantization to reduce the number of colors
                    // First we'll scale down the image if needed
                    NetVips.Image ScaledImage = ScaleImageDown(image);

                    // Now generate a quantizer from the Image
                    ColorCutQuantizer quantizer = new ColorCutQuantizer(
                            GetPixelsFromImage(ScaledImage),
                            maxColors,
                            filters.Count == 0 ? null : filters.ToArray());

                    swatches = quantizer.getQuantizedColors();
                }
                else if (this.swatches != null)
                {
                    // Else we're using the provided swatches
                    swatches = this.swatches;
                }
                else
                {
                    // The constructors enforce either a image or swatches are present.
                    throw new Exception();
                }
                // Now create a Palette instance
                Palette p = new Palette(swatches, targets);
                // And make it generate itself
                p.Generate();
                return p;
            }

            private int[] GetPixelsFromImage(NetVips.Image Image)
            {
                Image = Image.Colourspace("srgb").Cast("uchar");
                if (!Image.HasAlpha())
                {
                    Image = Image.Bandjoin(255);
                }

                int ImageWidth = Image.Width;
                int ImageHeight = Image.Height;

                IntPtr ImageDataPtr = Image.WriteToMemory(out var ByteLength);

                try
                {
                    if ((int)ByteLength != ImageWidth * ImageHeight * 4)
                    {
                        throw new Exception("Unexpected byte length.");
                    }

                    int[] Pixels = new int[ImageWidth * ImageHeight];
                    Marshal.Copy(ImageDataPtr, Pixels, 0, ImageWidth * ImageHeight);

                    for (int i = 0; i < Pixels.Length;i++)
                    {
                        Pixels[i] = ColorUtils.Argb((Pixels[i] >> 24) & 0xff, Pixels[i] & 0xff, (Pixels[i] >> 8) & 0xff, (Pixels[i] >> 16) & 0xff);
                    }

                    return Pixels;
                } finally
                {
                    NetVips.NetVips.Free(ImageDataPtr);
                }
            }

            private NetVips.Image ScaleImageDown(NetVips.Image Image)
            {
                double scaleRatio = -1;
                if (resizeArea > 0)
                {
                    int bitmapArea = Image.Width * Image.Height;
                    if (bitmapArea > resizeArea)
                    {
                        scaleRatio = Math.Sqrt(resizeArea / (double)bitmapArea);
                    }
                }
                else if (resizeMaxDimension > 0)
                {
                    int maxDimension = Math.Max(Image.Width, Image.Height);
                    if (maxDimension > resizeMaxDimension)
                    {
                        scaleRatio = resizeMaxDimension / (double)maxDimension;
                    }
                }
                if (scaleRatio <= 0)
                {
                    // Scaling has been disabled or not needed so just return the Bitmap
                    return Image;
                }
                return Image.Resize(scaleRatio, NetVips.Enums.Kernel.Nearest);
            }
        }

        /// <summary>
        /// A Filter provides a mechanism for exercising fine-grained control over which colors
        /// are valid within a resulting {@link Palette}.
        /// </summary>
        public interface Filter
        {
            /// <summary>
            /// Hook to allow clients to be able filter colors from resulting palette.
            ///
            /// @param rgb the color in RGB888.
            /// @param hsl HSL representation of the color.
            ///
            /// @return true if the color is allowed, false if not.
            ///
            /// @see Builder#addFilter(Filter)
            /// <see cref="Builder.AddFilter"/>
            /// </summary>
            bool IsAllowed(int rgb, float[] hsl);
        }

        /// <summary>
        /// The default filter.
        /// </summary>
        static readonly Filter DEFAULT_FILTER = new DefaultFilter();

        class DefaultFilter : Filter
        {
            private static readonly float BLACK_MAX_LIGHTNESS = 0.05f;
            private static readonly float WHITE_MIN_LIGHTNESS = 0.95f;

            public bool IsAllowed(int rgb, float[] hsl)
            {
                return !IsWhite(hsl) && !IsBlack(hsl) && !IsNearRedILine(hsl);
            }

            /// <returns>true if the color represents a color which is close to black.</returns>
            private bool IsBlack(float[] hslColor)
            {
                return hslColor[2] <= BLACK_MAX_LIGHTNESS;
            }

            /// <returns>true if the color represents a color which is close to white.</returns>
            private bool IsWhite(float[] hslColor)
            {
                return hslColor[2] >= WHITE_MIN_LIGHTNESS;
            }

            /// <returns>true if the color lies close to the red side of the I line.</returns>
            private bool IsNearRedILine(float[] hslColor)
            {
                return hslColor[0] >= 10f && hslColor[0] <= 37f && hslColor[1] <= 0.82f;
            }
        }
    }
}
