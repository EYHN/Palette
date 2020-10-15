using System;
using System.Collections.Generic;
using System.Text;
using Priority_Queue;

namespace Palette
{
    class ColorCutQuantizer
    {
        const int COMPONENT_RED = -3;
        const int COMPONENT_GREEN = -2;
        const int COMPONENT_BLUE = -1;
        private const int QUANTIZE_WORD_WIDTH = 5;
        private const int QUANTIZE_WORD_MASK = (1 << QUANTIZE_WORD_WIDTH) - 1;
        readonly int[] mColors;
        readonly int[] mHistogram;
        readonly List<Palette.Swatch> mQuantizedColors;
        readonly Palette.Filter[] mFilters;
        private readonly float[] mTempHsl = new float[3];

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pixels">histogram representing an image's pixel data</param>
        /// <param name="maxColors">The maximum number of colors that should be in the result palette.</param>
        /// <param name="filters">Set of filters to use in the quantization stage</param>
        internal ColorCutQuantizer(int[] pixels, int maxColors, Palette.Filter[] filters)
        {
            mFilters = filters;
            int[] hist = mHistogram = new int[1 << (QUANTIZE_WORD_WIDTH * 3)];
            for (int i = 0; i < pixels.Length; i++)
            {
                int quantizedColor = quantizeFromRgb888(pixels[i]);
                // Now update the pixel value to the quantized value
                pixels[i] = quantizedColor;
                // And update the histogram
                hist[quantizedColor]++;
            }
            // Now let's count the number of distinct colors
            int distinctColorCount = 0;
            for (int color = 0; color < hist.Length; color++)
            {
                if (hist[color] > 0 && shouldIgnoreColor(color))
                {
                    // If we should ignore the color, set the population to 0
                    hist[color] = 0;
                }
                if (hist[color] > 0)
                {
                    // If the color has population, increase the distinct color count
                    distinctColorCount++;
                }
            }
            // Now lets go through create an array consisting of only distinct colors
            int[] colors = mColors = new int[distinctColorCount];
            int distinctColorIndex = 0;
            for (int color = 0; color < hist.Length; color++)
            {
                if (hist[color] > 0)
                {
                    colors[distinctColorIndex++] = color;
                }
            }
            if (distinctColorCount <= maxColors)
            {
                // The image has fewer colors than the maximum requested, so just return the colors
                mQuantizedColors = new List<Palette.Swatch>();
                foreach (int color in colors)
                {
                    mQuantizedColors.Add(new Palette.Swatch(approximateToRgb888(color), hist[color]));
                }
            }
            else
            {
                // We need use quantization to reduce the number of colors
                mQuantizedColors = quantizePixels(maxColors);
            }
        }

        /// <returns>the list of quantized colors</returns>
        internal List<Palette.Swatch> getQuantizedColors()
        {
            return mQuantizedColors;
        }

        private List<Palette.Swatch> quantizePixels(int maxColors)
        {
            // Create the priority queue which is sorted by volume descending. This means we always
            // split the largest box in the queue
            SimplePriorityQueue<Vbox> pq = new SimplePriorityQueue<Vbox>(new VolumeComparer());
            // To start, offer a box which contains all of the colors
            Vbox baseVbox = new Vbox(this, 0, mColors.Length - 1);
            pq.Enqueue(new Vbox(this, 0, mColors.Length - 1), baseVbox.getVolume());
            // Now go through the boxes, splitting them until we have reached maxColors or there are no
            // more boxes to split
            splitBoxes(pq, maxColors);
            // Finally, return the average colors of the color boxes
            return generateAverageColors(pq);
        }

        private void splitBoxes(SimplePriorityQueue<Vbox> queue, int maxSize)
        {
            while (queue.Count < maxSize)
            {
                Vbox vbox = queue.Dequeue();
                if (vbox != null && vbox.canSplit())
                {
                    // First split the box, and offer the result
                    Vbox newVbox = vbox.splitBox();
                    queue.Enqueue(newVbox, newVbox.getVolume());
                    // Then offer the box back
                    queue.Enqueue(vbox, vbox.getVolume());
                }
                else
                {
                    // If we get here then there are no more boxes to split, so return
                    return;
                }
            }
        }

