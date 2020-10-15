using System;
using System.Collections.Generic;
using System.Text;

namespace Palette
{
    public sealed class Target
    {
        private const float TARGET_DARK_LUMA = 0.26f;
        private const float MAX_DARK_LUMA = 0.45f;
        private const float MIN_LIGHT_LUMA = 0.55f;
        private const float TARGET_LIGHT_LUMA = 0.74f;
        private const float MIN_NORMAL_LUMA = 0.3f;
        private const float TARGET_NORMAL_LUMA = 0.5f;
        private const float MAX_NORMAL_LUMA = 0.7f;
        private const float TARGET_MUTED_SATURATION = 0.3f;
        private const float MAX_MUTED_SATURATION = 0.4f;
        private const float TARGET_VIBRANT_SATURATION = 1f;
        private const float MIN_VIBRANT_SATURATION = 0.35f;
        private const float WEIGHT_SATURATION = 0.24f;
        private const float WEIGHT_LUMA = 0.52f;
        private const float WEIGHT_POPULATION = 0.24f;
        static readonly int INDEX_MIN = 0;
        static readonly int INDEX_TARGET = 1;
        static readonly int INDEX_MAX = 2;
        static readonly int INDEX_WEIGHT_SAT = 0;
        static readonly int INDEX_WEIGHT_LUMA = 1;
        static readonly int INDEX_WEIGHT_POP = 2;
        /**
         * A target which has the characteristics of a vibrant color which is light in luminance.
        */
        public static readonly Target LIGHT_VIBRANT;
        /**
         * A target which has the characteristics of a vibrant color which is neither light or dark.
         */
        public static readonly Target VIBRANT;
        /**
         * A target which has the characteristics of a vibrant color which is dark in luminance.
         */
        public static readonly Target DARK_VIBRANT;
        /**
         * A target which has the characteristics of a muted color which is light in luminance.
         */
        public static readonly Target LIGHT_MUTED;
        /**
         * A target which has the characteristics of a muted color which is neither light or dark.
         */
        public static readonly Target MUTED;
        /**
         * A target which has the characteristics of a muted color which is dark in luminance.
         */
        public static readonly Target DARK_MUTED;

        readonly float[] SaturationTargets = new float[3];
        readonly float[] LightnessTargets = new float[3];
        readonly float[] Weights = new float[3];

        /// <summary>
        /// Whether any color selected for this target is exclusive for this target only.
        ///
        /// <para>If false, then the color can be selected for other targets.</para>
        /// </summary>
        public bool IsExclusive = true; // default to true

        static Target()
        {
            LIGHT_VIBRANT = new Target();
            SetDefaultLightLightnessValues(LIGHT_VIBRANT);
            SetDefaultVibrantSaturationValues(LIGHT_VIBRANT);
            VIBRANT = new Target();
            SetDefaultNormalLightnessValues(VIBRANT);
            SetDefaultVibrantSaturationValues(VIBRANT);
            DARK_VIBRANT = new Target();
            SetDefaultDarkLightnessValues(DARK_VIBRANT);
            SetDefaultVibrantSaturationValues(DARK_VIBRANT);
            LIGHT_MUTED = new Target();
            SetDefaultLightLightnessValues(LIGHT_MUTED);
            SetDefaultMutedSaturationValues(LIGHT_MUTED);
            MUTED = new Target();
            SetDefaultNormalLightnessValues(MUTED);
            SetDefaultMutedSaturationValues(MUTED);
            DARK_MUTED = new Target();
            SetDefaultDarkLightnessValues(DARK_MUTED);
            SetDefaultMutedSaturationValues(DARK_MUTED);
        }
        Target()
        {
            SetTargetDefaultValues(SaturationTargets);
            SetTargetDefaultValues(LightnessTargets);
            SetDefaultWeights();
        }

        Target(Target From)
        {
            Array.Copy(From.SaturationTargets, SaturationTargets, SaturationTargets.Length);
            Array.Copy(From.LightnessTargets, LightnessTargets, LightnessTargets.Length);
            Array.Copy(From.Weights, Weights, Weights.Length);
        }

        /// <summary>
        /// The minimum saturation value for this target.
        /// </summary>
        public float GetMinimumSaturation()
        {
            return SaturationTargets[INDEX_MIN];
        }

