using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ConfocalKonfigurator
{
    internal enum MicroscopeKind
    {
        Pascal,
        Lsm5Live
    }

    internal enum AcquisitionPriority
    {
        MaximumPhotonYield,
        FastestAcquisition,
        DiagnosticBestSeparation
    }

    internal sealed class Fluorophore
    {
        public string Name;
        public string Category;
        public int ExcitationPeak;
        public int EmissionPeak;
        public int EmissionWidth;
        public int Brightness;
        public int[] PascalLasers;
        public int[] LiveLasers;
        public SpectralProfile EmissionProfile;
        public string EnglishGeneralNote;
        public string EnglishPascalNote;
        public string EnglishLiveNote;

        public Fluorophore(string name, string category, int excitationPeak, int emissionPeak,
            int emissionWidth, int brightness, int[] pascalLasers, int[] liveLasers)
        {
            Name = name;
            Category = category;
            ExcitationPeak = excitationPeak;
            EmissionPeak = emissionPeak;
            EmissionWidth = emissionWidth;
            Brightness = brightness;
            PascalLasers = pascalLasers;
            LiveLasers = liveLasers;
            EnglishGeneralNote = String.Empty;
            EnglishPascalNote = String.Empty;
            EnglishLiveNote = String.Empty;
        }

        public bool IsAvailable(MicroscopeKind kind)
        {
            return GetLasers(kind).Length > 0;
        }

        public int[] GetLasers(MicroscopeKind kind)
        {
            return kind == MicroscopeKind.Pascal ? PascalLasers : LiveLasers;
        }

        public int PreferredLaser(MicroscopeKind kind)
        {
            int[] lasers = GetLasers(kind);
            return lasers.Length == 0 ? 0 : lasers[0];
        }

        public override string ToString()
        {
            return "[" + Category + "] " + Name + "  (Ex " + ExcitationPeak + " / Em " + EmissionPeak + " nm)";
        }
    }

    internal sealed class SpectralBand
    {
        public int Start;
        public int End;

        public SpectralBand(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool Contains(int wavelength)
        {
            return wavelength >= Start && wavelength <= End;
        }
    }

    internal sealed class EmissionFilter
    {
        public string Name;
        public List<SpectralBand> Bands;

        public EmissionFilter(string name, params SpectralBand[] bands)
        {
            Name = name;
            Bands = new List<SpectralBand>();
            int i;
            for (i = 0; i < bands.Length; i++)
            {
                Bands.Add(bands[i]);
            }
        }

        public bool Passes(int wavelength)
        {
            int i;
            for (i = 0; i < Bands.Count; i++)
            {
                if (Bands[i].Contains(wavelength))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class MicroscopeDefinition
    {
        public MicroscopeKind Kind;
        public string DisplayName;
        public int[] Lasers;
        public List<EmissionFilter> Channel1Filters;
        public List<EmissionFilter> Channel2Filters;
        public int[] Splitters;

        public MicroscopeDefinition(MicroscopeKind kind, string displayName, int[] lasers, int[] splitters)
        {
            Kind = kind;
            DisplayName = displayName;
            Lasers = lasers;
            Splitters = splitters;
            Channel1Filters = new List<EmissionFilter>();
            Channel2Filters = new List<EmissionFilter>();
        }
    }

    internal sealed class TrackConfiguration
    {
        public List<Fluorophore> Dyes;
        public Fluorophore Channel1Dye;
        public Fluorophore Channel2Dye;
        public EmissionFilter Channel1Filter;
        public EmissionFilter Channel2Filter;
        public List<int> Lasers;
        public string MainSplitter;
        public string SecondarySplitter;
        public string PlateSetting;
        public string FilterSetSetting;
        public string RearSetting;
        public string Reason;
        public double Score;
        public double SpillIntoChannel1;
        public double SpillIntoChannel2;
        public double SignalInChannel1;
        public double SignalInChannel2;
        public double SameLaserInterference;
        public double YieldRetentionInChannel1;
        public double YieldRetentionInChannel2;
        public double BleedThroughIntoChannel1;
        public double BleedThroughIntoChannel2;
        public bool MeetsQualityLimits;
        public bool UsesRepresentativeProfile;
        public bool UsesCombinedLiveLaserRejection;

        public TrackConfiguration()
        {
            Dyes = new List<Fluorophore>();
            Lasers = new List<int>();
            // The lower Pascal filter-set position shown in the supplied dialog
            // remains bypassed so that no undocumented filter set costs signal.
            FilterSetSetting = "None";
        }

        public bool IsParallel()
        {
            return Dyes.Count == 2;
        }
    }

    internal sealed class PlanCandidate
    {
        public List<TrackConfiguration> Tracks;
        public double Score;
        public double TotalTargetSignal;
        public double TotalInterference;
        public double MinimumYieldRetention;
        public double MaximumBleedThrough;
        public bool MeetsQualityLimits;
        public AcquisitionPriority Priority;

        public PlanCandidate()
        {
            Tracks = new List<TrackConfiguration>();
        }
    }

    internal static class Catalog
    {
        public static List<Fluorophore> CreateFluorophores()
        {
            List<Fluorophore> dyes = new List<Fluorophore>();

            // Fluorescent proteins. Values are approximate peak wavelengths and are used for a
            // conservative configuration recommendation, not as a substitute for a spectral scan.
            Add(dyes, "EGFP", "Protein", 488, 509, 54, 3, A(488), A(488));
            Add(dyes, "GFP", "Protein", 488, 509, 58, 2, A(488), A(488));
            Add(dyes, "Emerald GFP", "Protein", 487, 509, 54, 3, A(488), A(488));
            Add(dyes, "mNeonGreen", "Protein", 506, 517, 55, 4, A(488), A(488));
            Add(dyes, "YFP", "Protein", 514, 527, 58, 2, A(488), A(488));
            Add(dyes, "EYFP", "Protein", 514, 527, 58, 3, A(488), A(488));
            Add(dyes, "Venus", "Protein", 515, 528, 56, 3, A(488), A(488));
            Add(dyes, "Citrine", "Protein", 516, 529, 56, 3, A(488), A(488));
            Add(dyes, "mOrange", "Protein", 548, 562, 58, 3, A(543), A(561));
            Add(dyes, "mOrange2", "Protein", 549, 565, 58, 3, A(543), A(561));
            Add(dyes, "DsRed", "Protein", 558, 583, 62, 2, A(543), A(561));
            Add(dyes, "DsRed2", "Protein", 563, 583, 60, 2, A(543), A(561));
            Add(dyes, "tdTomato", "Protein", 554, 581, 58, 4, A(543), A(561));
            Add(dyes, "TagRFP", "Protein", 555, 584, 58, 3, A(543), A(561));
            Add(dyes, "mRuby2", "Protein", 559, 600, 60, 3, A(543), A(561));
            Add(dyes, "mCherry", "Protein", 587, 610, 60, 3, A(543), A(561));
            Add(dyes, "mKate2", "Protein", 588, 635, 62, 2, A(543), A(561));
            Add(dyes, "mPlum", "Protein", 590, 649, 65, 2, A(543), A(561));
            Add(dyes, "iRFP670", "Protein", 643, 670, 66, 2, A(633), A(635));
            Add(dyes, "mTagBFP2", "Protein", 399, 456, 50, 3, A(), A(405));
            Add(dyes, "ECFP", "Protein", 439, 476, 54, 2, A(458), A(405));
            Add(dyes, "Cerulean", "Protein", 433, 475, 54, 3, A(458), A(405));

            // Nucleic-acid dyes and small-molecule fluorophores.
            Add(dyes, "DAPI", "DNA dye", 359, 461, 55, 3, A(), A(405));
            Add(dyes, "Hoechst 33342", "DNA dye", 350, 461, 56, 3, A(), A(405));
            Add(dyes, "Hoechst 33258", "DNA dye", 352, 461, 56, 2, A(), A(405));
            Add(dyes, "Alexa Fluor 405", "Alexa Fluor", 402, 421, 42, 3, A(), A(405));
            Add(dyes, "Pacific Blue", "Dye", 410, 455, 50, 3, A(), A(405));
            Add(dyes, "FITC", "Dye", 495, 519, 62, 3, A(488), A(488));
            Add(dyes, "Alexa Fluor 488", "Alexa Fluor", 495, 519, 60, 4, A(488), A(488));
            Add(dyes, "Alexa Fluor 514", "Alexa Fluor", 518, 540, 60, 3, A(488), A(488));
            Add(dyes, "Alexa Fluor 532", "Alexa Fluor", 531, 554, 60, 3, A(543), A(561));
            Add(dyes, "Alexa Fluor 546", "Alexa Fluor", 556, 573, 60, 3, A(543), A(561));
            Add(dyes, "Alexa Fluor 555", "Alexa Fluor", 555, 565, 60, 4, A(543), A(561));
            Add(dyes, "Alexa Fluor 568", "Alexa Fluor", 578, 603, 62, 3, A(543), A(561));
            Add(dyes, "Alexa Fluor 594", "Alexa Fluor", 590, 617, 62, 3, A(543), A(561));
            Add(dyes, "Alexa Fluor 647", "Alexa Fluor", 650, 668, 64, 4, A(633), A(635));
            Add(dyes, "Alexa Fluor 680", "Alexa Fluor", 679, 702, 66, 2, A(633), A(635));
            Add(dyes, "Cy2", "Cyanine", 489, 506, 58, 3, A(488), A(488));
            Add(dyes, "Cy3", "Cyanine", 550, 570, 60, 3, A(543), A(561));
            Add(dyes, "Cy5", "Cyanine", 650, 670, 65, 3, A(633), A(635));
            Add(dyes, "TRITC", "Dye", 555, 576, 62, 3, A(543), A(561));
            Add(dyes, "Texas Red", "Dye", 595, 615, 62, 3, A(543), A(561));
            Add(dyes, "Rhodamine B", "Dye", 555, 580, 62, 3, A(543), A(561));
            Add(dyes, "Calcein", "Live-cell dye", 495, 515, 58, 3, A(488), A(488));
            Add(dyes, "Fluo-4", "Calcium indicator", 494, 516, 58, 3, A(488), A(488));
            Add(dyes, "Fluo-8", "Calcium indicator", 490, 514, 58, 3, A(488), A(488));
            Add(dyes, "Propidium iodide", "DNA dye", 535, 617, 68, 2, A(543), A(561));
            Add(dyes, "SYTOX Green", "DNA dye", 504, 523, 60, 3, A(488), A(488));
            Add(dyes, "SYTOX Orange", "DNA dye", 547, 570, 60, 3, A(543), A(561));
            Add(dyes, "SYTOX Red", "DNA dye", 640, 658, 64, 3, A(633), A(635));
            Add(dyes, "MitoTracker Green", "Live-cell dye", 490, 516, 58, 3, A(488), A(488));
            Add(dyes, "MitoTracker Red CMXRos", "Live-cell dye", 579, 599, 62, 3, A(543), A(561));
            Add(dyes, "CellTracker Orange", "Live-cell dye", 548, 565, 60, 3, A(543), A(561));
            Add(dyes, "DiI", "Membrane dye", 549, 565, 62, 3, A(543), A(561));
            Add(dyes, "DiD", "Membrane dye", 644, 665, 66, 3, A(633), A(635));
            Add(dyes, "FM 4-64", "Membrane dye", 558, 734, 90, 2, A(543), A(561));
            Add(dyes, "BODIPY FL", "Dye", 503, 512, 56, 3, A(488), A(488));

            SpectralProfileLibrary.AttachProfiles(dyes);
            return dyes;
        }

        public static MicroscopeDefinition CreatePascal()
        {
            MicroscopeDefinition result = new MicroscopeDefinition(
                MicroscopeKind.Pascal,
                "Zeiss LSM Pascal",
                A(458, 488, 543, 633),
                A(490, 515, 545, 635));

            // Filter options documented in the supplied Pascal screenshots.
            result.Channel1Filters.Add(new EmissionFilter("LP 475", new SpectralBand(475, 800)));
            result.Channel1Filters.Add(new EmissionFilter("LP 505", new SpectralBand(505, 800)));
            result.Channel1Filters.Add(new EmissionFilter("LP 530", new SpectralBand(530, 800)));
            result.Channel1Filters.Add(new EmissionFilter("LP 560", new SpectralBand(560, 800)));
            result.Channel1Filters.Add(new EmissionFilter("LP 650", new SpectralBand(650, 800)));

            result.Channel2Filters.Add(new EmissionFilter("BP 475-525", new SpectralBand(475, 525)));
            result.Channel2Filters.Add(new EmissionFilter("BP 505-530", new SpectralBand(505, 530)));
            result.Channel2Filters.Add(new EmissionFilter("BP 505-600", new SpectralBand(505, 600)));
            result.Channel2Filters.Add(new EmissionFilter("BP 530-600", new SpectralBand(530, 600)));
            result.Channel2Filters.Add(new EmissionFilter("BP 560-615", new SpectralBand(560, 615)));
            return result;
        }

        public static MicroscopeDefinition CreateLive()
        {
            MicroscopeDefinition result = new MicroscopeDefinition(
                MicroscopeKind.Lsm5Live,
                "Zeiss LSM 5 Live",
                A(405, 488, 561, 635),
                A(490, 515, 535, 565, 635));

            // Filter options documented in the supplied LSM 5 Live screenshots.
            result.Channel1Filters.Add(new EmissionFilter("BP 415-525", new SpectralBand(415, 525)));
            result.Channel1Filters.Add(new EmissionFilter("BP 445-525", new SpectralBand(445, 525)));
            result.Channel1Filters.Add(new EmissionFilter("LP 495", new SpectralBand(495, 800)));
            result.Channel1Filters.Add(new EmissionFilter("BP 495-520 + BP 550-615 IR", new SpectralBand(495, 520), new SpectralBand(550, 615)));
            result.Channel1Filters.Add(new EmissionFilter("BP 575-615 + LP 655", new SpectralBand(575, 615), new SpectralBand(655, 800)));
            result.Channel1Filters.Add(new EmissionFilter("BP 520-555", new SpectralBand(520, 555)));
            result.Channel1Filters.Add(new EmissionFilter("BP 665-750 IR 0°", new SpectralBand(665, 750)));
            result.Channel1Filters.Add(new EmissionFilter("BP 665-750 IR 90°", new SpectralBand(665, 750)));

            result.Channel2Filters.Add(new EmissionFilter("BP 415-480", new SpectralBand(415, 480)));
            result.Channel2Filters.Add(new EmissionFilter("BP 415-505", new SpectralBand(415, 505)));
            result.Channel2Filters.Add(new EmissionFilter("BP 420-475 + BP 500-545 IR", new SpectralBand(420, 475), new SpectralBand(500, 545)));
            result.Channel2Filters.Add(new EmissionFilter("BP 445-505", new SpectralBand(445, 505)));
            result.Channel2Filters.Add(new EmissionFilter("BP 495-555", new SpectralBand(495, 555)));
            result.Channel2Filters.Add(new EmissionFilter("BP 495-525", new SpectralBand(495, 525)));
            result.Channel2Filters.Add(new EmissionFilter("BP 540-625", new SpectralBand(540, 625)));
            result.Channel2Filters.Add(new EmissionFilter("BP 505-610 IR", new SpectralBand(505, 610)));
            return result;
        }

        private static void Add(List<Fluorophore> target, string name, string category, int excitation, int emission,
            int width, int brightness, int[] pascalLasers, int[] liveLasers)
        {
            target.Add(new Fluorophore(name, category, excitation, emission, width, brightness, pascalLasers, liveLasers));
        }

        private static int[] A(params int[] values)
        {
            return values;
        }
    }

    internal sealed class RecommendationEngine
    {
        internal const double MinimumYieldRetention = 0.50;
        internal const double MaximumBleedThrough = 0.10;
        private const double MinimumAbsoluteCollection = 0.25;

        private MicroscopeDefinition microscope;
        private Dictionary<string, double> bestIsolatedSignalCache;

        public RecommendationEngine(MicroscopeDefinition definition)
        {
            microscope = definition;
            bestIsolatedSignalCache = new Dictionary<string, double>();
        }

        public PlanCandidate MakePlan(List<Fluorophore> dyes)
        {
            return MakePlan(dyes, AcquisitionPriority.MaximumPhotonYield);
        }

        public PlanCandidate MakePlan(List<Fluorophore> dyes, AcquisitionPriority priority)
        {
            PlanCandidate best = null;
            BuildPlans(dyes, dyes, new List<TrackConfiguration>(), priority, ref best);
            return best;
        }

        public List<string> FindBlockingPairMessages(List<Fluorophore> dyes)
        {
            List<string> result = new List<string>();
            int i;
            int j;
            for (i = 0; i < dyes.Count; i++)
            {
                for (j = i + 1; j < dyes.Count; j++)
                {
                    if (IsFundamentallyUnresolvable(dyes[i], dyes[j]))
                    {
                        result.Add(dyes[i].Name + " and " + dyes[j].Name +
                            " cannot be separated reliably with the available lasers and filters. " +
                            "Even separate tracks would lose too much signal or collect too much colour overlap.");
                    }
                }
            }
            return result;
        }

        public List<string> FindRejectedParallelCandidateMessages(List<Fluorophore> dyes)
        {
            List<string> result = new List<string>();
            int i;
            int j;
            for (i = 0; i < dyes.Count; i++)
            {
                for (j = i + 1; j < dyes.Count; j++)
                {
                    if (IsFundamentallyUnresolvable(dyes[i], dyes[j]) ||
                        MakeParallelTrack(dyes[i], dyes[j], dyes,
                            AcquisitionPriority.MaximumPhotonYield) != null)
                    {
                        continue;
                    }
                    TrackConfiguration candidate = FindBestParallelCandidate(dyes[i], dyes[j], dyes,
                        AcquisitionPriority.DiagnosticBestSeparation, false);
                    if (candidate == null)
                    {
                        continue;
                    }

                    StringBuilder message = new StringBuilder();
                    message.Append(dyes[i].Name + " + " + dyes[j].Name + ": evaluated " +
                        candidate.PlateSetting + ", Ch1 " + candidate.Channel1Dye.Name + " -> " +
                        candidate.Channel1Filter.Name + ", Ch2 " + candidate.Channel2Dye.Name + " -> " +
                        candidate.Channel2Filter.Name + ". ");
                    if (candidate.UsesRepresentativeProfile)
                    {
                        message.Append("A dye-specific reference curve is missing, so the program does not promote this path to a simultaneous recommendation.");
                    }
                    else
                    {
                        message.Append("Yield retention is " +
                            Percent(candidate.YieldRetentionInChannel1) + " in Ch1 and " +
                            Percent(candidate.YieldRetentionInChannel2) + " in Ch2; modeled same-dye bleed is " +
                            Percent(candidate.BleedThroughIntoChannel1) + " into Ch1 and " +
                            Percent(candidate.BleedThroughIntoChannel2) +
                            " into Ch2. This path fails the common 50% minimum-yield and/or 10% maximum-bleed limits, so it is not recommended even for the fewest-tracks option.");
                    }
                    if (candidate.UsesCombinedLiveLaserRejection)
                    {
                        message.Append(" The laser line inside the nominal ChL2 pass band is evaluated as blocked by the combined excitation-separator/NFT path, not by the emission filter alone.");
                    }
                    result.Add(message.ToString());
                }
            }
            return result;
        }

        public List<string> FindRejectedParallelSummaryMessages(List<Fluorophore> dyes)
        {
            List<string> result = new List<string>();
            int i;
            int j;
            for (i = 0; i < dyes.Count; i++)
            {
                for (j = i + 1; j < dyes.Count; j++)
                {
                    if (IsFundamentallyUnresolvable(dyes[i], dyes[j]) ||
                        MakeParallelTrack(dyes[i], dyes[j], dyes,
                            AcquisitionPriority.MaximumPhotonYield) != null)
                    {
                        continue;
                    }

                    TrackConfiguration candidate = FindBestParallelCandidate(dyes[i], dyes[j], dyes,
                        AcquisitionPriority.DiagnosticBestSeparation, false);
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.UsesRepresentativeProfile)
                    {
                        result.Add(dyes[i].Name + " + " + dyes[j].Name +
                            ": parallel collection was not recommended because reliable measured emission data are not available for every selected fluorophore.");
                    }
                    else
                    {
                        result.Add(dyes[i].Name + " + " + dyes[j].Name +
                            ": a tested parallel setting kept " +
                            Percent(candidate.YieldRetentionInChannel1) + " signal for " +
                            candidate.Channel1Dye.Name + " with " +
                            Percent(candidate.BleedThroughIntoChannel1) + " estimated overlap, and " +
                            Percent(candidate.YieldRetentionInChannel2) + " signal for " +
                            candidate.Channel2Dye.Name + " with " +
                            Percent(candidate.BleedThroughIntoChannel2) +
                            " estimated overlap. At least one channel falls outside the 50% signal / 10% overlap limits.");
                    }
                }
            }
            return result;
        }

        private void BuildPlans(List<Fluorophore> remaining, List<Fluorophore> allDyes,
            List<TrackConfiguration> current, AcquisitionPriority priority, ref PlanCandidate best)
        {
            if (remaining.Count == 0)
            {
                PlanCandidate candidate = new PlanCandidate();
                candidate.Priority = priority;
                candidate.MinimumYieldRetention = 1.0;
                candidate.MeetsQualityLimits = true;
                int i;
                for (i = 0; i < current.Count; i++)
                {
                    candidate.Tracks.Add(current[i]);
                    candidate.TotalTargetSignal += TargetSignal(current[i]);
                    candidate.TotalInterference += TrackInterference(current[i]);
                    candidate.MeetsQualityLimits = candidate.MeetsQualityLimits &&
                        current[i].MeetsQualityLimits;
                    if (current[i].Channel1Dye != null)
                    {
                        candidate.MinimumYieldRetention = Math.Min(candidate.MinimumYieldRetention,
                            current[i].YieldRetentionInChannel1);
                        candidate.MaximumBleedThrough = Math.Max(candidate.MaximumBleedThrough,
                            current[i].BleedThroughIntoChannel1);
                    }
                    if (current[i].Channel2Dye != null)
                    {
                        candidate.MinimumYieldRetention = Math.Min(candidate.MinimumYieldRetention,
                            current[i].YieldRetentionInChannel2);
                        candidate.MaximumBleedThrough = Math.Max(candidate.MaximumBleedThrough,
                            current[i].BleedThroughIntoChannel2);
                    }
                }
                // Retain a scalar score for diagnostics, but use the explicit strategy comparator
                // below so a track-count bonus cannot silently override the selected objective.
                candidate.Score = candidate.TotalTargetSignal - candidate.TotalInterference;
                if (IsBetterPlan(candidate, best, priority))
                {
                    best = candidate;
                }
                return;
            }

            Fluorophore first = remaining[0];
            List<Fluorophore> afterSingle = CopyWithout(remaining, 0);
            TrackConfiguration single = MakeSingleTrack(first, allDyes);
            current.Add(single);
            BuildPlans(afterSingle, allDyes, current, priority, ref best);
            current.RemoveAt(current.Count - 1);

            int pairIndex;
            for (pairIndex = 1; pairIndex < remaining.Count; pairIndex++)
            {
                TrackConfiguration pair = MakeParallelTrack(first, remaining[pairIndex], allDyes,
                    priority);
                if (pair != null)
                {
                    List<Fluorophore> afterPair = CopyWithout(remaining, pairIndex, 0);
                    current.Add(pair);
                    BuildPlans(afterPair, allDyes, current, priority, ref best);
                    current.RemoveAt(current.Count - 1);
                }
            }
        }

        private static double TargetSignal(TrackConfiguration track)
        {
            return track.SignalInChannel1 + track.SignalInChannel2;
        }

        private static double TrackInterference(TrackConfiguration track)
        {
            return track.BleedThroughIntoChannel1 + track.BleedThroughIntoChannel2;
        }

        private static bool IsBetterPlan(PlanCandidate candidate, PlanCandidate best,
            AcquisitionPriority priority)
        {
            if (best == null)
            {
                return true;
            }

            const double tolerance = 0.000001;
            if (candidate.MeetsQualityLimits != best.MeetsQualityLimits)
            {
                return candidate.MeetsQualityLimits;
            }
            if (priority == AcquisitionPriority.FastestAcquisition &&
                candidate.Tracks.Count != best.Tracks.Count)
            {
                return candidate.Tracks.Count < best.Tracks.Count;
            }

            if (Math.Abs(candidate.TotalTargetSignal - best.TotalTargetSignal) > tolerance)
            {
                return candidate.TotalTargetSignal > best.TotalTargetSignal;
            }
            if (Math.Abs(candidate.TotalInterference - best.TotalInterference) > tolerance)
            {
                return candidate.TotalInterference < best.TotalInterference;
            }

            // In maximum-photon mode, track count is deliberately only a tie-breaker.
            return candidate.Tracks.Count < best.Tracks.Count;
        }

        private static List<Fluorophore> CopyWithout(List<Fluorophore> source, params int[] indexes)
        {
            List<Fluorophore> result = new List<Fluorophore>();
            int i;
            int j;
            bool omit;
            for (i = 0; i < source.Count; i++)
            {
                omit = false;
                for (j = 0; j < indexes.Length; j++)
                {
                    if (i == indexes[j])
                    {
                        omit = true;
                        break;
                    }
                }
                if (!omit)
                {
                    result.Add(source[i]);
                }
            }
            return result;
        }

        private TrackConfiguration MakeSingleTrack(Fluorophore dye, List<Fluorophore> allDyes)
        {
            TrackConfiguration best = null;
            TrackConfiguration bestFallback = null;
            double bestIsolatedSignal = BestIsolatedSignal(dye);
            int[] availableLasers = dye.GetLasers(microscope.Kind);
            int laserIndex;
            for (laserIndex = 0; laserIndex < availableLasers.Length; laserIndex++)
            {
                int laser = availableLasers[laserIndex];
                List<int> activeLasers = new List<int>();
                activeLasers.Add(laser);
                int channel;
                for (channel = 1; channel <= 2; channel++)
                {
                    List<EmissionFilter> filters = channel == 1 ?
                        microscope.Channel1Filters : microscope.Channel2Filters;
                    int i;
                    for (i = 0; i < filters.Count; i++)
                    {
                        EmissionFilter filter = filters[i];
                        bool usesCombinedRejection;
                        if (!PathRejectsAllLasers(filter, activeLasers, 0, channel, out usesCombinedRejection))
                        {
                            continue;
                        }

                        double signal = FilterSignal(dye, filter);
                        if (signal < MinimumAbsoluteCollection)
                        {
                            continue;
                        }

                        double retention = SafeRatio(signal, bestIsolatedSignal);
                        double modeledBleed = TotalBleedFromOtherSelectedDyes(dye, allDyes,
                            activeLasers, filter);

                        TrackConfiguration current = new TrackConfiguration();
                        current.Dyes.Add(dye);
                        current.Lasers.Add(laser);
                        current.MainSplitter = MainSplitterFor(current.Lasers);
                        current.SecondarySplitter = "Not required (one detection channel)";
                        current.PlateSetting = SingleChannelPlateSetting(channel);
                        current.RearSetting = microscope.Kind == MicroscopeKind.Lsm5Live ? "Rear" : String.Empty;
                        if (channel == 1)
                        {
                            current.Channel1Dye = dye;
                            current.Channel1Filter = filter;
                            current.SignalInChannel1 = signal;
                            current.YieldRetentionInChannel1 = retention;
                            current.BleedThroughIntoChannel1 = modeledBleed;
                        }
                        else
                        {
                            current.Channel2Dye = dye;
                            current.Channel2Filter = filter;
                            current.SignalInChannel2 = signal;
                            current.YieldRetentionInChannel2 = retention;
                            current.BleedThroughIntoChannel2 = modeledBleed;
                        }
                        current.SameLaserInterference = modeledBleed;
                        current.UsesRepresentativeProfile =
                            dye.EmissionProfile.Evidence == SpectralEvidence.Representative;
                        current.UsesCombinedLiveLaserRejection = usesCombinedRejection;
                        current.MeetsQualityLimits = retention >= MinimumYieldRetention &&
                            modeledBleed <= MaximumBleedThrough;
                        current.Score = signal - modeledBleed;
                        current.Reason = "The filter collects " + Percent(signal) +
                            " of the normalized reference emission profile (" + Percent(retention) +
                            " of the best isolated path); modeled bleed from other selected labels is " +
                            Percent(modeledBleed) + ".";
                        if (IsBetterSingleTrack(current, bestFallback))
                        {
                            bestFallback = current;
                        }
                        if (current.MeetsQualityLimits && IsBetterSingleTrack(current, best))
                        {
                            best = current;
                        }
                    }
                }
            }

            if (best == null && bestFallback != null)
            {
                best = bestFallback;
            }

            if (best == null)
            {
                // All current catalog entries should have a valid documented path. Keep an
                // explicit unsafe fallback so a future catalog edit cannot crash the program.
                best = new TrackConfiguration();
                best.Dyes.Add(dye);
                int fallbackLaser = dye.PreferredLaser(microscope.Kind);
                best.Lasers.Add(fallbackLaser);
                best.MainSplitter = MainSplitterFor(best.Lasers);
                best.SecondarySplitter = "Not required (one detection channel)";
                best.PlateSetting = SingleChannelPlateSetting(1);
                best.RearSetting = microscope.Kind == MicroscopeKind.Lsm5Live ? "Rear" : String.Empty;
                best.Channel1Dye = dye;
                best.Channel1Filter = microscope.Channel1Filters[0];
                best.Score = -1000.0;
                best.MeetsQualityLimits = false;
                best.Reason = "No documented laser-rejecting detection path was found; do not use this fallback without optical verification.";
            }
            return best;
        }

        private static bool IsBetterSingleTrack(TrackConfiguration candidate,
            TrackConfiguration best)
        {
            if (best == null)
            {
                return true;
            }
            const double tolerance = 0.000001;
            double candidateSignal = TargetSignal(candidate);
            double bestSignal = TargetSignal(best);
            if (Math.Abs(candidateSignal - bestSignal) > tolerance)
            {
                return candidateSignal > bestSignal;
            }
            return TrackInterference(candidate) < TrackInterference(best) - tolerance;
        }

        private TrackConfiguration MakeParallelTrack(Fluorophore a, Fluorophore b,
            List<Fluorophore> allDyes, AcquisitionPriority priority)
        {
            if (IsFundamentallyUnresolvable(a, b))
            {
                return null;
            }

            return FindBestParallelCandidate(a, b, allDyes, priority, true);
        }

        private TrackConfiguration FindBestParallelCandidate(Fluorophore a, Fluorophore b,
            List<Fluorophore> allDyes, AcquisitionPriority priority, bool enforceQualityLimits)
        {
            List<int> lasers = CollectLasers(a, b);
            if (String.IsNullOrEmpty(MainSplitterFor(lasers)))
            {
                return null;
            }
            TrackConfiguration best = null;
            int i;
            int j;
            for (i = 0; i < microscope.Channel1Filters.Count; i++)
            {
                EmissionFilter filter1 = microscope.Channel1Filters[i];
                for (j = 0; j < microscope.Channel2Filters.Count; j++)
                {
                    EmissionFilter filter2 = microscope.Channel2Filters[j];
                    ConsiderPair(a, b, allDyes, lasers, filter1, filter2, priority,
                        enforceQualityLimits, ref best);
                    ConsiderPair(b, a, allDyes, lasers, filter1, filter2, priority,
                        enforceQualityLimits, ref best);
                }
            }
            return best;
        }

        private void ConsiderPair(Fluorophore channel1Dye, Fluorophore channel2Dye,
            List<Fluorophore> allDyes, List<int> lasers, EmissionFilter filter1,
            EmissionFilter filter2, AcquisitionPriority priority, bool enforceQualityLimits,
            ref TrackConfiguration best)
        {
            int splitter = NearestValidSplitter(channel1Dye.EmissionPeak, channel2Dye.EmissionPeak);
            bool combinedRejection1;
            bool combinedRejection2;
            if (!PathRejectsAllLasers(filter1, lasers, splitter, 1, out combinedRejection1) ||
                !PathRejectsAllLasers(filter2, lasers, splitter, 2, out combinedRejection2))
            {
                return;
            }

            double self1 = FilterSignal(channel1Dye, filter1);
            double self2 = FilterSignal(channel2Dye, filter2);
            double spillInto1 = FilterSignal(channel2Dye, filter1);
            double spillInto2 = FilterSignal(channel1Dye, filter2);
            if (self1 < MinimumAbsoluteCollection || self2 < MinimumAbsoluteCollection)
            {
                return;
            }

            double retention1 = SafeRatio(self1, BestIsolatedSignal(channel1Dye));
            double retention2 = SafeRatio(self2, BestIsolatedSignal(channel2Dye));
            double bleedInto1 = SafeRatio(spillInto1, self2);
            double bleedInto2 = SafeRatio(spillInto2, self1);

            bool representative =
                channel1Dye.EmissionProfile.Evidence == SpectralEvidence.Representative ||
                channel2Dye.EmissionProfile.Evidence == SpectralEvidence.Representative;

            int otherIndex;
            for (otherIndex = 0; otherIndex < allDyes.Count; otherIndex++)
            {
                Fluorophore other = allDyes[otherIndex];
                if (other == channel1Dye || other == channel2Dye ||
                    !DyeUsesAnyLaser(other, lasers))
                {
                    continue;
                }
                double otherReference = BestIsolatedSignal(other);
                bleedInto1 += SafeRatio(FilterSignal(other, filter1), otherReference);
                bleedInto2 += SafeRatio(FilterSignal(other, filter2), otherReference);
                representative = representative ||
                    other.EmissionProfile.Evidence == SpectralEvidence.Representative;
            }

            bool meetsQualityLimits = !representative &&
                retention1 >= MinimumYieldRetention && retention2 >= MinimumYieldRetention &&
                bleedInto1 <= MaximumBleedThrough && bleedInto2 <= MaximumBleedThrough;
            if (enforceQualityLimits && !meetsQualityLimits)
            {
                return;
            }

            TrackConfiguration current = new TrackConfiguration();
            current.Dyes.Add(channel1Dye);
            current.Dyes.Add(channel2Dye);
            current.Channel1Dye = channel1Dye;
            current.Channel2Dye = channel2Dye;
            current.Channel1Filter = filter1;
            current.Channel2Filter = filter2;
            current.Lasers = new List<int>(lasers);
            current.MainSplitter = MainSplitterFor(lasers);
            current.SecondarySplitter = SecondarySplitterFor(channel1Dye, channel2Dye);
            current.PlateSetting = PlateFor(channel1Dye, channel2Dye);
            current.RearSetting = microscope.Kind == MicroscopeKind.Lsm5Live ? "Rear" : String.Empty;
            current.SpillIntoChannel1 = spillInto1;
            current.SpillIntoChannel2 = spillInto2;
            current.SignalInChannel1 = self1;
            current.SignalInChannel2 = self2;
            current.YieldRetentionInChannel1 = retention1;
            current.YieldRetentionInChannel2 = retention2;
            current.BleedThroughIntoChannel1 = bleedInto1;
            current.BleedThroughIntoChannel2 = bleedInto2;
            current.MeetsQualityLimits = meetsQualityLimits;
            current.UsesRepresentativeProfile = representative;
            current.UsesCombinedLiveLaserRejection = combinedRejection1 || combinedRejection2;
            current.Score = retention1 + retention2 - bleedInto1 - bleedInto2;
            current.Reason = "Simultaneous acquisition retains " + Percent(retention1) +
                " of Ch1's best isolated yield and " + Percent(retention2) +
                " of Ch2's; modeled same-dye bleed is " + Percent(bleedInto1) +
                " into Ch1 and " + Percent(bleedInto2) + " into Ch2. " +
                "Single-label controls remain mandatory because abundance, excitation efficiency and detector response are specimen-dependent.";
            if (IsBetterParallelTrack(current, best, priority))
            {
                best = current;
            }
        }

        private static bool IsBetterParallelTrack(TrackConfiguration candidate,
            TrackConfiguration best, AcquisitionPriority priority)
        {
            if (best == null)
            {
                return true;
            }

            const double tolerance = 0.000001;
            if (priority != AcquisitionPriority.DiagnosticBestSeparation)
            {
                double candidateSignal = TargetSignal(candidate);
                double bestSignal = TargetSignal(best);
                if (Math.Abs(candidateSignal - bestSignal) > tolerance)
                {
                    return candidateSignal > bestSignal;
                }
            }

            double candidateMinimumRetention = Math.Min(candidate.YieldRetentionInChannel1,
                candidate.YieldRetentionInChannel2);
            double bestMinimumRetention = Math.Min(best.YieldRetentionInChannel1,
                best.YieldRetentionInChannel2);
            if (Math.Abs(candidateMinimumRetention - bestMinimumRetention) > tolerance)
            {
                return candidateMinimumRetention > bestMinimumRetention;
            }

            double candidateSpill = TrackInterference(candidate);
            double bestSpill = TrackInterference(best);
            if (Math.Abs(candidateSpill - bestSpill) > tolerance)
            {
                return candidateSpill < bestSpill;
            }
            return TargetSignal(candidate) > TargetSignal(best) + tolerance;
        }

        private double BestIsolatedSignal(Fluorophore dye)
        {
            double cached;
            if (bestIsolatedSignalCache.TryGetValue(dye.Name, out cached))
            {
                return cached;
            }

            double best = 0.0;
            int[] availableLasers = dye.GetLasers(microscope.Kind);
            int laserIndex;
            for (laserIndex = 0; laserIndex < availableLasers.Length; laserIndex++)
            {
                List<int> activeLasers = new List<int>();
                activeLasers.Add(availableLasers[laserIndex]);
                int channel;
                for (channel = 1; channel <= 2; channel++)
                {
                    List<EmissionFilter> filters = channel == 1 ?
                        microscope.Channel1Filters : microscope.Channel2Filters;
                    int filterIndex;
                    for (filterIndex = 0; filterIndex < filters.Count; filterIndex++)
                    {
                        bool usesCombinedRejection;
                        if (!PathRejectsAllLasers(filters[filterIndex], activeLasers, 0, channel,
                            out usesCombinedRejection))
                        {
                            continue;
                        }
                        best = Math.Max(best, FilterSignal(dye, filters[filterIndex]));
                    }
                }
            }
            bestIsolatedSignalCache[dye.Name] = best;
            return best;
        }

        private double TotalBleedFromOtherSelectedDyes(Fluorophore target,
            List<Fluorophore> allDyes, List<int> activeLasers, EmissionFilter targetFilter)
        {
            double total = 0.0;
            int i;
            for (i = 0; i < allDyes.Count; i++)
            {
                Fluorophore other = allDyes[i];
                if (other == target || !DyeUsesAnyLaser(other, activeLasers))
                {
                    continue;
                }
                total += SafeRatio(FilterSignal(other, targetFilter), BestIsolatedSignal(other));
            }
            return total;
        }

        private bool DyeUsesAnyLaser(Fluorophore dye, List<int> lasers)
        {
            int i;
            for (i = 0; i < lasers.Count; i++)
            {
                if (DyeUsesLaser(dye, lasers[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static double SafeRatio(double numerator, double denominator)
        {
            return denominator <= 0.0 ? 1000.0 : numerator / denominator;
        }

        private bool IsFundamentallyUnresolvable(Fluorophore a, Fluorophore b)
        {
            if (!HasCommonLaser(a, b))
            {
                return false;
            }
            return !HasDiscriminatingSequentialTrack(a, b) ||
                !HasDiscriminatingSequentialTrack(b, a);
        }

        private bool HasDiscriminatingSequentialTrack(Fluorophore target, Fluorophore interferer)
        {
            int[] targetLasers = target.GetLasers(microscope.Kind);
            int laserIndex;
            for (laserIndex = 0; laserIndex < targetLasers.Length; laserIndex++)
            {
                int laser = targetLasers[laserIndex];
                List<int> activeLasers = new List<int>();
                activeLasers.Add(laser);
                bool interfererIsExcited = DyeUsesLaser(interferer, laser);
                int channel;
                for (channel = 1; channel <= 2; channel++)
                {
                    List<EmissionFilter> filters = channel == 1 ?
                        microscope.Channel1Filters : microscope.Channel2Filters;
                    int filterIndex;
                    for (filterIndex = 0; filterIndex < filters.Count; filterIndex++)
                    {
                        bool usesCombinedRejection;
                        if (!PathRejectsAllLasers(filters[filterIndex], activeLasers, 0, channel,
                            out usesCombinedRejection))
                        {
                            continue;
                        }
                        double self = FilterSignal(target, filters[filterIndex]);
                        if (self < MinimumAbsoluteCollection ||
                            SafeRatio(self, BestIsolatedSignal(target)) < MinimumYieldRetention)
                        {
                            continue;
                        }
                        if (!interfererIsExcited)
                        {
                            return true;
                        }
                        bool representative =
                            target.EmissionProfile.Evidence == SpectralEvidence.Representative ||
                            interferer.EmissionProfile.Evidence == SpectralEvidence.Representative;
                        if (representative)
                        {
                            continue;
                        }
                        double bleed = SafeRatio(FilterSignal(interferer, filters[filterIndex]),
                            BestIsolatedSignal(interferer));
                        if (bleed <= MaximumBleedThrough)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasCommonLaser(Fluorophore a, Fluorophore b)
        {
            int[] first = a.GetLasers(microscope.Kind);
            int[] second = b.GetLasers(microscope.Kind);
            int i;
            int j;
            for (i = 0; i < first.Length; i++)
            {
                for (j = 0; j < second.Length; j++)
                {
                    if (first[i] == second[j])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool DyeUsesLaser(Fluorophore dye, int laser)
        {
            int[] lasers = dye.GetLasers(microscope.Kind);
            int i;
            for (i = 0; i < lasers.Length; i++)
            {
                if (lasers[i] == laser)
                {
                    return true;
                }
            }
            return false;
        }

        private List<int> CollectLasers(Fluorophore a, Fluorophore b)
        {
            List<int> result = new List<int>();
            AddUnique(result, a.PreferredLaser(microscope.Kind));
            AddUnique(result, b.PreferredLaser(microscope.Kind));
            return result;
        }

        private static void AddUnique(List<int> target, int value)
        {
            int i;
            for (i = 0; i < target.Count; i++)
            {
                if (target[i] == value)
                {
                    return;
                }
            }
            if (value > 0)
            {
                target.Add(value);
            }
        }

        private string MainSplitterFor(List<int> lasers)
        {
            if (microscope.Kind == MicroscopeKind.Lsm5Live)
            {
                return "Activate in Light Path: " + JoinLasers(lasers);
            }

            bool has458 = Contains(lasers, 458);
            bool has488 = Contains(lasers, 488);
            bool has543 = Contains(lasers, 543);
            bool has633 = Contains(lasers, 633);
            if (has458 && (has543 || has633))
            {
                if (has488)
                {
                    return String.Empty;
                }
                return "HFT 458/543/633";
            }
            if (has488 && (has543 || has633))
            {
                return "HFT 488/543/633";
            }
            if (has543 && !has488 && !has633 && !has458)
            {
                return "HFT 543";
            }
            if (has488 && !has543 && !has633 && !has458)
            {
                return "HFT 488";
            }
            if (has458 && !has488 && !has543 && !has633)
            {
                return "HFT 458/514";
            }
            if (has458 && has488)
            {
                // None of the photographed Pascal HFT positions injects both lines.
                return String.Empty;
            }
            if (has633)
            {
                return "HFT 488/543/633";
            }
            if (has543)
            {
                return "HFT 488/543/633";
            }
            return "HFT 488";
        }

        private string SecondarySplitterFor(Fluorophore a, Fluorophore b)
        {
            int splitter = NearestValidSplitter(a.EmissionPeak, b.EmissionPeak);
            if (microscope.Kind == MicroscopeKind.Pascal)
            {
                return "NFT " + splitter;
            }
            return "NFT " + splitter;
        }

        private string PlateFor(Fluorophore a, Fluorophore b)
        {
            if (microscope.Kind == MicroscopeKind.Pascal)
            {
                return "NFT " + NearestValidSplitter(a.EmissionPeak, b.EmissionPeak);
            }
            return "NFT " + NearestValidSplitter(a.EmissionPeak, b.EmissionPeak);
        }

        private string SingleChannelPlateSetting(int channel)
        {
            // Ch1 is the straight-through arm in both supplied dialog layouts.
            // With Ch2 off, None bypasses an unnecessary secondary optical element.
            if (channel == 1)
            {
                return "None";
            }

            // The installed Live Plate is retained for a Ch2-only path until the
            // routing and throughput of the photographed Mirror position have
            // been verified on the microscope itself.
            return microscope.Kind == MicroscopeKind.Lsm5Live ? "Plate" : "None";
        }

        private int NearestValidSplitter(int firstEmission, int secondEmission)
        {
            int low = Math.Min(firstEmission, secondEmission);
            int high = Math.Max(firstEmission, secondEmission);
            int midpoint = (low + high) / 2;
            int best = microscope.Splitters[0];
            int bestDistance = 10000;
            int i;
            for (i = 0; i < microscope.Splitters.Length; i++)
            {
                int candidate = microscope.Splitters[i];
                int distance = Math.Abs(candidate - midpoint);
                if (candidate > low && candidate < high && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool Contains(List<int> source, int value)
        {
            int i;
            for (i = 0; i < source.Count; i++)
            {
                if (source[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        private bool PathRejectsAllLasers(EmissionFilter filter, List<int> lasers, int splitter,
            int channel, out bool usedCombinedRejection)
        {
            usedCombinedRejection = false;
            int i;
            for (i = 0; i < lasers.Count; i++)
            {
                int laser = lasers[i];
                if (!filter.Passes(laser))
                {
                    continue;
                }
                if (LiveCombinedPathRejectsLaser(filter, laser, splitter, channel))
                {
                    usedCombinedRejection = true;
                    continue;
                }
                return false;
            }
            return true;
        }

        private bool LiveCombinedPathRejectsLaser(EmissionFilter filter, int laser, int splitter, int channel)
        {
            if (microscope.Kind != MicroscopeKind.Lsm5Live)
            {
                return false;
            }

            // The documented red/far-red candidate path uses the excitation separator together
            // with NFT 635. The short branch may therefore use ChL2 BP 540-625 even though its
            // nominal pass band contains 561 nm; the filter must not be evaluated in isolation.
            if (laser == 561 && filter.Name == "BP 540-625")
            {
                if (splitter == 0)
                {
                    return true;
                }
                return splitter == 635 && channel == 2;
            }
            return false;
        }

        private static double FilterSignal(Fluorophore dye, EmissionFilter filter)
        {
            return dye.EmissionProfile.FractionPassed(filter);
        }

        private static string JoinLasers(List<int> lasers)
        {
            StringBuilder result = new StringBuilder();
            int i;
            for (i = 0; i < lasers.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(lasers[i]);
                result.Append(" nm");
            }
            return result.ToString();
        }

        private static string Percent(double value)
        {
            if (value < 0.0)
            {
                value = 0.0;
            }
            return Math.Round(value * 100.0).ToString() + "%";
        }
    }

    internal sealed class MainForm : Form
    {
        private const string EnglishEmptySlot = "-- no fluorophore --";
        private ComboBox microscopeBox;
        private ComboBox priorityBox;
        private ComboBox[] fluorophoreBoxes;
        private Button analyzeButton;
        private Button resetButton;
        private Button showButton;
        private RichTextBox resultBox;
        private Label availabilityLabel;
        private Label modeLabel;
        private List<Fluorophore> allFluorophores;
        private MicroscopeDefinition pascal;
        private MicroscopeDefinition live;
        private bool refreshing;
        private MicroscopeDefinition lastMicroscope;
        private PlanCandidate lastMaximumPlan;
        private PlanCandidate lastFastPlan;
        private List<string> lastBlocking;
        private bool lastPlansEquivalent;

        public MainForm()
        {
            allFluorophores = Catalog.CreateFluorophores();
            pascal = Catalog.CreatePascal();
            live = Catalog.CreateLive();
            InitializeComponent();
            microscopeBox.SelectedIndex = 0;
            priorityBox.SelectedIndex = 0;
            RefreshFluorophoreLists();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Confocal Configurator - LSM Pascal / LSM 5 Live";
            try
            {
                Icon executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (executableIcon != null)
                {
                    Icon = executableIcon;
                }
            }
            catch
            {
                // The application remains usable if an older Windows shell cannot extract the icon.
            }
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1080, 730);
            MinimumSize = new Size(980, 650);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(242, 246, 249);

            Label title = new Label();
            title.Text = "Confocal Configurator";
            title.Font = new Font("Segoe UI", 18.0f, FontStyle.Bold, GraphicsUnit.Point);
            title.Location = new Point(24, 18);
            title.Size = new Size(450, 36);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Finds clear, low-overlap settings and uses parallel collection when it is suitable";
            subtitle.ForeColor = Color.FromArgb(65, 80, 95);
            subtitle.Location = new Point(26, 55);
            subtitle.Size = new Size(850, 22);
            Controls.Add(subtitle);

            GroupBox selectionGroup = new GroupBox();
            selectionGroup.Text = "1. Select your experiment";
            selectionGroup.Location = new Point(24, 93);
            selectionGroup.Size = new Size(465, 438);
            selectionGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            Controls.Add(selectionGroup);

            Label microscopeLabel = new Label();
            microscopeLabel.Text = "Microscope:";
            microscopeLabel.Location = new Point(18, 32);
            microscopeLabel.Size = new Size(100, 22);
            selectionGroup.Controls.Add(microscopeLabel);

            microscopeBox = new ComboBox();
            microscopeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            microscopeBox.Location = new Point(122, 29);
            microscopeBox.Size = new Size(315, 25);
            microscopeBox.Items.Add("Zeiss LSM Pascal (458 / 488 / 543 / 633 nm)");
            microscopeBox.Items.Add("Zeiss LSM 5 Live (405 / 488 / 561 / 635 nm)");
            microscopeBox.SelectedIndexChanged += new EventHandler(MicroscopeChanged);
            selectionGroup.Controls.Add(microscopeBox);

            availabilityLabel = new Label();
            availabilityLabel.Location = new Point(21, 64);
            availabilityLabel.Size = new Size(413, 48);
            availabilityLabel.ForeColor = Color.FromArgb(112, 76, 24);
            availabilityLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            selectionGroup.Controls.Add(availabilityLabel);

            fluorophoreBoxes = new ComboBox[4];
            int i;
            for (i = 0; i < fluorophoreBoxes.Length; i++)
            {
                Label slotLabel = new Label();
                slotLabel.Text = "Fluorophore " + (i + 1).ToString() + ":";
                slotLabel.Location = new Point(18, 126 + i * 55);
                slotLabel.Size = new Size(100, 22);
                selectionGroup.Controls.Add(slotLabel);

                ComboBox box = new ComboBox();
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.Location = new Point(122, 122 + i * 55);
                box.Size = new Size(315, 25);
                box.MaxDropDownItems = 18;
                box.SelectedIndexChanged += new EventHandler(FluorophoreChanged);
                fluorophoreBoxes[i] = box;
                selectionGroup.Controls.Add(box);
            }

            Label priorityLabel = new Label();
            priorityLabel.Text = "Visualise:";
            priorityLabel.Location = new Point(18, 326);
            priorityLabel.Size = new Size(100, 22);
            selectionGroup.Controls.Add(priorityLabel);

            priorityBox = new ComboBox();
            priorityBox.DropDownStyle = ComboBoxStyle.DropDownList;
            priorityBox.Location = new Point(122, 322);
            priorityBox.Size = new Size(315, 25);
            priorityBox.Items.Add("Best signal");
            priorityBox.Items.Add("Faster acquisition (fewer tracks)");
            priorityBox.SelectedIndexChanged += new EventHandler(PriorityChanged);
            selectionGroup.Controls.Add(priorityBox);

            analyzeButton = new Button();
            analyzeButton.Text = "Find ideal settings";
            analyzeButton.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            analyzeButton.Location = new Point(122, 359);
            analyzeButton.Size = new Size(230, 34);
            analyzeButton.BackColor = Color.FromArgb(37, 115, 173);
            analyzeButton.ForeColor = Color.White;
            analyzeButton.FlatStyle = FlatStyle.Flat;
            analyzeButton.Click += new EventHandler(AnalyzeClickedEnglish);
            selectionGroup.Controls.Add(analyzeButton);

            resetButton = new Button();
            resetButton.Text = "Reset";
            resetButton.Location = new Point(360, 359);
            resetButton.Size = new Size(77, 34);
            resetButton.Click += new EventHandler(ResetClickedEnglish);
            selectionGroup.Controls.Add(resetButton);

            showButton = new Button();
            showButton.Text = "Show me";
            showButton.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold, GraphicsUnit.Point);
            showButton.Location = new Point(122, 399);
            showButton.Size = new Size(315, 27);
            showButton.Enabled = false;
            showButton.Click += new EventHandler(ShowClicked);
            selectionGroup.Controls.Add(showButton);

            GroupBox principleGroup = new GroupBox();
            principleGroup.Text = "How the recommendation is chosen";
            principleGroup.Location = new Point(24, 544);
            principleGroup.Size = new Size(465, 148);
            principleGroup.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            Controls.Add(principleGroup);

            Label principle = new Label();
            principle.Text = "- Each channel must keep at least 50% of its best available signal.\r\n" +
                "- Estimated colour overlap must stay at 10% or less.\r\n" +
                "- Colours are collected together only when both limits are met.\r\n" +
                "- Faster means fewer acquisition passes; it does not increase scanner speed.\r\n" +
                "- Identical results are shown only once.";
            principle.Location = new Point(17, 27);
            principle.Size = new Size(430, 112);
            principle.Font = new Font("Segoe UI", 8.7f, FontStyle.Regular, GraphicsUnit.Point);
            principleGroup.Controls.Add(principle);

            GroupBox resultGroup = new GroupBox();
            resultGroup.Text = "2. Recommended settings";
            resultGroup.Location = new Point(506, 93);
            resultGroup.Size = new Size(550, 599);
            resultGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(resultGroup);

            modeLabel = new Label();
            modeLabel.Text = "Select a microscope and at least one fluorophore.";
            modeLabel.Location = new Point(18, 28);
            modeLabel.Size = new Size(514, 27);
            modeLabel.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold, GraphicsUnit.Point);
            modeLabel.ForeColor = Color.FromArgb(34, 85, 130);
            resultGroup.Controls.Add(modeLabel);

            resultBox = new RichTextBox();
            resultBox.ReadOnly = true;
            resultBox.BackColor = Color.White;
            resultBox.BorderStyle = BorderStyle.FixedSingle;
            resultBox.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            resultBox.Location = new Point(18, 60);
            resultBox.Size = new Size(514, 515);
            resultBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            resultBox.Text = "Your recommended settings will appear here.\r\n\r\n" +
                "Select the microscope and fluorophores, then click Find ideal settings.";
            resultGroup.Controls.Add(resultBox);

            Label footer = new Label();
            footer.ForeColor = Color.FromArgb(82, 90, 98);
            footer.Text = "Starting guide only - does not control the microscope - confirm colour separation with single-label controls";
            footer.Location = new Point(25, 700);
            footer.Size = new Size(1000, 22);
            footer.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(footer);

            ResumeLayout(false);
        }

        private void MicroscopeChanged(object sender, EventArgs e)
        {
            if (!refreshing)
            {
                InvalidateLastRecommendation();
                RefreshFluorophoreLists();
            }
        }

        private MicroscopeDefinition SelectedMicroscope()
        {
            return microscopeBox.SelectedIndex == 1 ? live : pascal;
        }

        private AcquisitionPriority SelectedPriority()
        {
            return priorityBox != null && priorityBox.SelectedIndex == 1 ?
                AcquisitionPriority.FastestAcquisition :
                AcquisitionPriority.MaximumPhotonYield;
        }

        private void PriorityChanged(object sender, EventArgs e)
        {
            if (!refreshing)
            {
                UpdateShowButtonText();
            }
        }

        private void FluorophoreChanged(object sender, EventArgs e)
        {
            if (!refreshing)
            {
                InvalidateLastRecommendation();
            }
        }

        private void InvalidateLastRecommendation()
        {
            lastMicroscope = null;
            lastMaximumPlan = null;
            lastFastPlan = null;
            lastBlocking = null;
            lastPlansEquivalent = false;
            if (priorityBox != null)
            {
                if (priorityBox.Items.Count > 0)
                {
                    priorityBox.Items[0] = "Best signal";
                }
                priorityBox.Enabled = true;
            }
            if (showButton != null)
            {
                showButton.Enabled = false;
            }
        }

        private void RefreshFluorophoreLists()
        {
            refreshing = true;
            MicroscopeDefinition selected = SelectedMicroscope();
            string[] priorNames = new string[fluorophoreBoxes.Length];
            int i;
            for (i = 0; i < fluorophoreBoxes.Length; i++)
            {
                Fluorophore prior = fluorophoreBoxes[i].SelectedItem as Fluorophore;
                priorNames[i] = prior == null ? String.Empty : prior.Name;
                fluorophoreBoxes[i].Items.Clear();
                fluorophoreBoxes[i].Items.Add(EnglishEmptySlot);
                int j;
                for (j = 0; j < allFluorophores.Count; j++)
                {
                    if (allFluorophores[j].IsAvailable(selected.Kind))
                    {
                        fluorophoreBoxes[i].Items.Add(allFluorophores[j]);
                    }
                }
                fluorophoreBoxes[i].SelectedIndex = 0;
                SelectByName(fluorophoreBoxes[i], priorNames[i]);
            }

            if (selected.Kind == MicroscopeKind.Pascal)
            {
                availabilityLabel.Text = "Pascal: only fluorophores that can use the installed 458, 488, 543 or 633 nm lasers are shown. " +
                    "DAPI, Hoechst, BFP and Alexa Fluor 405 are hidden because no suitable UV/405 nm laser is available.";
            }
            else
            {
                availabilityLabel.Text = "LSM 5 Live: fluorophores that can use the installed 405, 488, 561 or 635 nm lasers are shown.";
            }
            resultBox.Text = "Selection changed. Click Find ideal settings to calculate the beam path.";
            modeLabel.Text = "Choose fluorophores, then calculate the recommended settings.";
            refreshing = false;
        }

        private void UpdateShowButtonText()
        {
            if (showButton == null)
            {
                return;
            }
            if (lastPlansEquivalent)
            {
                showButton.Text = "Ideal Configuration";
                return;
            }
            showButton.Text = SelectedPriority() == AcquisitionPriority.FastestAcquisition ?
                "Show faster configuration" :
                "Show best-signal configuration";
        }

        private static void SelectByName(ComboBox box, string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return;
            }
            int i;
            for (i = 1; i < box.Items.Count; i++)
            {
                Fluorophore dye = box.Items[i] as Fluorophore;
                if (dye != null && dye.Name == name)
                {
                    box.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ResetClickedEnglish(object sender, EventArgs e)
        {
            int i;
            for (i = 0; i < fluorophoreBoxes.Length; i++)
            {
                fluorophoreBoxes[i].SelectedIndex = 0;
            }
            resultBox.Text = "Selection reset.";
            modeLabel.Text = "Select a microscope and at least one fluorophore.";
            modeLabel.ForeColor = Color.FromArgb(34, 85, 130);
            InvalidateLastRecommendation();
        }

        private void AnalyzeClickedEnglish(object sender, EventArgs e)
        {
            List<Fluorophore> selected = SelectedFluorophores();
            if (selected.Count == 0)
            {
                modeLabel.Text = "Select at least one fluorophore.";
                resultBox.Text = "No fluorophore has been selected.";
                return;
            }
            if (HasDuplicate(selected))
            {
                modeLabel.Text = "Select each fluorophore only once.";
                resultBox.Text = "Duplicate selection detected. A fluorophore cannot be configured as two independent colours.";
                return;
            }

            MicroscopeDefinition selectedMicroscope = SelectedMicroscope();
            RecommendationEngine engine = new RecommendationEngine(selectedMicroscope);
            List<string> blocking = engine.FindBlockingPairMessages(selected);
            PlanCandidate maximumPlan = engine.MakePlan(selected,
                AcquisitionPriority.MaximumPhotonYield);
            PlanCandidate fastPlan = engine.MakePlan(selected,
                AcquisitionPriority.FastestAcquisition);
            if (maximumPlan == null || fastPlan == null)
            {
                modeLabel.Text = "No configuration could be generated for this selection.";
                resultBox.Text = "No suitable configuration is available.";
                return;
            }

            resultBox.Text = BuildEnglishComparisonReport(selectedMicroscope, selected,
                maximumPlan, fastPlan, blocking);
            resultBox.SelectionStart = 0;
            resultBox.ScrollToCaret();
            lastMicroscope = selectedMicroscope;
            lastMaximumPlan = maximumPlan;
            lastFastPlan = fastPlan;
            lastBlocking = blocking;
            lastPlansEquivalent = PlansEquivalent(maximumPlan, fastPlan);
            if (lastPlansEquivalent)
            {
                priorityBox.Items[0] = "Ideal configuration";
                priorityBox.SelectedIndex = 0;
                priorityBox.Enabled = false;
            }
            else
            {
                priorityBox.Items[0] = "Best signal";
                priorityBox.Enabled = true;
            }
            showButton.Enabled = true;
            UpdateShowButtonText();
            if (blocking.Count > 0)
            {
                modeLabel.Text = "Warning: at least one dye pair cannot be cleanly separated with this hardware.";
                modeLabel.ForeColor = Color.FromArgb(155, 69, 24);
            }
            else if (lastPlansEquivalent)
            {
                modeLabel.Text = "One ideal configuration found: " +
                    TrackCountText(maximumPlan.Tracks.Count) + ".";
                modeLabel.ForeColor = Color.FromArgb(34, 85, 130);
            }
            else
            {
                modeLabel.Text = "Two suitable options: best signal uses " +
                    TrackCountText(maximumPlan.Tracks.Count) + "; faster acquisition uses " +
                    TrackCountText(fastPlan.Tracks.Count) + ".";
                modeLabel.ForeColor = fastPlan.Tracks.Count < maximumPlan.Tracks.Count ?
                    Color.FromArgb(31, 122, 70) : Color.FromArgb(34, 85, 130);
            }
        }

        private void ShowClicked(object sender, EventArgs e)
        {
            PlanCandidate selectedPlan = SelectedPriority() == AcquisitionPriority.FastestAcquisition ?
                lastFastPlan : lastMaximumPlan;
            if (lastMicroscope == null || selectedPlan == null)
            {
                return;
            }
            using (VisualizationForm form = new VisualizationForm(lastMicroscope, selectedPlan, lastBlocking))
            {
                form.ShowDialog(this);
            }
        }

        private List<Fluorophore> SelectedFluorophores()
        {
            List<Fluorophore> result = new List<Fluorophore>();
            int i;
            for (i = 0; i < fluorophoreBoxes.Length; i++)
            {
                Fluorophore dye = fluorophoreBoxes[i].SelectedItem as Fluorophore;
                if (dye != null)
                {
                    result.Add(dye);
                }
            }
            return result;
        }

        private static bool HasDuplicate(List<Fluorophore> selected)
        {
            int i;
            int j;
            for (i = 0; i < selected.Count; i++)
            {
                for (j = i + 1; j < selected.Count; j++)
                {
                    if (selected[i].Name == selected[j].Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool PlansEquivalent(PlanCandidate first, PlanCandidate second)
        {
            if (first == null || second == null || first.Tracks.Count != second.Tracks.Count)
            {
                return false;
            }

            bool[] matched = new bool[second.Tracks.Count];
            int firstIndex;
            for (firstIndex = 0; firstIndex < first.Tracks.Count; firstIndex++)
            {
                bool found = false;
                int secondIndex;
                for (secondIndex = 0; secondIndex < second.Tracks.Count; secondIndex++)
                {
                    if (!matched[secondIndex] &&
                        TracksEquivalent(first.Tracks[firstIndex], second.Tracks[secondIndex]))
                    {
                        matched[secondIndex] = true;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TracksEquivalent(TrackConfiguration first,
            TrackConfiguration second)
        {
            return first.Dyes.Count == second.Dyes.Count &&
                SameText(first.MainSplitter, second.MainSplitter) &&
                SameText(first.SecondarySplitter, second.SecondarySplitter) &&
                SameText(first.PlateSetting, second.PlateSetting) &&
                SameText(first.FilterSetSetting, second.FilterSetSetting) &&
                SameText(first.RearSetting, second.RearSetting) &&
                SameDye(first.Channel1Dye, second.Channel1Dye) &&
                SameDye(first.Channel2Dye, second.Channel2Dye) &&
                SameFilter(first.Channel1Filter, second.Channel1Filter) &&
                SameFilter(first.Channel2Filter, second.Channel2Filter) &&
                SameLaserSet(first.Lasers, second.Lasers);
        }

        private static bool SameText(string first, string second)
        {
            return String.Equals(first ?? String.Empty, second ?? String.Empty,
                StringComparison.Ordinal);
        }

        private static bool SameDye(Fluorophore first, Fluorophore second)
        {
            return first == null ? second == null :
                second != null && SameText(first.Name, second.Name);
        }

        private static bool SameFilter(EmissionFilter first, EmissionFilter second)
        {
            return first == null ? second == null :
                second != null && SameText(first.Name, second.Name);
        }

        private static bool SameLaserSet(List<int> first, List<int> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }
            int i;
            for (i = 0; i < first.Count; i++)
            {
                if (!second.Contains(first[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static string BuildEnglishReport(MicroscopeDefinition microscope, List<Fluorophore> dyes,
            PlanCandidate plan, List<string> blocking)
        {
            return BuildEnglishReport(microscope, dyes, plan, blocking,
                AcquisitionPriority.MaximumPhotonYield);
        }

        internal static string BuildEnglishReport(MicroscopeDefinition microscope, List<Fluorophore> dyes,
            PlanCandidate plan, List<string> blocking, AcquisitionPriority priority)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("RECOMMENDED SETTINGS");
            text.AppendLine("Microscope: " + microscope.DisplayName);
            text.AppendLine("Fluorophores: " + JoinDyes(dyes));
            text.AppendLine();
            AppendQualityPolicy(text);
            AppendBlockingWarnings(text, blocking);
            AppendEnglishPlanSection(text, microscope, plan, priority, blocking.Count == 0, false);
            AppendFluorophoreWarnings(text, microscope, dyes);
            AppendValidationNotes(text, microscope);
            return text.ToString();
        }

        internal static string BuildEnglishComparisonReport(MicroscopeDefinition microscope,
            List<Fluorophore> dyes, PlanCandidate maximumPlan, PlanCandidate fastPlan,
            List<string> blocking)
        {
            bool equivalent = PlansEquivalent(maximumPlan, fastPlan);
            StringBuilder text = new StringBuilder();
            text.AppendLine("RECOMMENDED SETTINGS");
            text.AppendLine("Microscope: " + microscope.DisplayName);
            text.AppendLine("Fluorophores: " + JoinDyes(dyes));
            text.AppendLine();
            AppendQualityPolicy(text);
            AppendBlockingWarnings(text, blocking);

            if (!equivalent)
            {
                text.AppendLine("TWO SUITABLE OPTIONS");
                text.AppendLine("Both options pass the quality check. Faster acquisition means fewer tracks; it does not change the microscope's scanner-speed setting.");
                text.AppendLine();
            }

            if (equivalent)
            {
                AppendEnglishPlanSection(text, microscope, maximumPlan,
                    AcquisitionPriority.MaximumPhotonYield, blocking.Count == 0, true);
            }
            else
            {
                AppendEnglishPlanSection(text, microscope, maximumPlan,
                    AcquisitionPriority.MaximumPhotonYield, blocking.Count == 0, false);
                AppendEnglishPlanSection(text, microscope, fastPlan,
                    AcquisitionPriority.FastestAcquisition, blocking.Count == 0, false);
            }

            List<string> rejectedParallel =
                new RecommendationEngine(microscope).FindRejectedParallelSummaryMessages(dyes);
            if (rejectedParallel.Count > 0)
            {
                text.AppendLine("WHY THE COLOURS ARE NOT COLLECTED TOGETHER");
                int candidateIndex;
                for (candidateIndex = 0; candidateIndex < rejectedParallel.Count; candidateIndex++)
                {
                    text.AppendLine("- " + rejectedParallel[candidateIndex]);
                }
                text.AppendLine();
            }

            AppendFluorophoreWarnings(text, microscope, dyes);
            AppendValidationNotes(text, microscope);
            return text.ToString();
        }

        private static void AppendQualityPolicy(StringBuilder text)
        {
            text.AppendLine("QUALITY CHECK");
            text.AppendLine("A setting is recommended only when every channel keeps at least " +
                FormatPercent(RecommendationEngine.MinimumYieldRetention) +
                " of its best available signal and estimated colour overlap stays at " +
                FormatPercent(RecommendationEngine.MaximumBleedThrough) + " or less.");
            text.AppendLine("Colours are collected together only when measured spectral data support that choice.");
            text.AppendLine();
        }

        private static void AppendBlockingWarnings(StringBuilder text, List<string> blocking)
        {
            if (blocking.Count == 0)
            {
                return;
            }
            text.AppendLine("NO RELIABLE SEPARATION FOUND");
            int b;
            for (b = 0; b < blocking.Count; b++)
            {
                text.AppendLine("- " + blocking[b]);
            }
            text.AppendLine("Do not treat the affected fluorophores as separate signals. The track settings below are individual starting points only.");
            text.AppendLine();
        }

        private static void AppendEnglishPlanSection(StringBuilder text,
            MicroscopeDefinition microscope, PlanCandidate plan, AcquisitionPriority priority,
            bool overallReliable, bool sharedByBothObjectives)
        {
            bool fastest = priority == AcquisitionPriority.FastestAcquisition;
            if (sharedByBothObjectives)
            {
                text.AppendLine("IDEAL CONFIGURATION");
            }
            else
            {
                text.AppendLine(fastest ?
                    "OPTION 2 - FASTER ACQUISITION" :
                    "OPTION 1 - BEST SIGNAL");
            }
            if (!overallReliable)
            {
                text.AppendLine("These settings are not a reliable multicolour recommendation.");
            }
            else if (!plan.MeetsQualityLimits)
            {
                text.AppendLine("One or more channels does not pass the signal and overlap checks. Review the warning above.");
            }
            else if (plan.Tracks.Count == 1)
            {
                text.AppendLine(plan.Tracks[0].IsParallel() ?
                    "Acquisition: 1 track - both colours are collected together." :
                    "Acquisition: 1 track.");
            }
            else
            {
                text.AppendLine("Acquisition: " + plan.Tracks.Count.ToString() +
                    " tracks collected one after another (Multi Track).");
                text.AppendLine("Use frame-by-frame track switching because the optical settings can differ between tracks.");
            }
            if (overallReliable && plan.MeetsQualityLimits)
            {
                if (sharedByBothObjectives)
                {
                    text.AppendLine("Why this is the only option shown: it gives the best estimated signal and also uses the fewest suitable acquisition tracks.");
                }
                else if (fastest)
                {
                    text.AppendLine("Best for: faster overall acquisition and less time between colours. It may collect less signal in exchange for fewer acquisition passes.");
                }
                else
                {
                    text.AppendLine("Best for: collecting the strongest signal. Additional tracks can make the acquisition slower and increase the time between colours.");
                }
            }
            text.AppendLine();

            int i;
            for (i = 0; i < plan.Tracks.Count; i++)
            {
                AppendEnglishTrack(text, microscope, plan.Tracks[i], i + 1);
            }
        }

        private static void AppendValidationNotes(StringBuilder text, MicroscopeDefinition microscope)
        {
            text.AppendLine("BEFORE RECORDING");
            if (microscope.Kind == MicroscopeKind.Pascal)
            {
                text.AppendLine("- The listed Pascal laser power, gain and pinhole values are starting settings. Check the specimen for saturation and bleaching.");
            }
            else
            {
                text.AppendLine("- No fixed LSM 5 Live power, gain or pinhole values were supplied. Adjust them on the specimen and avoid saturation.");
            }
            text.AppendLine("- Confirm colour separation with single-label controls using the final laser power, gain and filters.");
            text.AppendLine("- Signal and overlap percentages are spectrum-based estimates, not measurements of the final image.");
            text.AppendLine("- Verify that the listed lasers, splitters and filters are installed on the microscope.");
        }

        private static void AppendEnglishTrack(StringBuilder text, MicroscopeDefinition microscope,
            TrackConfiguration track, int number)
        {
            text.AppendLine("TRACK " + number.ToString() + " - " + JoinDyes(track.Dyes));
            text.AppendLine("Laser(s): " + JoinLasers(track.Lasers));
            if (microscope.Kind == MicroscopeKind.Pascal)
            {
                text.AppendLine("Main beam splitter: " + track.MainSplitter);
                text.AppendLine("Secondary beam splitter: " + track.PlateSetting);
                text.AppendLine("Filter set: " + track.FilterSetSetting);
                text.AppendLine("Transmission: " + PascalTransmission(track.Lasers));
                text.AppendLine("Detector gain: 700");
                text.AppendLine("Pinhole: 1.5 Airy units");
            }
            else
            {
                text.AppendLine("Plate: " + track.PlateSetting);
                text.AppendLine("Rear position: " + track.RearSetting);
            }

            if (track.Channel1Dye != null)
            {
                text.AppendLine("Channel 1: " + track.Channel1Dye.Name + " -> " + track.Channel1Filter.Name);
                text.AppendLine("  Signal kept: " + FormatPercent(track.YieldRetentionInChannel1) +
                    " of the best available setting | Estimated colour overlap: " +
                    FormatPercent(track.BleedThroughIntoChannel1));
            }
            else
            {
                text.AppendLine("Channel 1: OFF");
            }
            if (track.Channel2Dye != null)
            {
                text.AppendLine("Channel 2: " + track.Channel2Dye.Name + " -> " + track.Channel2Filter.Name);
                text.AppendLine("  Signal kept: " + FormatPercent(track.YieldRetentionInChannel2) +
                    " of the best available setting | Estimated colour overlap: " +
                    FormatPercent(track.BleedThroughIntoChannel2));
            }
            else
            {
                text.AppendLine("Channel 2: OFF");
            }
            text.AppendLine();
        }

        private static string PascalTransmission(List<int> lasers)
        {
            StringBuilder result = new StringBuilder();
            int i;
            for (i = 0; i < lasers.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(lasers[i]);
                result.Append(" nm = ");
                result.Append(lasers[i] == 488 ? "7%" : "50%");
            }
            return result.ToString();
        }

        private static void AppendFluorophoreWarnings(StringBuilder text,
            MicroscopeDefinition microscope, List<Fluorophore> dyes)
        {
            bool headingWritten = false;
            int i;
            for (i = 0; i < dyes.Count; i++)
            {
                string warning = EnglishDyeWarning(dyes[i], microscope);
                if (String.IsNullOrEmpty(warning))
                {
                    continue;
                }
                if (!headingWritten)
                {
                    text.AppendLine("IMPORTANT FLUOROPHORE NOTES");
                    headingWritten = true;
                }
                text.AppendLine("- " + dyes[i].Name + ": " + warning);
            }
            if (headingWritten)
            {
                text.AppendLine();
            }
        }

        private static string FormatPercent(double value)
        {
            value = Math.Max(0.0, value);
            return Math.Round(value * 100.0).ToString() + "%";
        }

        private static string EnglishDyeWarning(Fluorophore dye, MicroscopeDefinition microscope)
        {
            StringBuilder result = new StringBuilder();
            if (dye.EmissionProfile.Evidence == SpectralEvidence.Representative)
            {
                result.Append("No dye-specific measured emission curve is available, so the program will not automatically collect this fluorophore together with another colour.");
            }
            if (!String.IsNullOrEmpty(dye.EnglishGeneralNote))
            {
                if (result.Length > 0)
                {
                    result.Append(" ");
                }
                result.Append(dye.EnglishGeneralNote);
            }
            string microscopeNote = microscope.Kind == MicroscopeKind.Pascal ?
                dye.EnglishPascalNote : dye.EnglishLiveNote;
            if (!String.IsNullOrEmpty(microscopeNote))
            {
                if (result.Length > 0)
                {
                    result.Append(" ");
                }
                result.Append(microscopeNote);
            }
            return result.ToString();
        }

        private static string JoinDyes(List<Fluorophore> dyes)
        {
            StringBuilder result = new StringBuilder();
            int i;
            for (i = 0; i < dyes.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(dyes[i].Name);
            }
            return result.ToString();
        }

        private static string TrackCountText(int count)
        {
            return count.ToString() + (count == 1 ? " track" : " tracks");
        }

        private static string JoinLasers(List<int> lasers)
        {
            StringBuilder result = new StringBuilder();
            int i;
            for (i = 0; i < lasers.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(lasers[i]);
                result.Append(" nm");
            }
            return result.ToString();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