        private List<Palette.Swatch> generateAverageColors(SimplePriorityQueue<Vbox> vboxes)
        {
            List<Palette.Swatch> colors = new List<Palette.Swatch>(vboxes.Count);
            foreach (Vbox vbox in vboxes)
            {
                Palette.Swatch swatch = vbox.getAverageColor();
                if (!shouldIgnoreColor(swatch))
                {
                    // As we're averaging a color box, we can still get colors which we do not want, so
                    // we check again here
                    colors.Add(swatch);
                }
            }
            return colors;
        }

        /// <summary>
        /// Represents a tightly fitting box around a color space.
        /// </summary>
        private class Vbox: IComparable<Vbox>
        {
            private readonly ColorCutQuantizer parent;
            // lower and upper index are inclusive
            private int mLowerIndex;
            private int mUpperIndex;
            // Population of colors within this box
            private int mPopulation;
            private int mMinRed, mMaxRed;
            private int mMinGreen, mMaxGreen;
            private int mMinBlue, mMaxBlue;

            internal Vbox(ColorCutQuantizer parent, int lowerIndex, int upperIndex)
            {
                this.parent = parent;
                mLowerIndex = lowerIndex;
                mUpperIndex = upperIndex;
                fitBox();
            }

            internal int getVolume()
            {
                return (mMaxRed - mMinRed + 1) * (mMaxGreen - mMinGreen + 1) *
                        (mMaxBlue - mMinBlue + 1);
            }

            internal bool canSplit()
            {
                return getColorCount() > 1;
            }

            internal int getColorCount()
            {
                return 1 + mUpperIndex - mLowerIndex;
            }

            /// <summary>
            /// Recomputes the boundaries of this box to tightly fit the colors within the box.
            /// </summary>
            internal void fitBox()
            {
                int[] colors = parent.mColors;
                int[] hist = parent.mHistogram;
                // Reset the min and max to opposite values
                int minRed, minGreen, minBlue;
                minRed = minGreen = minBlue = int.MaxValue;
                int maxRed, maxGreen, maxBlue;
                maxRed = maxGreen = maxBlue = int.MinValue;
                int count = 0;
                for (int i = mLowerIndex; i <= mUpperIndex; i++)
                {
                    int color = colors[i];
                    count += hist[color];
                    int r = quantizedRed(color);
                    int g = quantizedGreen(color);
                    int b = quantizedBlue(color);
                    if (r > maxRed)
                    {
                        maxRed = r;
                    }
                    if (r < minRed)
                    {
                        minRed = r;
                    }
                    if (g > maxGreen)
                    {
                        maxGreen = g;
                    }
                    if (g < minGreen)
                    {
                        minGreen = g;
                    }
                    if (b > maxBlue)
                    {
                        maxBlue = b;
                    }
                    if (b < minBlue)
                    {
                        minBlue = b;
                    }
                }
                mMinRed = minRed;
                mMaxRed = maxRed;
                mMinGreen = minGreen;
                mMaxGreen = maxGreen;
                mMinBlue = minBlue;
                mMaxBlue = maxBlue;
                mPopulation = count;
            }

            /// <summary>
            /// Split this color box at the mid-point along its longest dimension
            /// </summary>
            /// <returns>the new ColorBox</returns>
            internal Vbox splitBox()
            {
                if (!canSplit())
                {
                    throw new InvalidOperationException("Can not split a box with only 1 color");
                }
                // find median along the longest dimension
                int splitPoint = findSplitPoint();
                Vbox newBox = new Vbox(parent, splitPoint + 1, mUpperIndex);
                // Now change this box's upperIndex and recompute the color boundaries
                mUpperIndex = splitPoint;
                fitBox();
                return newBox;
            }

