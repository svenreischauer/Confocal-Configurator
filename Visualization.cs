using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ConfocalKonfigurator
{
    internal sealed class VisualizationForm : Form
    {
        private MicroscopeDefinition microscope;
        private PlanCandidate plan;
        private List<string> blocking;
        private ComboBox trackBox;
        private BeamPathPreview preview;
        private Label trackLabel;

        public VisualizationForm(MicroscopeDefinition microscopeDefinition, PlanCandidate recommendation,
            List<string> blockingMessages)
        {
            microscope = microscopeDefinition;
            plan = recommendation;
            blocking = blockingMessages;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Show me - " + microscope.DisplayName;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1080, 755);
            MinimumSize = new Size(900, 640);
            BackColor = Color.FromArgb(239, 244, 248);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);

            Label heading = new Label();
            heading.Text = "Recommended light path";
            heading.Font = new Font("Segoe UI", 16.0f, FontStyle.Bold, GraphicsUnit.Point);
            heading.Location = new Point(22, 16);
            heading.Size = new Size(390, 31);
            Controls.Add(heading);

            Label explanation = new Label();
            explanation.Text = "Visual guide based on the supplied Zeiss dialog layouts. Highlighted controls are the recommended settings.";
            explanation.ForeColor = Color.FromArgb(71, 85, 98);
            explanation.Location = new Point(24, 48);
            explanation.Size = new Size(760, 24);
            Controls.Add(explanation);

            trackLabel = new Label();
            trackLabel.Text = "Track:";
            trackLabel.Location = new Point(800, 22);
            trackLabel.Size = new Size(45, 22);
            trackLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(trackLabel);

            trackBox = new ComboBox();
            trackBox.DropDownStyle = ComboBoxStyle.DropDownList;
            trackBox.Location = new Point(848, 19);
            trackBox.Size = new Size(205, 25);
            trackBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            int i;
            for (i = 0; i < plan.Tracks.Count; i++)
            {
                trackBox.Items.Add(new TrackChoice(i, plan.Tracks[i]));
            }
            trackBox.SelectedIndex = 0;
            trackBox.SelectedIndexChanged += new EventHandler(TrackChanged);
            Controls.Add(trackBox);

            Label warning = new Label();
            warning.Location = new Point(24, 73);
            warning.Size = new Size(1028, 25);
            warning.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            warning.ForeColor = Color.FromArgb(148, 72, 25);
            warning.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
            if (blocking != null && blocking.Count > 0)
            {
                warning.Text = "Warning: the selected dye set contains at least one pair that cannot be reliably separated with the documented hardware.";
            }
            else
            {
                warning.Text = "The visual layout updates for the currently selected track.";
                warning.ForeColor = Color.FromArgb(53, 99, 133);
            }
            Controls.Add(warning);

            preview = new BeamPathPreview();
            preview.Location = new Point(20, 104);
            preview.Size = new Size(1035, 615);
            preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            preview.SetConfiguration(microscope, plan.Tracks[0], 1, plan.Tracks.Count);
            Controls.Add(preview);

            Button closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Location = new Point(954, 724);
            closeButton.Size = new Size(100, 27);
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Controls.Add(closeButton);
            AcceptButton = closeButton;
        }

        private void TrackChanged(object sender, EventArgs e)
        {
            TrackChoice choice = trackBox.SelectedItem as TrackChoice;
            if (choice != null)
            {
                preview.SetConfiguration(microscope, choice.Track, choice.Index + 1, plan.Tracks.Count);
            }
        }
    }

    internal sealed class TrackChoice
    {
        public int Index;
        public TrackConfiguration Track;

        public TrackChoice(int index, TrackConfiguration track)
        {
            Index = index;
            Track = track;
        }

        public override string ToString()
        {
            string mode = Track.IsParallel() ? "simultaneous candidate" : "single-colour";
            return "Track " + (Index + 1).ToString() + " - " + mode + ": " + JoinDyes(Track.Dyes);
        }

        private static string JoinDyes(List<Fluorophore> dyes)
        {
            string result = String.Empty;
            int i;
            for (i = 0; i < dyes.Count; i++)
            {
                if (i > 0)
                {
                    result += " + ";
                }
                result += dyes[i].Name;
            }
            return result;
        }
    }

    internal sealed class BeamPathPreview : Control
    {
        private MicroscopeDefinition microscope;
        private TrackConfiguration track;
        private int trackNumber;
        private int trackCount;

        public BeamPathPreview()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        public void SetConfiguration(MicroscopeDefinition microscopeDefinition, TrackConfiguration configuration,
            int selectedTrackNumber, int totalTrackCount)
        {
            microscope = microscopeDefinition;
            track = configuration;
            trackNumber = selectedTrackNumber;
            trackCount = totalTrackCount;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (microscope == null || track == null)
            {
                e.Graphics.Clear(Color.White);
                return;
            }

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            const float designWidth = 1000.0f;
            const float designHeight = 590.0f;
            float scaleX = ClientSize.Width / designWidth;
            float scaleY = ClientSize.Height / designHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offsetX = (ClientSize.Width - designWidth * scale) / 2.0f;
            float offsetY = (ClientSize.Height - designHeight * scale) / 2.0f;
            graphics.TranslateTransform(offsetX, offsetY);
            graphics.ScaleTransform(scale, scale);

            if (microscope.Kind == MicroscopeKind.Pascal)
            {
                DrawPascal(graphics);
            }
            else
            {
                DrawLive(graphics);
            }
            graphics.ResetTransform();
        }

        private void DrawPascal(Graphics graphics)
        {
            graphics.Clear(Color.FromArgb(227, 229, 232));
            Rectangle window = new Rectangle(16, 14, 968, 560);
            using (SolidBrush background = new SolidBrush(Color.FromArgb(231, 232, 233)))
            using (Pen border = new Pen(Color.FromArgb(72, 77, 84), 2.0f))
            {
                graphics.FillRectangle(background, window);
                graphics.DrawRectangle(border, window);
            }
            DrawGradientTitle(graphics, new Rectangle(17, 15, 966, 36), "Configuration Control", Color.FromArgb(20, 65, 145));
            DrawPascalTabs(graphics);

            Rectangle assignment = new Rectangle(38, 159, 610, 363);
            using (SolidBrush panel = new SolidBrush(Color.FromArgb(204, 206, 210)))
            using (Pen border = new Pen(Color.FromArgb(95, 99, 105), 1.0f))
            {
                graphics.FillRectangle(panel, assignment);
                graphics.DrawRectangle(border, assignment);
            }
            using (SolidBrush header = new SolidBrush(Color.FromArgb(72, 73, 81)))
            {
                graphics.FillRectangle(header, new Rectangle(39, 160, 608, 30));
            }
            DrawText(graphics, "Beam Path and Channel Assignment", 14.0f, FontStyle.Bold, Color.White,
                new RectangleF(52, 164, 430, 24), StringAlignment.Near);
            DrawText(graphics, "Track " + trackNumber.ToString() + " of " + trackCount.ToString() +
                (track.IsParallel() ? "  -  simultaneous candidate" : "  -  single-colour"), 9.0f, FontStyle.Regular,
                Color.FromArgb(45, 54, 65), new RectangleF(680, 158, 270, 24), StringAlignment.Near);

            DrawPascalLightPath(graphics);

            Rectangle laserPanel = new Rectangle(664, 188, 285, 255);
            DrawClassicPanel(graphics, laserPanel, "Excitation", Color.FromArgb(72, 73, 81));
            DrawPascalLaserRows(graphics, laserPanel);

            Rectangle detectorPanel = new Rectangle(664, 458, 285, 92);
            DrawClassicPanel(graphics, detectorPanel, "Detector settings", Color.FromArgb(72, 73, 81));
            DrawText(graphics, "Gain: 700", 10.0f, FontStyle.Bold, Color.FromArgb(29, 42, 53),
                new RectangleF(685, 495, 120, 18), StringAlignment.Near);
            DrawText(graphics, "Pinhole: 1.5 Airy units", 10.0f, FontStyle.Bold, Color.FromArgb(29, 42, 53),
                new RectangleF(685, 518, 220, 18), StringAlignment.Near);

            DrawText(graphics, "Blue outlines and labels mark the controls to set. This is a visual guide, not a hardware control panel.",
                9.0f, FontStyle.Italic, Color.FromArgb(77, 82, 88), new RectangleF(42, 535, 605, 20), StringAlignment.Near);
        }

        private void DrawPascalTabs(Graphics graphics)
        {
            DrawClassicTab(graphics, new Rectangle(40, 64, 205, 47), "Channel Mode", true);
            DrawClassicTab(graphics, new Rectangle(246, 64, 205, 47), "Lambda Mode", false);
            DrawClassicTab(graphics, new Rectangle(452, 64, 205, 47), "Online Fingerprinting", false);
            bool multi = trackCount > 1;
            DrawClassicTab(graphics, new Rectangle(40, 113, 205, 40), "Single Track", !multi);
            DrawClassicTab(graphics, new Rectangle(246, 113, 205, 40), "Multi Track", multi);
            DrawClassicTab(graphics, new Rectangle(452, 113, 205, 40), "Ratio", false);
        }

        private void DrawPascalLightPath(Graphics graphics)
        {
            Color activePath = Color.FromArgb(244, 246, 248);
            Color inactivePath = Color.FromArgb(158, 162, 167);
            bool channel1Active = track.Channel1Dye != null;
            bool channel2Active = track.Channel2Dye != null;
            using (Pen commonPath = new Pen(activePath, 5.0f))
            using (Pen channel1Path = new Pen(channel1Active ? activePath : inactivePath, 5.0f))
            using (Pen channel2Path = new Pen(channel2Active ? activePath : inactivePath, 5.0f))
            using (Pen excitationPath = new Pen(activePath, 5.0f))
            {
                // Match the photographed Pascal topology: the lower filter-set
                // position and HFT are in the common vertical path. The upper
                // Plate/NFT position separates Ch2 from the continuing Ch1 arm.
                graphics.DrawLine(commonPath, 225, 500, 225, 330);
                graphics.DrawLine(channel1Path, 225, 330, 225, 244);
                graphics.DrawLine(channel1Path, 225, 244, 430, 244);
                graphics.DrawLine(channel1Path, 472, 244, 495, 244);
                graphics.DrawLine(channel2Path, 247, 330, 430, 330);
                graphics.DrawLine(channel2Path, 472, 330, 495, 330);
                graphics.DrawLine(excitationPath, 247, 400, 470, 400);
            }

            DrawText(graphics, "Specimen", 10.5f, FontStyle.Bold, Color.FromArgb(46, 51, 58),
                new RectangleF(57, 486, 105, 22), StringAlignment.Far);
            using (Pen specimen = new Pen(Color.FromArgb(75, 79, 84), 4.0f))
            {
                graphics.DrawLine(specimen, 166, 500, 286, 500);
            }

            DrawPascalOpticControl(graphics, new Rectangle(203, 308, 44, 44), "Secondary splitter",
                track.PlateSetting);
            DrawPascalOpticControl(graphics, new Rectangle(203, 378, 44, 44), "Main beam splitter",
                track.MainSplitter);
            DrawPascalOpticControl(graphics, new Rectangle(203, 441, 44, 44), "Filter set",
                track.FilterSetSetting);

            DrawPascalEmissionFilter(graphics, new Rectangle(430, 222, 42, 44), track.Channel1Dye,
                track.Channel1Filter);
            DrawPascalEmissionFilter(graphics, new Rectangle(430, 308, 42, 44), track.Channel2Dye,
                track.Channel2Filter);
            DrawPascalChannel(graphics, new Rectangle(495, 207, 137, 72), "Ch1", "long-wavelength path",
                track.Channel1Dye);
            DrawPascalChannel(graphics, new Rectangle(495, 293, 137, 72), "Ch2", "short-wavelength path",
                track.Channel2Dye);
            DrawPascalExcitationPort(graphics, new Rectangle(470, 374, 162, 52));
        }

        private void DrawPascalOpticControl(Graphics graphics, Rectangle rectangle, string label, string value)
        {
            Color selected = Color.FromArgb(31, 74, 150);
            Rectangle valueRectangle = new Rectangle(49, rectangle.Y + 16, 142, 22);
            DrawText(graphics, label, 7.7f, FontStyle.Regular, Color.FromArgb(68, 72, 77),
                new RectangleF(49, rectangle.Y - 2, 142, 17), StringAlignment.Far);
            using (SolidBrush valueFill = new SolidBrush(Color.FromArgb(225, 233, 246)))
            using (Pen valueBorder = new Pen(selected, 1.0f))
            {
                graphics.FillRectangle(valueFill, valueRectangle);
                graphics.DrawRectangle(valueBorder, valueRectangle);
            }
            DrawText(graphics, Trim(value, 22), 8.3f, FontStyle.Bold, selected,
                new RectangleF(valueRectangle.X + 4, valueRectangle.Y + 1, valueRectangle.Width - 8,
                valueRectangle.Height - 2), StringAlignment.Center);

            using (SolidBrush outer = new SolidBrush(Color.FromArgb(205, 207, 210)))
            using (Pen selectedBorder = new Pen(selected, 2.0f))
            using (Pen innerBorder = new Pen(Color.FromArgb(67, 70, 75), 1.0f))
            using (Pen diagonal = new Pen(Color.FromArgb(55, 59, 64), 3.0f))
            {
                graphics.FillRectangle(outer, rectangle);
                graphics.DrawRectangle(selectedBorder, rectangle);
                Rectangle inner = new Rectangle(rectangle.X + 8, rectangle.Y + 7,
                    rectangle.Width - 16, rectangle.Height - 14);
                graphics.DrawRectangle(innerBorder, inner);
                graphics.DrawLine(diagonal, inner.Left + 2, inner.Bottom - 2, inner.Right - 2, inner.Top + 2);
            }
        }

        private void DrawPascalEmissionFilter(Graphics graphics, Rectangle rectangle, Fluorophore dye,
            EmissionFilter filter)
        {
            bool enabled = dye != null;
            Color selected = Color.FromArgb(31, 74, 150);
            DrawText(graphics, "Emission filter", 7.3f, FontStyle.Regular, Color.FromArgb(68, 72, 77),
                new RectangleF(rectangle.X - 101, rectangle.Y - 25, 92, 15), StringAlignment.Far);
            DrawText(graphics, enabled ? filter.Name : "None", 8.5f, FontStyle.Bold,
                enabled ? selected : Color.FromArgb(105, 109, 114),
                new RectangleF(rectangle.X - 105, rectangle.Y - 10, 96, 18), StringAlignment.Far);
            using (SolidBrush outer = new SolidBrush(Color.FromArgb(205, 207, 210)))
            using (Pen border = new Pen(enabled ? selected : Color.FromArgb(103, 107, 112), enabled ? 2.0f : 1.0f))
            using (SolidBrush glass = new SolidBrush(enabled ? FluorophoreDisplayColour(dye) : Color.FromArgb(137, 140, 144)))
            using (Pen glassBorder = new Pen(Color.FromArgb(58, 62, 67), 1.0f))
            {
                graphics.FillRectangle(outer, rectangle);
                graphics.DrawRectangle(border, rectangle);
                Rectangle slot = new Rectangle(rectangle.X + 16, rectangle.Y + 6, 11, rectangle.Height - 12);
                graphics.FillRectangle(glass, slot);
                graphics.DrawRectangle(glassBorder, slot);
            }
        }

        private void DrawPascalChannel(Graphics graphics, Rectangle rectangle, string channel, string arm,
            Fluorophore dye)
        {
            bool enabled = dye != null;
            Color selected = Color.FromArgb(31, 74, 150);
            Color dyeColour = enabled ? FluorophoreDisplayColour(dye) : Color.FromArgb(118, 121, 125);
            using (SolidBrush outer = new SolidBrush(Color.FromArgb(211, 213, 216)))
            using (Pen border = new Pen(enabled ? selected : Color.FromArgb(103, 107, 112), enabled ? 2.0f : 1.0f))
            {
                graphics.FillRectangle(outer, rectangle);
                graphics.DrawRectangle(border, rectangle);
            }
            DrawCheckbox(graphics, rectangle.X + 8, rectangle.Y + 8, enabled, selected);
            using (SolidBrush swatch = new SolidBrush(dyeColour))
            using (Pen swatchBorder = new Pen(Color.FromArgb(58, 62, 67), 1.0f))
            {
                graphics.FillRectangle(swatch, new Rectangle(rectangle.X + 35, rectangle.Y + 7, 49, 18));
                graphics.DrawRectangle(swatchBorder, new Rectangle(rectangle.X + 35, rectangle.Y + 7, 49, 18));
            }
            DrawText(graphics, channel, 10.0f, FontStyle.Bold, Color.FromArgb(42, 46, 51),
                new RectangleF(rectangle.X + 89, rectangle.Y + 5, 42, 22), StringAlignment.Center);
            DrawText(graphics, enabled ? Trim(dye.Name, 21) : "OFF", 8.8f, enabled ? FontStyle.Bold : FontStyle.Regular,
                enabled ? selected : Color.FromArgb(105, 109, 114),
                new RectangleF(rectangle.X + 6, rectangle.Y + 32, rectangle.Width - 12, 18), StringAlignment.Center);
            DrawText(graphics, arm, 7.2f, FontStyle.Regular, Color.FromArgb(80, 84, 89),
                new RectangleF(rectangle.X + 6, rectangle.Y + 51, rectangle.Width - 12, 15), StringAlignment.Center);
        }

        private void DrawPascalExcitationPort(Graphics graphics, Rectangle rectangle)
        {
            Color selected = Color.FromArgb(31, 74, 150);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(211, 213, 216)))
            using (Pen border = new Pen(selected, 2.0f))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
            }
            Point[] triangle = new Point[]
            {
                new Point(rectangle.X + 13, rectangle.Y + 39),
                new Point(rectangle.X + 31, rectangle.Y + 7),
                new Point(rectangle.X + 49, rectangle.Y + 39)
            };
            using (SolidBrush warning = new SolidBrush(Color.FromArgb(242, 216, 54)))
            using (Pen warningBorder = new Pen(Color.FromArgb(55, 59, 64), 2.0f))
            using (SolidBrush laser = new SolidBrush(Color.FromArgb(218, 50, 42)))
            {
                graphics.FillPolygon(warning, triangle);
                graphics.DrawPolygon(warningBorder, triangle);
                graphics.FillEllipse(laser, new Rectangle(rectangle.X + 25, rectangle.Y + 23, 12, 12));
            }
            DrawText(graphics, "Excitation", 9.5f, FontStyle.Bold, Color.FromArgb(42, 46, 51),
                new RectangleF(rectangle.X + 54, rectangle.Y + 5, rectangle.Width - 60, 20), StringAlignment.Near);
            DrawText(graphics, ActiveLaserLabel(), 8.0f, FontStyle.Bold, selected,
                new RectangleF(rectangle.X + 54, rectangle.Y + 25, rectangle.Width - 60, 18), StringAlignment.Near);
        }

        private void DrawPascalLaserRows(Graphics graphics, Rectangle panel)
        {
            int[] allLasers = new int[] { 458, 488, 543, 633 };
            DrawText(graphics, "Active line", 7.5f, FontStyle.Regular, Color.FromArgb(61, 65, 70),
                new RectangleF(panel.X + 10, panel.Y + 66, 104, 14), StringAlignment.Center);
            DrawText(graphics, "Transmission", 7.5f, FontStyle.Regular, Color.FromArgb(61, 65, 70),
                new RectangleF(panel.X + 120, panel.Y + 66, 76, 14), StringAlignment.Center);
            int i;
            for (i = 0; i < allLasers.Length; i++)
            {
                int wavelength = allLasers[i];
                bool active = ContainsLaser(wavelength);
                int y = panel.Y + 83 + i * 38;
                DrawCheckbox(graphics, panel.X + 18, y, active, Color.FromArgb(29, 99, 160));
                DrawText(graphics, wavelength.ToString() + " nm", 10.0f, active ? FontStyle.Bold : FontStyle.Regular,
                    active ? Color.FromArgb(24, 50, 78) : Color.FromArgb(118, 122, 127),
                    new RectangleF(panel.X + 45, y - 1, 75, 20), StringAlignment.Near);
                using (SolidBrush field = new SolidBrush(active ? Color.White : Color.FromArgb(211, 214, 217)))
                using (Pen fieldBorder = new Pen(Color.FromArgb(104, 109, 115), 1.0f))
                {
                    graphics.FillRectangle(field, new Rectangle(panel.X + 132, y - 2, 54, 22));
                    graphics.DrawRectangle(fieldBorder, new Rectangle(panel.X + 132, y - 2, 54, 22));
                }
                string transmission = wavelength == 488 ? "7%" : "50%";
                DrawText(graphics, active ? transmission : "off", 9.0f, FontStyle.Bold,
                    active ? Color.FromArgb(26, 49, 71) : Color.FromArgb(125, 128, 132),
                    new RectangleF(panel.X + 136, y, 47, 17), StringAlignment.Center);
                using (Pen slider = new Pen(active ? Color.FromArgb(55, 61, 69) : Color.FromArgb(150, 154, 158), 2.0f))
                {
                    graphics.DrawLine(slider, panel.X + 198, y + 9, panel.X + 254, y + 9);
                }
            }
        }

        private void DrawLive(Graphics graphics)
        {
            graphics.Clear(Color.FromArgb(31, 39, 48));
            Rectangle window = new Rectangle(14, 12, 972, 562);
            using (SolidBrush panel = new SolidBrush(Color.FromArgb(35, 45, 56)))
            using (Pen border = new Pen(Color.FromArgb(97, 140, 167), 2.0f))
            {
                graphics.FillRectangle(panel, window);
                graphics.DrawRectangle(border, window);
            }
            DrawGradientTitle(graphics, new Rectangle(15, 13, 970, 38), "Light Path", Color.FromArgb(39, 156, 191));
            using (Pen frame = new Pen(Color.FromArgb(104, 151, 179), 1.0f))
            using (Pen line = new Pen(Color.FromArgb(126, 143, 158), 1.0f))
            {
                graphics.DrawRectangle(frame, new Rectangle(36, 57, 928, 31));
                graphics.DrawLine(line, 36, 137, 964, 137);
            }
            DrawText(graphics, "LIVE", 14.0f, FontStyle.Bold, Color.White,
                new RectangleF(36, 57, 928, 31), StringAlignment.Center);
            DrawText(graphics, "Track " + trackNumber.ToString() + " of " + trackCount.ToString() +
                (track.IsParallel() ? "  -  simultaneous candidate" : "  -  single-colour"), 10.0f, FontStyle.Bold,
                Color.FromArgb(224, 235, 241), new RectangleF(40, 101, 480, 25), StringAlignment.Near);

            DrawLiveLightPath(graphics);
            DrawLiveLaserSelector(graphics);
            DrawLiveEmissionFilter(graphics, new Rectangle(730, 181, 66, 58), "ChL1",
                track.Channel1Dye, track.Channel1Filter);
            DrawLiveEmissionFilter(graphics, new Rectangle(730, 301, 66, 58), "ChL2",
                track.Channel2Dye, track.Channel2Filter);
            DrawLiveDetector(graphics, new Rectangle(900, 181, 60, 58), "ChL1", track.Channel1Dye);
            DrawLiveDetector(graphics, new Rectangle(900, 301, 60, 58), "ChL2", track.Channel2Dye);

            DrawText(graphics, "Bright outlines mark controls; post-filter paths use fluorophore colours; inactive paths are dimmed.",
                8.7f, FontStyle.Italic, Color.FromArgb(174, 194, 207),
                new RectangleF(34, 548, 930, 18), StringAlignment.Center);
        }

        private void DrawLiveLightPath(Graphics graphics)
        {
            Color inactivePath = Color.FromArgb(87, 104, 116);
            Color white = Color.FromArgb(239, 246, 249);
            bool channel1Active = track.Channel1Dye != null;
            bool channel2Active = track.Channel2Dye != null;
            Color channel1Colour = channel1Active ? FluorophoreDisplayColour(track.Channel1Dye) : inactivePath;
            Color channel2Colour = channel2Active ? FluorophoreDisplayColour(track.Channel2Dye) : inactivePath;
            using (Pen commonPath = new Pen(white, 5.0f))
            using (Pen upperPath = new Pen(channel1Active ? white : inactivePath, 5.0f))
            using (Pen lowerPath = new Pen(channel2Active ? white : inactivePath, 5.0f))
            using (Pen upperDetectorPath = new Pen(channel1Colour, 5.0f))
            using (Pen lowerDetectorPath = new Pen(channel2Colour, 5.0f))
            {
                // Original Live layout: Plate splits the lower ChL2 arm from the
                // ChL1 path continuing upwards. Laser coupling and Rear remain
                // below it in the common vertical specimen path.
                graphics.DrawLine(commonPath, 130, 490, 130, 330);
                graphics.DrawLine(upperPath, 130, 330, 130, 210);
                graphics.DrawLine(upperPath, 130, 210, 730, 210);
                graphics.DrawLine(lowerPath, 160, 330, 730, 330);
                graphics.DrawLine(commonPath, 160, 400, 395, 400);
                graphics.DrawLine(upperDetectorPath, 796, 210, 900, 210);
                graphics.DrawLine(lowerDetectorPath, 796, 330, 900, 330);
            }

            DrawLiveOpticControl(graphics, new Rectangle(100, 301, 60, 58), "Plate position",
                track.PlateSetting, true, Color.FromArgb(235, 241, 232));
            DrawLiveLensTile(graphics, new Rectangle(100, 371, 60, 56),
                Color.FromArgb(163, 184, 187), false);
            DrawLiveOpticControl(graphics, new Rectangle(100, 432, 60, 56), "Rear position",
                track.RearSetting, false, Color.FromArgb(39, 49, 58));

            using (Pen specimenPath = new Pen(Color.FromArgb(239, 246, 249), 4.0f))
            using (SolidBrush stage = new SolidBrush(Color.FromArgb(229, 237, 240)))
            using (Pen stageBorder = new Pen(Color.FromArgb(154, 179, 192), 1.0f))
            {
                graphics.DrawLine(specimenPath, 130, 487, 130, 499);
                Point[] stageShape = new Point[]
                {
                    new Point(94, 505), new Point(130, 493),
                    new Point(166, 505), new Point(130, 518)
                };
                graphics.FillPolygon(stage, stageShape);
                graphics.DrawPolygon(stageBorder, stageShape);
            }
            DrawText(graphics, "Specimen / stage", 9.0f, FontStyle.Bold, Color.FromArgb(226, 234, 239),
                new RectangleF(70, 519, 120, 18), StringAlignment.Center);
        }

        private void DrawLiveOpticControl(Graphics graphics, Rectangle rectangle, string label, string value,
            bool labelOnLeft, Color lensColour)
        {
            DrawLiveLensTile(graphics, rectangle, lensColour, true);
            float labelX = labelOnLeft ? rectangle.X - 88 : rectangle.Right + 8;
            StringAlignment alignment = labelOnLeft ? StringAlignment.Far : StringAlignment.Near;
            DrawText(graphics, label, 7.4f, FontStyle.Regular, Color.FromArgb(183, 201, 211),
                new RectangleF(labelX, rectangle.Y + 8, 80, 16), alignment);
            DrawText(graphics, Trim(value, 14), 8.8f, FontStyle.Bold, Color.FromArgb(63, 210, 222),
                new RectangleF(labelX, rectangle.Y + 25, 80, 20), alignment);
        }

        private static void DrawLiveLensTile(Graphics graphics, Rectangle rectangle, Color lensColour,
            bool selected)
        {
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(44, 57, 67)))
            using (Pen border = new Pen(selected ? Color.FromArgb(84, 197, 211) :
                Color.FromArgb(102, 130, 145), selected ? 2.0f : 1.0f))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
            }

            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(rectangle.X + rectangle.Width / 2.0f,
                rectangle.Y + rectangle.Height / 2.0f);
            graphics.RotateTransform(-35.0f);
            using (SolidBrush lens = new SolidBrush(lensColour))
            using (Pen lensBorder = new Pen(Color.FromArgb(218, 231, 236), 1.2f))
            {
                graphics.FillEllipse(lens, -19, -10, 38, 20);
                graphics.DrawEllipse(lensBorder, -19, -10, 38, 20);
            }
            graphics.Restore(state);
        }

        private void DrawLiveLaserSelector(Graphics graphics)
        {
            Rectangle source = new Rectangle(395, 372, 64, 56);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(43, 55, 66)))
            using (Pen border = new Pen(Color.FromArgb(84, 197, 211), 2.0f))
            using (SolidBrush tube = new SolidBrush(Color.FromArgb(232, 237, 239)))
            using (Pen tubeBorder = new Pen(Color.FromArgb(103, 122, 134), 1.0f))
            using (SolidBrush band = new SolidBrush(Color.FromArgb(218, 60, 48)))
            {
                graphics.FillRectangle(fill, source);
                graphics.DrawRectangle(border, source);
                Rectangle tubeRectangle = new Rectangle(source.X + 16, source.Y + 22, 34, 13);
                graphics.FillEllipse(tube, tubeRectangle);
                graphics.DrawEllipse(tubeBorder, tubeRectangle);
                graphics.FillRectangle(band, new Rectangle(source.X + 21, source.Y + 23, 5, 11));
            }
            DrawText(graphics, "Laser", 9.5f, FontStyle.Bold, Color.FromArgb(229, 238, 243),
                new RectangleF(source.Right + 8, source.Y + 17, 70, 20), StringAlignment.Near);

            Rectangle box = new Rectangle(300, 438, 410, 48);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(28, 39, 49)))
            using (Pen border = new Pen(Color.FromArgb(98, 139, 163), 1.5f))
            {
                graphics.FillRectangle(fill, box);
                graphics.DrawRectangle(border, box);
            }
            DrawText(graphics, "Active lines", 7.8f, FontStyle.Regular, Color.FromArgb(177, 198, 210),
                new RectangleF(box.X + 9, box.Y + 14, 76, 20), StringAlignment.Near);
            int[] allLasers = new int[] { 405, 488, 561, 635 };
            int i;
            for (i = 0; i < allLasers.Length; i++)
            {
                int wavelength = allLasers[i];
                bool active = ContainsLaser(wavelength);
                int x = box.X + 94 + i * 77;
                using (SolidBrush toggle = new SolidBrush(active ? LaserColour(wavelength) : Color.FromArgb(47, 61, 72)))
                using (Pen toggleBorder = new Pen(active ? Color.White : Color.FromArgb(116, 137, 149), 1.0f))
                {
                    graphics.FillRectangle(toggle, new Rectangle(x, box.Y + 14, 17, 17));
                    graphics.DrawRectangle(toggleBorder, new Rectangle(x, box.Y + 14, 17, 17));
                }
                if (active)
                {
                    using (Pen tick = new Pen(Color.White, 2.0f))
                    {
                        graphics.DrawLine(tick, x + 3, box.Y + 23, x + 7, box.Y + 27);
                        graphics.DrawLine(tick, x + 7, box.Y + 27, x + 14, box.Y + 18);
                    }
                }
                DrawText(graphics, wavelength.ToString(), 8.5f, active ? FontStyle.Bold : FontStyle.Regular,
                    active ? Color.White : Color.FromArgb(150, 166, 176),
                    new RectangleF(x + 20, box.Y + 12, 43, 21), StringAlignment.Near);
            }
        }

        private void DrawLiveEmissionFilter(Graphics graphics, Rectangle rectangle, string channel,
            Fluorophore dye, EmissionFilter filter)
        {
            bool enabled = dye != null;
            Color lensColour = enabled ? LiveFilterColour(filter) : Color.FromArgb(91, 104, 113);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(43, 55, 66)))
            using (Pen border = new Pen(enabled ? Color.FromArgb(84, 197, 211) : Color.FromArgb(96, 119, 132),
                enabled ? 2.0f : 1.0f))
            using (SolidBrush lensEdge = new SolidBrush(Color.FromArgb(218, 230, 234)))
            using (SolidBrush lens = new SolidBrush(lensColour))
            using (Pen lensBorder = new Pen(Color.FromArgb(141, 166, 180), 1.0f))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
                graphics.FillEllipse(lensEdge, new Rectangle(rectangle.X + 19, rectangle.Y + 7, 28, 44));
                graphics.FillEllipse(lens, new Rectangle(rectangle.X + 23, rectangle.Y + 9, 20, 40));
                graphics.DrawEllipse(lensBorder, new Rectangle(rectangle.X + 19, rectangle.Y + 7, 28, 44));
            }
            DrawText(graphics, channel + " emission filter", 7.2f, FontStyle.Regular,
                Color.FromArgb(174, 194, 205), new RectangleF(rectangle.X - 250, rectangle.Bottom + 1, 238, 14),
                StringAlignment.Far);
            DrawText(graphics, enabled ? Trim(filter.Name, 34) : "No filter selected", 9.0f, FontStyle.Bold,
                enabled ? lensColour : Color.FromArgb(136, 153, 163),
                new RectangleF(rectangle.X - 250, rectangle.Bottom + 14, 238, 19), StringAlignment.Far);
        }

        private void DrawLiveDetector(Graphics graphics, Rectangle rectangle, string channel, Fluorophore dye)
        {
            bool enabled = dye != null;
            Color dyeColour = enabled ? FluorophoreDisplayColour(dye) : Color.FromArgb(96, 119, 132);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(43, 55, 66)))
            using (Pen border = new Pen(dyeColour,
                enabled ? 2.0f : 1.0f))
            using (Pen arrow = new Pen(enabled ? dyeColour : Color.FromArgb(129, 149, 160), 3.0f))
            using (SolidBrush sensor = new SolidBrush(enabled ? Color.FromArgb(238, 240, 226) :
                Color.FromArgb(127, 139, 145)))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
                int centreY = rectangle.Y + rectangle.Height / 2;
                graphics.DrawLine(arrow, rectangle.X + 10, centreY, rectangle.X + 38, centreY);
                graphics.DrawLine(arrow, rectangle.X + 30, centreY - 8, rectangle.X + 38, centreY);
                graphics.DrawLine(arrow, rectangle.X + 30, centreY + 8, rectangle.X + 38, centreY);
                graphics.FillRectangle(sensor, new Rectangle(rectangle.X + 44, rectangle.Y + 9, 8,
                    rectangle.Height - 18));
            }
            DrawLiveCheckbox(graphics, rectangle.X - 29, rectangle.Y + 19, enabled, dyeColour);
            DrawText(graphics, channel, 10.5f, FontStyle.Bold, Color.FromArgb(235, 242, 245),
                new RectangleF(rectangle.X - 6, rectangle.Bottom + 1, rectangle.Width + 12, 18), StringAlignment.Center);
            DrawText(graphics, enabled ? Trim(dye.Name, 18) : "OFF", 7.8f,
                enabled ? FontStyle.Bold : FontStyle.Regular,
                enabled ? dyeColour : Color.FromArgb(143, 158, 167),
                new RectangleF(rectangle.X - 34, rectangle.Bottom + 19, rectangle.Width + 68, 17), StringAlignment.Center);
        }

        private static void DrawLiveCheckbox(Graphics graphics, int x, int y, bool checkedValue, Color accent)
        {
            Rectangle box = new Rectangle(x, y, 18, 18);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(39, 51, 61)))
            using (Pen border = new Pen(Color.FromArgb(137, 158, 169), 1.0f))
            {
                graphics.FillRectangle(fill, box);
                graphics.DrawRectangle(border, box);
            }
            if (checkedValue)
            {
                using (Pen tick = new Pen(accent, 3.0f))
                {
                    graphics.DrawLine(tick, x + 3, y + 10, x + 7, y + 14);
                    graphics.DrawLine(tick, x + 7, y + 14, x + 15, y + 4);
                }
            }
        }

        private bool ContainsLaser(int wavelength)
        {
            int i;
            for (i = 0; i < track.Lasers.Count; i++)
            {
                if (track.Lasers[i] == wavelength)
                {
                    return true;
                }
            }
            return false;
        }

        private string ActiveLaserLabel()
        {
            if (track.Lasers.Count == 0)
            {
                return "No line active";
            }
            string result = String.Empty;
            int i;
            for (i = 0; i < track.Lasers.Count; i++)
            {
                if (i > 0)
                {
                    result += " + ";
                }
                result += track.Lasers[i].ToString();
            }
            return result + " nm";
        }

        internal static Color FluorophoreDisplayColour(Fluorophore dye)
        {
            if (dye == null)
            {
                return Color.FromArgb(118, 121, 125);
            }
            int wavelength = dye.EmissionPeak;
            if (wavelength < 480) return Color.FromArgb(79, 126, 231);
            if (wavelength < 505) return Color.FromArgb(50, 194, 221);
            if (wavelength < 540) return Color.FromArgb(42, 194, 112);
            if (wavelength < 570) return Color.FromArgb(154, 203, 53);
            if (wavelength < 610) return Color.FromArgb(235, 169, 45);
            if (wavelength < 660) return Color.FromArgb(220, 67, 54);
            return Color.FromArgb(164, 45, 80);
        }

        private static Color LiveFilterColour(EmissionFilter filter)
        {
            if (filter == null || filter.Bands.Count == 0)
            {
                return Color.FromArgb(91, 104, 113);
            }
            SpectralBand firstBand = filter.Bands[0];
            int wavelength;
            if (filter.Name.StartsWith("LP "))
            {
                wavelength = firstBand.Start + 25;
            }
            else
            {
                wavelength = (firstBand.Start + firstBand.End) / 2;
            }
            if (wavelength < 480) return Color.FromArgb(48, 164, 228);
            if (wavelength < 505) return Color.FromArgb(40, 207, 207);
            if (wavelength < 540) return Color.FromArgb(54, 211, 117);
            if (wavelength < 570) return Color.FromArgb(155, 218, 68);
            if (wavelength < 620) return Color.FromArgb(239, 190, 45);
            if (wavelength < 660) return Color.FromArgb(226, 91, 52);
            return Color.FromArgb(185, 40, 77);
        }

        private static Color LaserColour(int wavelength)
        {
            if (wavelength == 405) return Color.FromArgb(118, 96, 225);
            if (wavelength == 458) return Color.FromArgb(61, 118, 229);
            if (wavelength == 488) return Color.FromArgb(34, 191, 231);
            if (wavelength == 543) return Color.FromArgb(86, 193, 73);
            if (wavelength == 561) return Color.FromArgb(238, 170, 45);
            return Color.FromArgb(221, 71, 67);
        }

        private static void DrawClassicPanel(Graphics graphics, Rectangle rectangle, string title, Color titleColour)
        {
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(214, 216, 219)))
            using (Pen border = new Pen(Color.FromArgb(82, 87, 94), 1.0f))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
            }
            using (SolidBrush header = new SolidBrush(titleColour))
            {
                graphics.FillRectangle(header, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 29));
            }
            DrawText(graphics, title, 12.0f, FontStyle.Bold, Color.White,
                new RectangleF(rectangle.X + 10, rectangle.Y + 4, rectangle.Width - 20, 22), StringAlignment.Near);
        }

        private static void DrawGradientTitle(Graphics graphics, Rectangle rectangle, string title, Color baseColour)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(rectangle,
                Color.FromArgb(Math.Min(255, baseColour.R + 35), Math.Min(255, baseColour.G + 35), Math.Min(255, baseColour.B + 35)),
                baseColour, LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(brush, rectangle);
            }
            DrawText(graphics, title, 17.0f, FontStyle.Bold, Color.White,
                new RectangleF(rectangle.X + 18, rectangle.Y + 4, rectangle.Width - 36, rectangle.Height - 7), StringAlignment.Near);
        }

        private static void DrawClassicTab(Graphics graphics, Rectangle rectangle, string title, bool selected)
        {
            using (SolidBrush fill = new SolidBrush(selected ? Color.FromArgb(219, 223, 227) : Color.FromArgb(198, 201, 205)))
            using (Pen border = new Pen(Color.FromArgb(81, 86, 92), 1.0f))
            {
                graphics.FillRectangle(fill, rectangle);
                graphics.DrawRectangle(border, rectangle);
            }
            DrawText(graphics, title, 11.0f, selected ? FontStyle.Bold : FontStyle.Regular,
                selected ? Color.FromArgb(35, 40, 46) : Color.FromArgb(119, 123, 128),
                new RectangleF(rectangle.X + 3, rectangle.Y + 9, rectangle.Width - 6, rectangle.Height - 15), StringAlignment.Center);
        }

        private static void DrawCheckbox(Graphics graphics, int x, int y, bool checkedValue, Color accent)
        {
            Rectangle box = new Rectangle(x, y, 18, 18);
            using (SolidBrush fill = new SolidBrush(Color.White))
            using (Pen border = new Pen(Color.FromArgb(74, 79, 85), 1.0f))
            {
                graphics.FillRectangle(fill, box);
                graphics.DrawRectangle(border, box);
            }
            if (checkedValue)
            {
                using (Pen tick = new Pen(accent, 3.0f))
                {
                    graphics.DrawLine(tick, x + 3, y + 10, x + 7, y + 14);
                    graphics.DrawLine(tick, x + 7, y + 14, x + 15, y + 4);
                }
            }
        }

        private static void DrawText(Graphics graphics, string text, float size, FontStyle style, Color colour,
            RectangleF rectangle, StringAlignment alignment)
        {
            using (Font font = new Font("Segoe UI", size, style, GraphicsUnit.Point))
            using (SolidBrush brush = new SolidBrush(colour))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = alignment;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                graphics.DrawString(text, font, brush, rectangle, format);
            }
        }

        private static string Trim(string value, int maximumLength)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maximumLength)
            {
                return value;
            }
            return value.Substring(0, maximumLength - 3) + "...";
        }
    }
}