        /// <summary>
        /// The target saturation value for this target.
        /// </summary>
        public float GetTargetSaturation()
        {
            return SaturationTargets[INDEX_TARGET];
        }

        /// <summary>
        /// The maximum saturation value for this target.
        /// </summary>
        public float GetMaximumSaturation()
        {
            return SaturationTargets[INDEX_MAX];
        }

        /// <summary>
        /// The minimum lightness value for this target.
        /// </summary>
        public float GetMinimumLightness()
        {
            return LightnessTargets[INDEX_MIN];
        }

        /// <summary>
        /// The target lightness value for this target.
        /// </summary>
        public float GetTargetLightness()
        {
            return LightnessTargets[INDEX_TARGET];
        }

        /// <summary>
        /// The maximum lightness value for this target.
        /// </summary>
        public float GetMaximumLightness()
        {
            return LightnessTargets[INDEX_MAX];
        }

        /// <summary>
        /// Returns the weight of importance that this target places on a color's saturation within
        /// the image.
        ///
        /// <para>The larger the weight, relative to the other weights, the more important that a color
        /// being close to the target value has on selection.</para>
        ///
        /// <see cref="GetTargetSaturation()"/>
        /// </summary>
        public float GetSaturationWeight()
        {
            return Weights[INDEX_WEIGHT_SAT];
        }


        /// <summary>
        /// Returns the weight of importance that this target places on a color's lightness within
        /// the image.
        ///
        /// <para>The larger the weight, relative to the other weights, the more important that a color
        /// being close to the target value has on selection.</para>
        ///
        /// <see cref="GetTargetLightness()"/>
        /// </summary>
        public float GetLightnessWeight()
        {
            return Weights[INDEX_WEIGHT_LUMA];
        }


        /// <summary>
        /// Returns the weight of importance that this target places on a color's population within
        /// the image.
        ///
        /// <para>The larger the weight, relative to the other weights, the more important that a
        /// color's population being close to the most populous has on selection.</para>
        /// </summary>
        public float GetPopulationWeight()
        {
            return Weights[INDEX_WEIGHT_POP];
        }

        private static void SetTargetDefaultValues(float[] values)
        {
            values[INDEX_MIN] = 0f;
            values[INDEX_TARGET] = 0.5f;
            values[INDEX_MAX] = 1f;
        }

        private void SetDefaultWeights()
        {
            Weights[INDEX_WEIGHT_SAT] = WEIGHT_SATURATION;
            Weights[INDEX_WEIGHT_LUMA] = WEIGHT_LUMA;
            Weights[INDEX_WEIGHT_POP] = WEIGHT_POPULATION;
        }
        public void NormalizeWeights()
        {
            float sum = 0;
            for (int i = 0, z = Weights.Length; i < z; i++)
            {
                float weight = Weights[i];
                if (weight > 0)
                {
                    sum += weight;
                }
            }
            if (sum != 0)
            {
                for (int i = 0, z = Weights.Length; i < z; i++)
                {
                    if (Weights[i] > 0)
                    {
                        Weights[i] /= sum;
                    }
                }
            }
        }
        private static void SetDefaultDarkLightnessValues(Target target)
        {
            target.LightnessTargets[INDEX_TARGET] = TARGET_DARK_LUMA;
            target.LightnessTargets[INDEX_MAX] = MAX_DARK_LUMA;
        }

        private static void SetDefaultNormalLightnessValues(Target target)
        {
            target.LightnessTargets[INDEX_MIN] = MIN_NORMAL_LUMA;
            target.LightnessTargets[INDEX_TARGET] = TARGET_NORMAL_LUMA;
            target.LightnessTargets[INDEX_MAX] = MAX_NORMAL_LUMA;
        }

        private static void SetDefaultLightLightnessValues(Target target)
        {
            target.LightnessTargets[INDEX_MIN] = MIN_LIGHT_LUMA;
            target.LightnessTargets[INDEX_TARGET] = TARGET_LIGHT_LUMA;
        }

        private static void SetDefaultVibrantSaturationValues(Target target)
        {
            target.SaturationTargets[INDEX_MIN] = MIN_VIBRANT_SATURATION;
            target.SaturationTargets[INDEX_TARGET] = TARGET_VIBRANT_SATURATION;
        }