            /// <returns>the dimension which this box is largest in</returns>
            internal int getLongestColorDimension()
            {
                int redLength = mMaxRed - mMinRed;
                int greenLength = mMaxGreen - mMinGreen;
                int blueLength = mMaxBlue - mMinBlue;
                if (redLength >= greenLength && redLength >= blueLength)
                {
                    return COMPONENT_RED;
                }
                else if (greenLength >= redLength && greenLength >= blueLength)
                {
                    return COMPONENT_GREEN;
                }
                else
                {
                    return COMPONENT_BLUE;
                }
            }

            /// <summary>
            /// Finds the point within this box's lowerIndex and upperIndex index of where to split.
            ///
            /// This is calculated by finding the longest color dimension, and then sorting the
            /// sub-array based on that dimension value in each color. The colors are then iterated over
            /// until a color is found with at least the midpoint of the whole box's dimension midpoint.
            /// </summary>
            /// <returns>the index of the colors array to split from</returns>
            internal int findSplitPoint()
            {
                int longestDimension = getLongestColorDimension();
                int[] colors = parent.mColors;
                int[] hist = parent.mHistogram;
                // We need to sort the colors in this box based on the longest color dimension.
                // As we can't use a Comparator to define the sort logic, we modify each color so that
                // its most significant is the desired dimension
                modifySignificantOctet(colors, longestDimension, mLowerIndex, mUpperIndex);
                // Now sort... Arrays.sort uses a exclusive toIndex so we need to add 1
                Array.Sort(colors, mLowerIndex, mUpperIndex + 1 - mLowerIndex);
                // Now revert all of the colors so that they are packed as RGB again
                modifySignificantOctet(colors, longestDimension, mLowerIndex, mUpperIndex);
                int midPoint = mPopulation / 2;
                for (int i = mLowerIndex, count = 0; i <= mUpperIndex; i++)
                {
                    count += hist[colors[i]];
                    if (count >= midPoint)
                    {
                        // we never want to split on the upperIndex, as this will result in the same
                        // box
                        return Math.Min(mUpperIndex - 1, i);
                    }
                }
                return mLowerIndex;
            }

            /// <returns>the average color of this box.</returns>
            internal Palette.Swatch getAverageColor()
            {
                int[] colors = parent.mColors;
                int[] hist = parent.mHistogram;
                int redSum = 0;
                int greenSum = 0;
                int blueSum = 0;
                int totalPopulation = 0;
                for (int i = mLowerIndex; i <= mUpperIndex; i++)
                {
                    int color = colors[i];
                    int colorPopulation = hist[color];
                    totalPopulation += colorPopulation;
                    redSum += colorPopulation * quantizedRed(color);
                    greenSum += colorPopulation * quantizedGreen(color);
                    blueSum += colorPopulation * quantizedBlue(color);
                }
                int redMean = (int)Math.Round(redSum / (float)totalPopulation);
                int greenMean = (int)Math.Round(greenSum / (float)totalPopulation);
                int blueMean = (int)Math.Round(blueSum / (float)totalPopulation);
                return new Palette.Swatch(approximateToRgb888(redMean, greenMean, blueMean), totalPopulation);
            }

            public int CompareTo(Vbox other)
            {
                if (other == null) return 1;

                return getVolume().CompareTo(other.getVolume());
            }
        }

