using System;
using System.Collections.Generic;

namespace ConfocalKonfigurator
{
    internal enum SpectralEvidence
    {
        Measured,
        Representative
    }

    internal sealed class SpectralProfile
    {
        public int[] Wavelengths;
        public int[] RelativeIntensity;
        public SpectralEvidence Evidence;
        public string Source;

        public SpectralProfile(int[] wavelengths, int[] relativeIntensity, SpectralEvidence evidence, string source)
        {
            Wavelengths = wavelengths;
            RelativeIntensity = relativeIntensity;
            Evidence = evidence;
            Source = source;
        }

        public double IntensityAt(int wavelength)
        {
            if (Wavelengths.Length == 0 || wavelength < Wavelengths[0] ||
                wavelength > Wavelengths[Wavelengths.Length - 1])
            {
                return 0.0;
            }

            int i;
            for (i = 1; i < Wavelengths.Length; i++)
            {
                if (wavelength <= Wavelengths[i])
                {
                    int leftWavelength = Wavelengths[i - 1];
                    int rightWavelength = Wavelengths[i];
                    double fraction = (wavelength - leftWavelength) /
                        (double)(rightWavelength - leftWavelength);
                    return RelativeIntensity[i - 1] +
                        (RelativeIntensity[i] - RelativeIntensity[i - 1]) * fraction;
                }
            }
            return RelativeIntensity[RelativeIntensity.Length - 1];
        }

        public double FractionPassed(EmissionFilter filter)
        {
            double total = 0.0;
            double passed = 0.0;
            int wavelength;
            for (wavelength = 350; wavelength <= 850; wavelength += 2)
            {
                double intensity = IntensityAt(wavelength);
                total += intensity;
                if (filter.Passes(wavelength))
                {
                    passed += intensity;
                }
            }
            return total <= 0.0 ? 0.0 : passed / total;
        }
    }

    internal static partial class SpectralProfileLibrary
    {
        private static readonly int[] BlueDnaOffsets =
            new int[] { -85, -65, -48, -34, -22, -11, 0, 12, 26, 43, 64, 90, 122, 160 };
        private static readonly int[] BlueDnaValues =
            new int[] { 0, 8, 35, 110, 300, 690, 1000, 910, 750, 560, 370, 205, 85, 10 };
        private static readonly int[] BlueOffsets =
            new int[] { -65, -45, -30, -20, -10, 0, 12, 25, 42, 62, 88, 115 };
        private static readonly int[] BlueValues =
            new int[] { 0, 15, 75, 230, 620, 1000, 850, 620, 390, 190, 55, 0 };
        private static readonly int[] GreenOffsets =
            new int[] { -48, -34, -24, -15, -7, 0, 10, 22, 38, 58, 82, 108 };
        private static readonly int[] GreenValues =
            new int[] { 0, 18, 90, 310, 700, 1000, 860, 650, 430, 235, 85, 10 };
        private static readonly int[] YellowGreenOffsets =
            new int[] { -52, -36, -25, -15, -7, 0, 11, 24, 41, 62, 88, 118 };
        private static readonly int[] YellowGreenValues =
            new int[] { 0, 15, 80, 300, 690, 1000, 875, 680, 475, 285, 120, 15 };
        private static readonly int[] OrangeOffsets =
            new int[] { -58, -40, -28, -17, -8, 0, 12, 27, 45, 67, 95, 128 };
        private static readonly int[] OrangeValues =
            new int[] { 0, 12, 65, 255, 650, 1000, 890, 700, 500, 305, 135, 15 };
        private static readonly int[] RedOffsets =
            new int[] { -66, -46, -31, -19, -9, 0, 13, 29, 49, 74, 105, 142 };
        private static readonly int[] RedValues =
            new int[] { 0, 10, 55, 230, 625, 1000, 910, 735, 535, 335, 155, 18 };
        private static readonly int[] FarRedOffsets =
            new int[] { -72, -50, -34, -21, -10, 0, 14, 31, 52, 78, 110, 148 };
        private static readonly int[] FarRedValues =
            new int[] { 0, 12, 70, 270, 665, 1000, 905, 720, 500, 285, 115, 10 };
        private static readonly int[] BroadRedOffsets =
            new int[] { -180, -140, -105, -75, -48, -25, 0, 32, 70, 115, 170 };
        private static readonly int[] BroadRedValues =
            new int[] { 0, 35, 120, 280, 520, 790, 1000, 920, 720, 430, 80 };

        public static void AttachProfiles(List<Fluorophore> dyes)
        {
            int i;
            for (i = 0; i < dyes.Count; i++)
            {
                AttachProfile(dyes[i]);
            }
        }

