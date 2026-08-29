using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ConfocalKonfigurator
{
    internal static class RegressionTests
    {
        private static int failures;

        private static int Main()
        {
            TestPascalHardwareCatalog();
            TestSameLaserPairsAreBlocked();
            TestAcquisitionPriorities();
            TestQualityGatedParallelPairs();
            TestPascalMainSplitterCompatibility();
            TestLiveRedFarRedPath();
            TestPascalSingleChannelBypass();
            TestLiveSingleChannelBypass();
            TestReferenceCurvesAndEnglishWarnings();
            TestPascalOriginalDialogTopologyRendering();
            TestLiveOriginalDialogTopologyRendering();
            TestLiveInactiveDetectorPathRendering();
            TestLiveFluorophorePathColours();
            TestPlainLanguageMainFormRendering();

            if (failures == 0)
            {
                Console.WriteLine("All confocal regression tests passed.");
                return 0;
            }
            Console.WriteLine(failures.ToString() + " regression test(s) failed.");
            return 1;
        }

        private static void TestPascalHardwareCatalog()
        {
            MicroscopeDefinition pascal = Catalog.CreatePascal();
            Assert(Contains(pascal.Lasers, 458), "Pascal exposes the photographed 458 nm line");
            Assert(Contains(pascal.Lasers, 488) && Contains(pascal.Lasers, 543) && Contains(pascal.Lasers, 633),
                "Pascal exposes 488/543/633 nm");
            Assert(FindDye("ECFP").IsAvailable(MicroscopeKind.Pascal), "ECFP is available at Pascal 458 nm");
            Assert(FindDye("Cerulean").IsAvailable(MicroscopeKind.Pascal), "Cerulean is available at Pascal 458 nm");
            Assert(!FindDye("DAPI").IsAvailable(MicroscopeKind.Pascal), "DAPI remains unavailable on Pascal");
            Assert(!FindDye("mTagBFP2").IsAvailable(MicroscopeKind.Pascal), "mTagBFP2 remains unavailable on Pascal");
            Assert(FindDye("Alexa Fluor 680").IsAvailable(MicroscopeKind.Pascal),
                "Alexa Fluor 680 treats 633 and 635 nm consistently");
        }

        private static void TestSameLaserPairsAreBlocked()
        {
            AssertBlocked(Catalog.CreatePascal(), "mOrange", "mCherry");
            AssertBlocked(Catalog.CreateLive(), "mOrange", "mCherry");
            AssertBlocked(Catalog.CreatePascal(), "Alexa Fluor 555", "Alexa Fluor 594");
            AssertBlocked(Catalog.CreateLive(), "Alexa Fluor 555", "Alexa Fluor 594");
            AssertBlocked(Catalog.CreateLive(), "Alexa Fluor 647", "Alexa Fluor 680");
            string blockedReport = Report(Catalog.CreatePascal(), "mOrange", "mCherry");
            Assert(blockedReport.IndexOf("NO RELIABLE SEPARATION FOUND") >= 0 &&
                blockedReport.IndexOf("Acquisition:") < 0,
                "An unresolved pair is not presented as a valid Multi Track mode");
        }

        private static void TestQualityGatedParallelPairs()
        {
            AssertSequential(Catalog.CreateLive(), "mTagBFP2", "EGFP",
                AcquisitionPriority.FastestAcquisition,
                "Live blue/green pair is not promoted when no parallel path passes both limits");
            AssertSequential(Catalog.CreatePascal(), "GFP", "DsRed",
                AcquisitionPriority.FastestAcquisition,
                "Pascal GFP + DsRed is not promoted through a low-quality parallel path");
            AssertSequential(Catalog.CreatePascal(), "EGFP", "DsRed",
                AcquisitionPriority.FastestAcquisition,
                "Pascal EGFP + DsRed is not promoted through a low-quality parallel path");

            PlanCandidate qualified = Plan(Catalog.CreateLive(),
                AcquisitionPriority.FastestAcquisition, "GFP", "Alexa Fluor 647");
            TrackConfiguration track = qualified == null || qualified.Tracks.Count != 1 ?
                null : qualified.Tracks[0];
            Assert(track != null && track.IsParallel() && track.MeetsQualityLimits &&
                track.YieldRetentionInChannel1 >= RecommendationEngine.MinimumYieldRetention &&
                track.YieldRetentionInChannel2 >= RecommendationEngine.MinimumYieldRetention &&
                track.BleedThroughIntoChannel1 <= RecommendationEngine.MaximumBleedThrough &&
                track.BleedThroughIntoChannel2 <= RecommendationEngine.MaximumBleedThrough,
                "A retained Live parallel pair satisfies the common per-channel yield and bleed gates");
        }

        private static void TestAcquisitionPriorities()
        {
            MicroscopeDefinition pascal = Catalog.CreatePascal();
            PlanCandidate signalPlan = Plan(pascal, AcquisitionPriority.MaximumPhotonYield,
                "Alexa Fluor 488", "Alexa Fluor 594");
            Assert(signalPlan != null && signalPlan.Tracks.Count == 2,
                "Maximum-photon priority gives Alexa Fluor 488 + 594 separate tracks");

            bool signalPlanUsesLp560 = false;
            if (signalPlan != null)
            {
                int i;
                for (i = 0; i < signalPlan.Tracks.Count; i++)
                {
                    TrackConfiguration track = signalPlan.Tracks[i];
                    if (track.Channel1Dye != null && track.Channel1Dye.Name == "Alexa Fluor 594" &&
                        track.Channel1Filter != null && track.Channel1Filter.Name == "LP 560")
                    {
                        signalPlanUsesLp560 = true;
                    }
                }
            }
            Assert(signalPlanUsesLp560,
                "Maximum-photon Alexa Fluor 594 track uses LP 560 rather than LP 650");

            Fluorophore alexa594 = FindDye("Alexa Fluor 594");
            double lp560Yield = alexa594.EmissionProfile.FractionPassed(
                FindFilter(pascal.Channel1Filters, "LP 560"));
            double lp650Yield = alexa594.EmissionProfile.FractionPassed(
                FindFilter(pascal.Channel1Filters, "LP 650"));
            Assert(lp560Yield > 0.0 && lp650Yield / lp560Yield <
                RecommendationEngine.MinimumYieldRetention,
                "Alexa Fluor 594 through LP 650 fails the 50% per-channel yield-retention gate");

            PlanCandidate fewestTracksPlan = Plan(pascal, AcquisitionPriority.FastestAcquisition,
                "Alexa Fluor 488", "Alexa Fluor 594");
            Assert(fewestTracksPlan != null && fewestTracksPlan.Tracks.Count == 2 &&
                !fewestTracksPlan.Tracks[0].IsParallel() &&
                !fewestTracksPlan.Tracks[1].IsParallel(),
                "Fewest-tracks Alexa Fluor 488 + 594 also uses Multi Track when parallel quality fails");

            bool fewestUsesLp560 = false;
            bool eitherUsesLp650 = false;
            PlanCandidate[] plans = new PlanCandidate[] { signalPlan, fewestTracksPlan };
            int planIndex;
            for (planIndex = 0; planIndex < plans.Length; planIndex++)
            {
                if (plans[planIndex] == null)
                {
                    continue;
                }
                int trackIndex;
                for (trackIndex = 0; trackIndex < plans[planIndex].Tracks.Count; trackIndex++)
                {
                    TrackConfiguration track = plans[planIndex].Tracks[trackIndex];
                    if (track.Channel1Dye != null && track.Channel1Dye.Name == "Alexa Fluor 594" &&
                        track.Channel1Filter != null)
                    {
                        fewestUsesLp560 = fewestUsesLp560 || track.Channel1Filter.Name == "LP 560";
                        eitherUsesLp650 = eitherUsesLp650 || track.Channel1Filter.Name == "LP 650";
                    }
                    if (track.Channel2Dye != null && track.Channel2Dye.Name == "Alexa Fluor 594" &&
                        track.Channel2Filter != null)
                    {
                        fewestUsesLp560 = fewestUsesLp560 || track.Channel2Filter.Name == "LP 560";
                        eitherUsesLp650 = eitherUsesLp650 || track.Channel2Filter.Name == "LP 650";
                    }
                }
            }
            Assert(fewestUsesLp560 && !eitherUsesLp650,
                "Both Alexa Fluor 488 + 594 recommendations retain the useful LP 560 path and reject LP 650");

            string report = ComparisonReport(pascal, "Alexa Fluor 488", "Alexa Fluor 594");
            Assert(MainForm.PlansEquivalent(signalPlan, fewestTracksPlan) &&
                report.IndexOf("IDEAL CONFIGURATION") >= 0 &&
                report.IndexOf("OPTION 1") < 0 && report.IndexOf("OPTION 2") < 0,
                "Identical optimization results are merged into one shared recommendation");
            Assert(report.IndexOf("at least 50%") >= 0 && report.IndexOf("10% or less") >= 0 &&
                report.IndexOf("Signal kept:") >= 0 &&
                report.IndexOf("Estimated colour overlap:") >= 0,
                "The recommendation explains signal and overlap percentages in plain English");
            Assert(report.IndexOf("normalized emission") < 0 &&
                report.IndexOf("same-dye") < 0 && report.IndexOf("Rationale:") < 0 &&
                report.IndexOf("Spectrum:") < 0,
                "The user-facing recommendation omits repeated spectral jargon");

            MicroscopeDefinition live = Catalog.CreateLive();
            PlanCandidate liveMaximum = Plan(live, AcquisitionPriority.MaximumPhotonYield,
                "GFP", "Alexa Fluor 647");
            PlanCandidate liveFastest = Plan(live, AcquisitionPriority.FastestAcquisition,
                "GFP", "Alexa Fluor 647");
            string differentReport = ComparisonReport(live, "GFP", "Alexa Fluor 647");
            Assert(!MainForm.PlansEquivalent(liveMaximum, liveFastest) &&
                differentReport.IndexOf("OPTION 1 - BEST SIGNAL") >= 0 &&
                differentReport.IndexOf("OPTION 2 - FASTER ACQUISITION") >= 0 &&
                differentReport.IndexOf("fewer tracks") >= 0 &&
                differentReport.IndexOf("scanner-speed setting") >= 0,
                "Genuinely different results retain both options and explain track count versus scanner speed");
        }

        private static void TestLiveRedFarRedPath()
        {
            MicroscopeDefinition live = Catalog.CreateLive();
            PlanCandidate plan = Plan(live, "mCherry", "Alexa Fluor 647");
            Assert(plan != null && plan.Tracks.Count == 2 &&
                !plan.Tracks[0].IsParallel() && !plan.Tracks[1].IsParallel(),
                "Live mCherry + Alexa Fluor 647 defaults to sequential acquisition at 20% measured red spill");

            RecommendationEngine engine = new RecommendationEngine(live);
            List<string> messages = engine.FindRejectedParallelCandidateMessages(
                Dyes("mCherry", "Alexa Fluor 647"));
            Assert(messages.Count == 1 && messages[0].IndexOf("NFT 635") >= 0 &&
                messages[0].IndexOf("BP 665-750") >= 0 && messages[0].IndexOf("BP 540-625") >= 0,
                "Live red/far-red report includes the documented NFT 635 candidate path");
            Assert(messages.Count == 1 &&
                messages[0].IndexOf("combined excitation-separator/NFT path") >= 0,
                "Live red/far-red candidate evaluates combined laser rejection");

            PlanCandidate fewestTracks = Plan(live, AcquisitionPriority.FastestAcquisition,
                "mCherry", "Alexa Fluor 647");
            Assert(fewestTracks != null && fewestTracks.Tracks.Count == 2 &&
                !fewestTracks.Tracks[0].IsParallel() && !fewestTracks.Tracks[1].IsParallel(),
                "Fewest-tracks Live red/far-red mode remains sequential above the 10% bleed limit");
        }

        private static void TestPascalMainSplitterCompatibility()
        {
            PlanCandidate plan = Plan(Catalog.CreatePascal(), "ECFP", "EGFP");
            Assert(plan != null && plan.Tracks.Count == 2 &&
                !plan.Tracks[0].IsParallel() && !plan.Tracks[1].IsParallel(),
                "Pascal 458 + 488 nm uses separate tracks because no photographed HFT injects both lines");
        }

        private static void TestPascalSingleChannelBypass()
        {
            PlanCandidate plan = Plan(Catalog.CreatePascal(), "EGFP");
            Assert(plan != null && plan.Tracks.Count == 1, "Pascal EGFP produces one track");
            if (plan != null && plan.Tracks.Count == 1)
            {
                Assert(plan.Tracks[0].PlateSetting == "None",
                    "Pascal single-channel 488 nm uses secondary splitter None");
                Assert(plan.Tracks[0].FilterSetSetting == "None",
                    "Pascal leaves the lower filter-set position on None");
            }
        }

        private static void TestLiveSingleChannelBypass()
        {
            MicroscopeDefinition live = Catalog.CreateLive();
            PlanCandidate plan = Plan(live, "EGFP");
            TrackConfiguration track = plan == null || plan.Tracks.Count == 0 ? null : plan.Tracks[0];
            Assert(track != null && track.Channel1Dye != null && track.Channel1Dye.Name == "EGFP" &&
                track.Channel2Dye == null && track.PlateSetting == "None",
                "Live EGFP ChL1-only acquisition bypasses the Plate position with None");

            string report = Report(live, "EGFP");
            Assert(report.IndexOf("Plate: None") >= 0,
                "The English Live single-channel report states Plate: None");

            string snapshotPath = Environment.GetEnvironmentVariable("CONFOCAL_LIVE_SINGLE_SNAPSHOT");
            if (!String.IsNullOrEmpty(snapshotPath) && track != null)
            {
                Exception renderingError = null;
                Thread renderingThread = new Thread(delegate()
                {
                    try
                    {
                        using (BeamPathPreview preview = new BeamPathPreview())
                        using (Bitmap bitmap = new Bitmap(1000, 590))
                        {
                            preview.Size = new Size(1000, 590);
                            preview.SetConfiguration(live, track, 2, 2);
                            preview.DrawToBitmap(bitmap, new Rectangle(0, 0, 1000, 590));
                            bitmap.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch (Exception exception)
                    {
                        renderingError = exception;
                    }
                });
                renderingThread.SetApartmentState(ApartmentState.STA);
                renderingThread.Start();
                renderingThread.Join();
                Assert(renderingError == null,
                    "Live EGFP ChL1-only bypass preview renders successfully");
            }
        }

        private static void TestPascalOriginalDialogTopologyRendering()
        {
            MicroscopeDefinition pascal = Catalog.CreatePascal();
            TrackConfiguration track = new TrackConfiguration();
            track.Dyes.Add(FindDye("DsRed2"));
            track.Dyes.Add(FindDye("EGFP"));
            track.Channel1Dye = track.Dyes[0];
            track.Channel2Dye = track.Dyes[1];
            track.Channel1Filter = FindFilter(pascal.Channel1Filters, "LP 560");
            track.Channel2Filter = FindFilter(pascal.Channel2Filters, "BP 505-530");
            track.Lasers.Add(488);
            track.Lasers.Add(543);
            track.MainSplitter = "HFT 488/543/633";
            track.SecondarySplitter = "NFT 545";
            track.PlateSetting = "NFT 545";
            Assert(track != null && track.IsParallel() &&
                track.Channel1Dye != null && track.Channel1Dye.Name == "DsRed2" &&
                track.Channel1Filter != null && track.Channel1Filter.Name == "LP 560" &&
                track.Channel2Dye != null && track.Channel2Dye.Name == "EGFP" &&
                track.Channel2Filter != null && track.Channel2Filter.Name == "BP 505-530" &&
                track.PlateSetting == "NFT 545" && track.FilterSetSetting == "None",
                "Pascal EGFP + DsRed2 maps to the photographed Ch1/Ch2 topology");

            bool renderedCorrectly = false;
            Exception renderingError = null;
            Thread renderingThread = new Thread(delegate()
            {
                try
                {
                    if (track == null)
                    {
                        return;
                    }
                    using (BeamPathPreview preview = new BeamPathPreview())
                    using (Bitmap bitmap = new Bitmap(1000, 590))
                    {
                        preview.Size = new Size(1000, 590);
                        preview.SetConfiguration(pascal, track, 1, 1);
                        preview.DrawToBitmap(bitmap, new Rectangle(0, 0, 1000, 590));
                        Color expected = Color.FromArgb(244, 246, 248);
                        renderedCorrectly = ColoursClose(bitmap.GetPixel(300, 244), expected) &&
                            ColoursClose(bitmap.GetPixel(300, 330), expected) &&
                            ColoursClose(bitmap.GetPixel(300, 400), expected) &&
                            ColoursClose(bitmap.GetPixel(225, 360), expected) &&
                            ColoursClose(bitmap.GetPixel(683, 232), Color.FromArgb(214, 216, 219)) &&
                            ColoursClose(bitmap.GetPixel(683, 272), Color.White);

                        string snapshotPath = Environment.GetEnvironmentVariable("CONFOCAL_PASCAL_SNAPSHOT");
                        if (!String.IsNullOrEmpty(snapshotPath))
                        {
                            bitmap.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
                catch (Exception exception)
                {
                    renderingError = exception;
                }
            });
            renderingThread.SetApartmentState(ApartmentState.STA);
            renderingThread.Start();
            renderingThread.Join();
            Assert(renderingError == null && renderedCorrectly,
                "Pascal visualisation follows the original topology with centred Excitation controls");

            string report = Report(Catalog.CreatePascal(), AcquisitionPriority.FastestAcquisition,
                "EGFP", "DsRed2");
            Assert(report.IndexOf("Filter set: None") >= 0,
                "The English Pascal report includes the lower filter-set position");
        }

        private static void TestReferenceCurvesAndEnglishWarnings()
        {
            Assert(FindDye("DAPI").EmissionProfile.Evidence == SpectralEvidence.Measured &&
                FindDye("Alexa Fluor 488").EmissionProfile.Evidence == SpectralEvidence.Measured &&
                FindDye("FM 4-64").EmissionProfile.Evidence == SpectralEvidence.Measured,
                "Common chemical dyes use dye-specific Thermo Fisher reference curves");
            Assert(FindDye("Cy2").EmissionProfile.Evidence == SpectralEvidence.Representative,
                "A dye without a bundled reference curve remains explicitly representative");

            Fluorophore ecfp = FindDye("ECFP");
            Assert(ecfp.EnglishPascalNote.IndexOf("458 nm") >= 0 &&
                ecfp.EnglishLiveNote.IndexOf("405 nm") >= 0,
                "ECFP exposes both microscope-specific compromise warnings");
            Assert(FindDye("Alexa Fluor 680").EnglishGeneralNote.IndexOf("633/635 nm") >= 0,
                "Alexa Fluor 680 exposes its strong off-peak warning");
            Assert(FindDye("Alexa Fluor 532").EnglishLiveNote.IndexOf("561 nm") >= 0,
                "Alexa Fluor 532 exposes its Live off-peak warning");
            Assert(!String.IsNullOrEmpty(FindDye("mKate2").EnglishPascalNote) &&
                !String.IsNullOrEmpty(FindDye("mKate2").EnglishLiveNote),
                "mKate2 exposes compromise warnings on both microscopes");
            Assert(FindDye("YFP").EnglishGeneralNote.IndexOf("overlap") >= 0 &&
                FindDye("FM 4-64").EnglishGeneralNote.IndexOf("broad") >= 0,
                "YFP overlap and FM 4-64 broad-emission warnings are structured in English");

            string ecfpReport = Report(Catalog.CreatePascal(), "ECFP");
            string alexa532Report = Report(Catalog.CreateLive(), "Alexa Fluor 532");
            string yfpReport = Report(Catalog.CreatePascal(), "YFP");
            string fmReport = Report(Catalog.CreateLive(), "FM 4-64");
            Assert(ecfpReport.IndexOf("Use 458 nm") >= 0 &&
                alexa532Report.IndexOf("561 nm laser is not the ideal") >= 0 &&
                yfpReport.IndexOf("overlap") >= 0 && fmReport.IndexOf("broad red emission") >= 0,
                "The active English report surfaces compromise and overlap warnings");

            PlanCandidate greenFarRed = Plan(Catalog.CreateLive(),
                AcquisitionPriority.FastestAcquisition, "GFP", "Alexa Fluor 647");
            TrackConfiguration qualified = greenFarRed == null || greenFarRed.Tracks.Count != 1 ?
                null : greenFarRed.Tracks[0];
            Assert(qualified != null && qualified.IsParallel() &&
                qualified.SignalInChannel1 > 0.0 && qualified.SignalInChannel2 > 0.0 &&
                qualified.YieldRetentionInChannel1 >= RecommendationEngine.MinimumYieldRetention &&
                qualified.YieldRetentionInChannel2 >= RecommendationEngine.MinimumYieldRetention &&
                qualified.BleedThroughIntoChannel1 <= RecommendationEngine.MaximumBleedThrough &&
                qualified.BleedThroughIntoChannel2 <= RecommendationEngine.MaximumBleedThrough,
                "Parallel decisions retain per-channel yield and same-dye bleed coefficients within both gates");
        }

        private static void TestLiveInactiveDetectorPathRendering()
        {
            bool renderedCorrectly = false;
            Exception renderingError = null;
            Thread renderingThread = new Thread(delegate()
            {
                try
                {
                    PlanCandidate plan = Plan(Catalog.CreateLive(), "DAPI");
                    TrackConfiguration track = plan.Tracks[0];
                    using (BeamPathPreview preview = new BeamPathPreview())
                    using (Bitmap bitmap = new Bitmap(1000, 590))
                    {
                        preview.Size = new Size(1000, 590);
                        preview.SetConfiguration(Catalog.CreateLive(), track, 1, 1);
                        preview.DrawToBitmap(bitmap, new Rectangle(0, 0, 1000, 590));
                        Color upper = bitmap.GetPixel(840, 210);
                        Color lower = bitmap.GetPixel(840, 330);
                        Color expectedUpper = track.Channel1Dye != null ?
                            BeamPathPreview.FluorophoreDisplayColour(track.Channel1Dye) :
                            Color.FromArgb(87, 104, 116);
                        Color expectedLower = track.Channel2Dye != null ?
                            BeamPathPreview.FluorophoreDisplayColour(track.Channel2Dye) :
                            Color.FromArgb(87, 104, 116);
                        renderedCorrectly = ColoursClose(upper, expectedUpper) &&
                            ColoursClose(lower, expectedLower) &&
                            (track.Channel1Dye == null || track.Channel2Dye == null);
                    }
                }
                catch (Exception exception)
                {
                    renderingError = exception;
                }
            });
            renderingThread.SetApartmentState(ApartmentState.STA);
            renderingThread.Start();
            renderingThread.Join();
            Assert(renderingError == null && renderedCorrectly,
                "Live visualisation renders an OFF detector branch in the neutral path colour");
        }

        private static void TestLiveOriginalDialogTopologyRendering()
        {
            PlanCandidate plan = Plan(Catalog.CreateLive(),
                AcquisitionPriority.FastestAcquisition, "Alexa Fluor 647", "GFP");
            TrackConfiguration track = plan == null || plan.Tracks.Count == 0 ? null : plan.Tracks[0];
            Assert(track != null && track.IsParallel() &&
                track.PlateSetting == "NFT 565" && track.RearSetting == "Rear" &&
                track.Channel1Dye != null && track.Channel1Dye.Name == "Alexa Fluor 647" &&
                track.Channel1Filter != null && track.Channel1Filter.Name == "BP 575-615 + LP 655" &&
                track.Channel2Dye != null && track.Channel2Dye.Name == "GFP" &&
                track.Channel2Filter != null && track.Channel2Filter.Name == "BP 495-555",
                "Live Alexa Fluor 647 + GFP maps to the photographed Plate/filter/channel topology");

            bool renderedCorrectly = false;
            Exception renderingError = null;
            Thread renderingThread = new Thread(delegate()
            {
                try
                {
                    if (track == null)
                    {
                        return;
                    }
                    using (BeamPathPreview preview = new BeamPathPreview())
                    using (Bitmap bitmap = new Bitmap(1000, 590))
                    {
                        preview.Size = new Size(1000, 590);
                        preview.SetConfiguration(Catalog.CreateLive(), track, 1, 1);
                        preview.DrawToBitmap(bitmap, new Rectangle(0, 0, 1000, 590));
                        Color white = Color.FromArgb(239, 246, 249);
                        Color channel1Colour = BeamPathPreview.FluorophoreDisplayColour(track.Channel1Dye);
                        Color channel2Colour = BeamPathPreview.FluorophoreDisplayColour(track.Channel2Dye);
                        renderedCorrectly = ColoursClose(bitmap.GetPixel(400, 210), white) &&
                            ColoursClose(bitmap.GetPixel(400, 330), white) &&
                            ColoursClose(bitmap.GetPixel(250, 400), white) &&
                            ColoursClose(bitmap.GetPixel(130, 365), white) &&
                            ColoursClose(bitmap.GetPixel(840, 210), channel1Colour) &&
                            ColoursClose(bitmap.GetPixel(840, 330), channel2Colour) &&
                            ColoursClose(bitmap.GetPixel(763, 210), Color.FromArgb(239, 190, 45)) &&
                            ColoursClose(bitmap.GetPixel(763, 330), Color.FromArgb(54, 211, 117)) &&
                            ColoursClose(bitmap.GetPixel(300, 495), Color.FromArgb(35, 45, 56));

                        string snapshotPath = Environment.GetEnvironmentVariable("CONFOCAL_LIVE_SNAPSHOT");
                        if (!String.IsNullOrEmpty(snapshotPath))
                        {
                            bitmap.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
                catch (Exception exception)
                {
                    renderingError = exception;
                }
            });
            renderingThread.SetApartmentState(ApartmentState.STA);
            renderingThread.Start();
            renderingThread.Join();
            Assert(renderingError == null && renderedCorrectly,
                "Live visualisation follows the original topology without the former defaults notice box");
        }

        private static void TestLiveFluorophorePathColours()
        {
            Color egfp = BeamPathPreview.FluorophoreDisplayColour(FindDye("EGFP"));
            Color alexa647 = BeamPathPreview.FluorophoreDisplayColour(FindDye("Alexa Fluor 647"));
            Assert(egfp == Color.FromArgb(42, 194, 112) &&
                alexa647 == Color.FromArgb(164, 45, 80) && egfp != alexa647,
                "Live path accents follow EGFP green and Alexa Fluor 647 far-red emission colours");
        }

        private static void TestPlainLanguageMainFormRendering()
        {
            bool rendered = false;
            Exception renderingError = null;
            Thread renderingThread = new Thread(delegate()
            {
                try
                {
                    using (MainForm form = new MainForm())
                    using (Bitmap bitmap = new Bitmap(1080, 730))
                    {
                        form.StartPosition = FormStartPosition.Manual;
                        form.Location = new Point(-2000, -2000);
                        form.ShowInTaskbar = false;
                        form.Show();
                        Application.DoEvents();

                        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        ComboBox[] dyeBoxes = (ComboBox[])typeof(MainForm).GetField(
                            "fluorophoreBoxes", flags).GetValue(form);
                        SelectDyeInComboBox(dyeBoxes[0], "Alexa Fluor 488");
                        SelectDyeInComboBox(dyeBoxes[1], "Alexa Fluor 594");
                        Button analyse = (Button)typeof(MainForm).GetField(
                            "analyzeButton", flags).GetValue(form);
                        analyse.PerformClick();
                        Application.DoEvents();

                        Button show = (Button)typeof(MainForm).GetField(
                            "showButton", flags).GetValue(form);
                        ComboBox visualise = (ComboBox)typeof(MainForm).GetField(
                            "priorityBox", flags).GetValue(form);
                        RichTextBox report = (RichTextBox)typeof(MainForm).GetField(
                            "resultBox", flags).GetValue(form);
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, 1080, 730));
                        form.Hide();
                        rendered = show.Text == "Ideal Configuration" && show.Enabled &&
                            !visualise.Enabled && visualise.Text == "Ideal configuration" &&
                            report.Text.IndexOf("Signal kept:") >= 0 &&
                            report.Text.IndexOf("Estimated colour overlap:") >= 0;
                        string snapshotPath = Environment.GetEnvironmentVariable(
                            "CONFOCAL_MAIN_SNAPSHOT");
                        if (!String.IsNullOrEmpty(snapshotPath))
                        {
                            bitmap.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
                catch (Exception exception)
                {
                    renderingError = exception;
                }
            });
            renderingThread.SetApartmentState(ApartmentState.STA);
            renderingThread.Start();
            renderingThread.Join();
            Assert(renderingError == null && rendered,
                "The plain-language main window renders successfully");
        }

        private static void SelectDyeInComboBox(ComboBox box, string name)
        {
            int i;
            for (i = 0; i < box.Items.Count; i++)
            {
                Fluorophore dye = box.Items[i] as Fluorophore;
                if (dye != null && dye.Name == name)
                {
                    box.SelectedIndex = i;
                    return;
                }
            }
            throw new InvalidOperationException("Dye not found in GUI selector: " + name);
        }

        private static bool ColoursClose(Color actual, Color expected)
        {
            return Math.Abs(actual.R - expected.R) <= 3 &&
                Math.Abs(actual.G - expected.G) <= 3 &&
                Math.Abs(actual.B - expected.B) <= 3;
        }

        private static void AssertBlocked(MicroscopeDefinition microscope, string first, string second)
        {
            RecommendationEngine engine = new RecommendationEngine(microscope);
            List<Fluorophore> dyes = Dyes(first, second);
            List<string> messages = engine.FindBlockingPairMessages(dyes);
            Assert(messages.Count == 1, microscope.DisplayName + " blocks " + first + " + " + second);
        }

        private static void AssertSequential(MicroscopeDefinition microscope, string first,
            string second, AcquisitionPriority priority, string description)
        {
            PlanCandidate plan = Plan(microscope, priority, first, second);
            Assert(plan != null && plan.Tracks.Count == 2 &&
                !plan.Tracks[0].IsParallel() && !plan.Tracks[1].IsParallel(), description);
        }

        private static PlanCandidate Plan(MicroscopeDefinition microscope, params string[] names)
        {
            return Plan(microscope, AcquisitionPriority.MaximumPhotonYield, names);
        }

        private static PlanCandidate Plan(MicroscopeDefinition microscope, AcquisitionPriority priority,
            params string[] names)
        {
            RecommendationEngine engine = new RecommendationEngine(microscope);
            return engine.MakePlan(Dyes(names), priority);
        }

        private static string Report(MicroscopeDefinition microscope, params string[] names)
        {
            return Report(microscope, AcquisitionPriority.MaximumPhotonYield, names);
        }

        private static string Report(MicroscopeDefinition microscope, AcquisitionPriority priority,
            params string[] names)
        {
            List<Fluorophore> dyes = Dyes(names);
            RecommendationEngine engine = new RecommendationEngine(microscope);
            return MainForm.BuildEnglishReport(microscope, dyes, engine.MakePlan(dyes, priority),
                engine.FindBlockingPairMessages(dyes), priority);
        }

        private static string ComparisonReport(MicroscopeDefinition microscope, params string[] names)
        {
            List<Fluorophore> dyes = Dyes(names);
            RecommendationEngine engine = new RecommendationEngine(microscope);
            PlanCandidate maximum = engine.MakePlan(dyes, AcquisitionPriority.MaximumPhotonYield);
            PlanCandidate fastest = engine.MakePlan(dyes, AcquisitionPriority.FastestAcquisition);
            return MainForm.BuildEnglishComparisonReport(microscope, dyes, maximum, fastest,
                engine.FindBlockingPairMessages(dyes));
        }

        private static List<Fluorophore> Dyes(params string[] names)
        {
            List<Fluorophore> result = new List<Fluorophore>();
            int i;
            for (i = 0; i < names.Length; i++)
            {
                result.Add(FindDye(names[i]));
            }
            return result;
        }

        private static Fluorophore FindDye(string name)
        {
            List<Fluorophore> catalog = Catalog.CreateFluorophores();
            int i;
            for (i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].Name == name)
                {
                    return catalog[i];
                }
            }
            throw new InvalidOperationException("Missing catalog dye: " + name);
        }

        private static EmissionFilter FindFilter(List<EmissionFilter> filters, string name)
        {
            int i;
            for (i = 0; i < filters.Count; i++)
            {
                if (filters[i].Name == name)
                {
                    return filters[i];
                }
            }
            throw new InvalidOperationException("Missing catalog filter: " + name);
        }

        private static bool Contains(int[] values, int value)
        {
            int i;
            for (i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Assert(bool condition, string description)
        {
            if (condition)
            {
                Console.WriteLine("PASS: " + description);
                return;
            }
            failures++;
            Console.WriteLine("FAIL: " + description);
        }
    }
}