        internal class VolumeComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                if (y - x > 0) return 1;
                else if (y - x < 0) return -1;
                else return 0;
            }
        }

        static void modifySignificantOctet(int[] a, int dimension,
            int lower, int upper)
        {
            switch (dimension)
            {
                case COMPONENT_RED:
                    // Already in RGB, no need to do anything
                    break;
                case COMPONENT_GREEN:
                    // We need to do a RGB to GRB swap, or vice-versa
                    for (int i = lower; i <= upper; i++)
                    {
                        int color = a[i];
                        a[i] = quantizedGreen(color) << (QUANTIZE_WORD_WIDTH + QUANTIZE_WORD_WIDTH)
                                | quantizedRed(color) << QUANTIZE_WORD_WIDTH
                                | quantizedBlue(color);
                    }
                    break;
                case COMPONENT_BLUE:
                    // We need to do a RGB to BGR swap, or vice-versa
                    for (int i = lower; i <= upper; i++)
                    {
                        int color = a[i];
                        a[i] = quantizedBlue(color) << (QUANTIZE_WORD_WIDTH + QUANTIZE_WORD_WIDTH)
                                | quantizedGreen(color) << QUANTIZE_WORD_WIDTH
                                | quantizedRed(color);
                    }
                    break;
            }
        }
        private bool shouldIgnoreColor(int color565)
        {
            int rgb = approximateToRgb888(color565);
            ColorUtils.colorToHSL(rgb, mTempHsl);
            return shouldIgnoreColor(rgb, mTempHsl);
        }
        private bool shouldIgnoreColor(Palette.Swatch color)
        {
            return shouldIgnoreColor(color.GetRgb(), color.GetHsl());
        }
        private bool shouldIgnoreColor(int rgb, float[] hsl)
        {
            if (mFilters != null && mFilters.Length > 0)
            {
                for (int i = 0, count = mFilters.Length; i < count; i++)
                {
                    if (!mFilters[i].IsAllowed(rgb, hsl))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Quantized a RGB888 value to have a word width of <see cref="QUANTIZE_WORD_WIDTH"/>.
        /// </summary>
        private static int quantizeFromRgb888(int color)
        {
            int r = modifyWordWidth(ColorUtils.Red(color), 8, QUANTIZE_WORD_WIDTH);
            int g = modifyWordWidth(ColorUtils.Green(color), 8, QUANTIZE_WORD_WIDTH);
            int b = modifyWordWidth(ColorUtils.Blue(color), 8, QUANTIZE_WORD_WIDTH);
            return r << (QUANTIZE_WORD_WIDTH + QUANTIZE_WORD_WIDTH) | g << QUANTIZE_WORD_WIDTH | b;
        }

        /// <summary>
        /// Quantized RGB888 values to have a word width of <see cref="QUANTIZE_WORD_WIDTH"/>.
        /// </summary>
        static int approximateToRgb888(int r, int g, int b)
        {
            return ColorUtils.Rgb(modifyWordWidth(r, QUANTIZE_WORD_WIDTH, 8),
                    modifyWordWidth(g, QUANTIZE_WORD_WIDTH, 8),
                    modifyWordWidth(b, QUANTIZE_WORD_WIDTH, 8));
        }
        private static int approximateToRgb888(int color)
        {
            return approximateToRgb888(quantizedRed(color), quantizedGreen(color), quantizedBlue(color));
        }

        /// <returns>red component of the quantized color</returns>
        static int quantizedRed(int color)
        {
            return (color >> (QUANTIZE_WORD_WIDTH + QUANTIZE_WORD_WIDTH)) & QUANTIZE_WORD_MASK;
        }

        /// <returns>green component of a quantized color</returns>
        static int quantizedGreen(int color)
        {
            return (color >> QUANTIZE_WORD_WIDTH) & QUANTIZE_WORD_MASK;
        }

        /// <returns>blue component of a quantized color</returns>
        static int quantizedBlue(int color)
        {
            return color & QUANTIZE_WORD_MASK;
        }

        private static int modifyWordWidth(int value, int currentWidth, int targetWidth)
        {
            int newValue;
            if (targetWidth > currentWidth)
            {
                // If we're approximating up in word width, we'll shift up
                newValue = value << (targetWidth - currentWidth);
            }
            else
            {
                // Else, we will just shift and keep the MSB
                newValue = value >> (currentWidth - targetWidth);
            }
            return newValue & ((1 << targetWidth) - 1);
        }
    }
}