        private static void AttachProfile(Fluorophore dye)
        {
            SpectralProfile measured = MeasuredFluorescentProteinProfile(dye.Name);
            if (measured == null)
            {
                measured = MeasuredChemicalDyeProfile(dye.Name);
            }
            if (measured != null)
            {
                dye.EmissionProfile = measured;
            }
            else if (dye.Name == "DAPI" || dye.Name == "Hoechst 33342" || dye.Name == "Hoechst 33258")
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, BlueDnaOffsets, BlueDnaValues,
                    "conservative DNA-dye reference envelope");
            }
            else if (dye.Name == "Alexa Fluor 405" || dye.Name == "Pacific Blue")
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, BlueOffsets, BlueValues,
                    "manufacturer-representative blue-dye envelope");
            }
            else if (dye.Name == "FM 4-64")
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, BroadRedOffsets, BroadRedValues,
                    "conservative broad FM 4-64 envelope");
            }
            else if (dye.Name == "Alexa Fluor 680")
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, FarRedOffsets, FarRedValues,
                    "manufacturer-representative near-IR dye envelope");
            }
            else if (dye.EmissionPeak >= 640)
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, FarRedOffsets, FarRedValues,
                    "manufacturer-representative far-red dye envelope");
            }
            else if (dye.EmissionPeak >= 590)
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, RedOffsets, RedValues,
                    "manufacturer-representative red dye envelope");
            }
            else if (dye.EmissionPeak >= 545)
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, OrangeOffsets, OrangeValues,
                    "manufacturer-representative orange dye envelope");
            }
            else if (dye.EmissionPeak >= 530)
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, YellowGreenOffsets, YellowGreenValues,
                    "manufacturer-representative yellow-green dye envelope");
            }
            else
            {
                dye.EmissionProfile = CreateRepresentative(dye.EmissionPeak, GreenOffsets, GreenValues,
                    "manufacturer-representative green dye envelope");
            }

            AssignEnglishNotes(dye);
        }

        private static SpectralProfile CreateRepresentative(int peak, int[] offsets, int[] values, string source)
        {
            int[] wavelengths = new int[offsets.Length];
            int i;
            for (i = 0; i < offsets.Length; i++)
            {
                wavelengths[i] = peak + offsets[i];
            }
            return new SpectralProfile(wavelengths, values, SpectralEvidence.Representative, source);
        }

        private static SpectralProfile CreateMeasured(int start, int step, int[] values, string source)
        {
            int[] wavelengths = new int[values.Length];
            int i;
            for (i = 0; i < values.Length; i++)
            {
                wavelengths[i] = start + i * step;
            }
            return new SpectralProfile(wavelengths, values, SpectralEvidence.Measured, source);
        }

        private static void AssignEnglishNotes(Fluorophore dye)
        {
            dye.EnglishGeneralNote = String.Empty;
            dye.EnglishPascalNote = String.Empty;
            dye.EnglishLiveNote = String.Empty;

            if (dye.Name == "GFP" || dye.Name == "FITC" || dye.Name == "Alexa Fluor 488" ||
                dye.Name == "Cy2" || dye.Name == "Calcein" || dye.Name == "Fluo-4" ||
                dye.Name == "Fluo-8" || dye.Name == "SYTOX Green" || dye.Name == "MitoTracker Green" ||
                dye.Name == "BODIPY FL")
            {
                dye.EnglishGeneralNote = "Its emission strongly overlaps other GFP/FITC-family labels; do not treat those labels as independent colours.";
            }
            if (dye.Name == "YFP" || dye.Name == "EYFP" || dye.Name == "Venus" || dye.Name == "Citrine")
            {
                dye.EnglishGeneralNote = "Its colour overlaps strongly with GFP-like fluorophores. They are usually not separable with the available fixed filters; check this with single-label controls.";
                dye.EnglishPascalNote = "The 488 nm laser is not the ideal excitation wavelength, so the signal may be weaker.";
                dye.EnglishLiveNote = "The 488 nm laser is not the ideal excitation wavelength, so the signal may be weaker.";
            }
            if (dye.Name == "mNeonGreen" || dye.Name == "Alexa Fluor 514")
            {
                dye.EnglishPascalNote = "The 488 nm laser can excite this fluorophore, but it is not the most efficient wavelength.";
                dye.EnglishLiveNote = "The 488 nm laser can excite this fluorophore, but it is not the most efficient wavelength.";
            }
            if (dye.Name == "mCherry" || dye.Name == "Alexa Fluor 568" || dye.Name == "Alexa Fluor 594" ||
                dye.Name == "Texas Red" || dye.Name == "MitoTracker Red CMXRos")
            {
                dye.EnglishPascalNote = "The 543 nm laser is not the ideal excitation wavelength, so the signal may be weaker. Check the signal before starting the experiment.";
            }
            if (dye.Name == "mKate2" || dye.Name == "mPlum")
            {
                dye.EnglishPascalNote = "The 543 nm laser is far from the ideal excitation wavelength and may give a weak signal.";
                dye.EnglishLiveNote = "The 561 nm laser is not the ideal excitation wavelength and may give a weak signal.";
            }
            if (dye.Name == "Alexa Fluor 532")
            {
                dye.EnglishLiveNote = "The 561 nm laser is not the ideal excitation wavelength and may excite this fluorophore inefficiently.";
            }
            if (dye.Name == "ECFP" || dye.Name == "Cerulean")
            {
                dye.EnglishPascalNote = "Use 458 nm. This is not the ideal excitation wavelength, so the signal may be weaker.";
                dye.EnglishLiveNote = "Use 405 nm. This is not the ideal excitation wavelength, so the signal may be weaker.";
            }
            if (dye.Name == "DAPI" || dye.Name == "Hoechst 33342" || dye.Name == "Hoechst 33258" ||
                dye.Name == "mTagBFP2" || dye.Name == "Alexa Fluor 405" || dye.Name == "Pacific Blue")
            {
                dye.EnglishLiveNote = "Use the 405 nm line.";
            }
            if (dye.Name == "Alexa Fluor 680")
            {
                dye.EnglishGeneralNote = "The 633/635 nm laser is far from the ideal excitation wavelength, so the signal may be very weak. Verify it experimentally.";
            }
            if (dye.Name == "FM 4-64")
            {
                dye.EnglishGeneralNote = "Its exceptionally broad red emission makes multicolour separation especially critical.";
            }
            if (dye.Name == "DsRed")
            {
                dye.EnglishGeneralNote = "Maturation and oligomerisation can affect signal and may produce additional spectral components.";
            }
        }
    }
}