        private static void SetDefaultMutedSaturationValues(Target target)
        {
            target.SaturationTargets[INDEX_TARGET] = TARGET_MUTED_SATURATION;
            target.SaturationTargets[INDEX_MAX] = MAX_MUTED_SATURATION;
        }

        /// <summary>
        /// Builder class for generating custom <see cref="Target"/> instances.
        /// </summary>
        public sealed class Builder
        {
            private Target target;

            /// <summary>
            /// Create a new <see cref="Target"/> builder from scratch.
            /// </summary>
            public Builder()
            {
                target = new Target();
            }
            /// <summary>
            /// Create a new builder based on an existing <see cref="Target"/>.
            /// </summary>
            public Builder(Target target)
            {
                this.target = new Target(target);
            }

            /// <summary>
            /// Set the minimum saturation value for this target.
            /// </summary>
            public Builder SetMinimumSaturation(float value)
            {
                target.SaturationTargets[INDEX_MIN] = value;
                return this;
            }

            /// <summary>
            /// Set the target/ideal saturation value for this target.
            /// </summary>
            public Builder SetTargetSaturation(float value)
            {
                target.SaturationTargets[INDEX_TARGET] = value;
                return this;
            }

            /// <summary>
            /// Set the maximum saturation value for this target.
            /// </summary>
            public Builder SetMaximumSaturation(float value)
            {
                target.SaturationTargets[INDEX_MAX] = value;
                return this;
            }

            /// <summary>
            /// Set the minimum lightness value for this target.
            /// </summary>
            public Builder SetMinimumLightness(float value)
            {
                target.LightnessTargets[INDEX_MIN] = value;
                return this;
            }

            /// <summary>
            /// Set the target/ideal lightness value for this target.
            /// </summary>
            public Builder SetTargetLightness(float value)
            {
                target.LightnessTargets[INDEX_TARGET] = value;
                return this;
            }

            /// <summary>
            /// Set the maximum lightness value for this target.
            /// </summary>
            public Builder SetMaximumLightness(float value)
            {
                target.LightnessTargets[INDEX_MAX] = value;
                return this;
            }

            /// <summary>
            /// Set the weight of importance that this target will place on saturation values.
            ///
            /// <para>The larger the weight, relative to the other weights, the more important that a color
            /// being close to the target value has on selection.</para>
            ///
            /// <para>A weight of 0 means that it has no weight, and thus has no
            /// bearing on the selection.</para>
            ///
            /// <see cref="SetTargetSaturation"/>
            /// </summary>
            public Builder SetSaturationWeight(float weight)
            {
                target.Weights[INDEX_WEIGHT_SAT] = weight;
                return this;
            }

            /// <summary>
            /// Set the weight of importance that this target will place on lightness values.
            ///
            /// <para>The larger the weight, relative to the other weights, the more important that a color
            /// being close to the target value has on selection.</para>
            ///
            /// <para>A weight of 0 means that it has no weight, and thus has no
            /// bearing on the selection.</para>
            ///
            /// <see cref="SetTargetLightness"/>
            /// </summary>
            public Builder SetLightnessWeight(float weight)
            {
                target.Weights[INDEX_WEIGHT_LUMA] = weight;
                return this;
            }
            /// <summary>
            /// Set the weight of importance that this target will place on a color's population within
            /// the image.
            ///
            /// <para>The larger the weight, relative to the other weights, the more important that a
            /// color's population being close to the most populous has on selection.</para>
            ///
            /// <para>A weight of 0 means that it has no weight, and thus has no
            /// bearing on the selection.</para>
            /// </summary>
            public Builder SetPopulationWeight(float weight)
            {
                target.Weights[INDEX_WEIGHT_POP] = weight;
                return this;
            }

            /// <summary>
            /// Set whether any color selected for this target is exclusive to this target only.
            /// Defaults to true.
            /// </summary>
            /// <param name="exclusive">
            /// true if any the color is exclusive to this target, or false is the
            /// color can be selected for other targets.
            /// </param>
            public Builder SetExclusive(bool exclusive)
            {
                target.IsExclusive = exclusive;
                return this;
            }

            /// <summary>
            /// Builds and returns the resulting <see cref="Target"/>.
            /// </summary>
            public Target Build()
            {
                return target;
            }
        }
    }
}
