using System;
using System.Net.Http;
using System.Windows.Forms;
using tarkov_settings.Setting;
using tarkov_settings.GPU;

namespace tarkov_settings
{
    public partial class MainForm : Form
    {
        private ProcessMonitor pMonitor = ProcessMonitor.Instance;
        private IGPU gpu = GPUDevice.Instance;
        private AppSetting appSetting;

        private bool minimizeOnStart = false;
        private string activeProcessTarget = AppSetting.EFT_PROCESS;
        private bool isLoadingColorProfile = false;

        public MainForm()
        {
            InitializeComponent();

            #region Load App Settings
            // Load Settings
            appSetting = AppSetting.Load();
            appSetting.EnsureDefaults();

            SelectProcessProfile(AppSetting.EFT_PROCESS, false);
            minimizeOnStart = appSetting.minimizeOnStart;
            this.minimizeStartCheckBox.Checked = minimizeOnStart;
            #endregion
            
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = String.Format("Tarkov Settings {0}", version);
            _ = new UpdateNotifier(version);

            // Saturation Initialize
            if (gpu.Vendor != GPUVendor.NVIDIA)
                DVLGroupBox.Enabled = false;

            #region Initialize Display
            // Initialize Display Dropdown
            foreach (string display in Display.displays)
            {
                DisplayCombo.Items.Add(display);
            }
            
            if(DisplayCombo.FindString(appSetting.display) != -1)
                DisplayCombo.SelectedIndex = DisplayCombo.FindString(appSetting.display);

            Display.Primary = (string)DisplayCombo.SelectedItem;
            #endregion

            // Initialize Process Monitor
            pMonitor.Parent = this;
            foreach (string pTarget in appSetting.pTargets)
            {
                pMonitor.Add(pTarget.ToLower());
            }
            pMonitor.Init();
        }

        #region BCGS Getter/Setter
        public double Brightness
        {
            get => BrightnessBar.Value / 100.0;
            set => BrightnessBar.Value = (int)(value * 100);
        }

        public double Contrast
        {
            get => ContrastBar.Value / 100.0;
            set => ContrastBar.Value = (int)(value * 100);
        }

        public double Gamma
        {
            get => GammaBar.Value / 100.0;
            set => GammaBar.Value = (int)(value * 100);
        }

        public int DVL
        {
            get => DVLBar.Value;
            set => DVLBar.Value = value;
        }

        public (double, double, double, int) GetColorValue()
        {
            SyncActiveColorProfile();
            return (
                BrightnessBar.Value / 100.0,
                ContrastBar.Value / 100.0,
                GammaBar.Value / 100.0,
                DVLBar.Value
                );
        }

        public (double, double, double, int) GetColorValue(string processTarget)
        {
            SyncActiveColorProfile();
            ColorProfile profile = appSetting.GetColorProfile(processTarget);
            return (
                profile.brightness,
                profile.contrast,
                profile.gamma,
                profile.saturation
                );
        }
        #endregion

        public bool IsEnabled { get=> this.enableToolStripMenuItem.Checked;}

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (minimizeOnStart)
            {
                this.Visible = false;
                this.ShowInTaskbar = false;
                this.trayIcon.ShowBalloonTip(
                    2500,
                    "Tarkov Settings Initailized!",
                    "Check out tray to modify your color setting",
                    ToolTipIcon.Info
                    );
            }
        }

        #region Control Event Handlers
        private void ColorLabel_DClick(object sender, EventArgs e)
        {
            var label = sender as Label;
            
            if (label.Equals(BrightnessLabel))
            {
                BrightnessBar.Value = 50;
            }
            else if (label.Equals(ContrastLabel))
            {
                ContrastBar.Value = 50;
            }
            else if (label.Equals(GammaLabel))
            {
                GammaBar.Value = 100;
            }
            else if (label.Equals(DVLLabel))
            {
                DVLBar.Value = 0;
            }
        }
        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            var trackBar = sender as TrackBar;

            if (trackBar.Equals(BrightnessBar))
            {
                BrightnessText.Text = (BrightnessBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(ContrastBar))
            {
                ContrastText.Text = (ContrastBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(GammaBar))
            {
                GammaText.Text = (GammaBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(DVLBar))
            {
                DVLText.Text = DVLBar.Value.ToString();
            }

            SyncActiveColorProfile();
        }
        private void DisplayCombo_SelectedValueChanged(object sender, EventArgs e)
        {
            string selectedDisplay = (string)DisplayCombo.SelectedItem;
            Display.Primary = selectedDisplay;

            if(Display.Primary != selectedDisplay)
            {
                DisplayCombo.SelectedIndex = DisplayCombo.FindString(Display.Primary);
            }
        }
        #endregion

        private void ShowForm(object sender, EventArgs e)
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
        }

        private void ExitFormClicked(object sender, EventArgs e)
        {
            SaveSettings();
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                Console.WriteLine(e.CloseReason);
                this.trayIcon.Dispose();
                Console.WriteLine("[mainForm] Closing pMonitor");
                pMonitor.Close();
            }
        }

        private void CheckOnMinimizeToTray(object sender, EventArgs e)
        {
            this.minimizeOnStart = this.minimizeStartCheckBox.Checked;
            SaveSettings();
        }

        private void EFTProfileButton_Click(object sender, EventArgs e)
        {
            SelectProcessProfile(AppSetting.EFT_PROCESS);
        }

        private void ArenaProfileButton_Click(object sender, EventArgs e)
        {
            SelectProcessProfile(AppSetting.ARENA_PROCESS);
        }

        private void SelectProcessProfile(string processTarget, bool saveCurrentProfile = true)
        {
            if (appSetting == null)
                return;

            if (saveCurrentProfile)
                SyncActiveColorProfile();

            activeProcessTarget = processTarget;
            ColorProfile profile = appSetting.GetColorProfile(activeProcessTarget);

            isLoadingColorProfile = true;
            try
            {
                Brightness = profile.brightness;
                Contrast = profile.contrast;
                Gamma = profile.gamma;
                DVL = profile.saturation;
            }
            finally
            {
                isLoadingColorProfile = false;
            }

            UpdateProfileButtons();
        }

        private void SyncActiveColorProfile()
        {
            if (appSetting == null || isLoadingColorProfile)
                return;

            appSetting.SetColorProfile(activeProcessTarget, Brightness, Contrast, Gamma, DVL);
        }

        private void SaveSettings()
        {
            if (appSetting == null)
                return;

            SyncActiveColorProfile();

            ColorProfile eftProfile = appSetting.GetColorProfile(AppSetting.EFT_PROCESS);
            appSetting.brightness = eftProfile.brightness;
            appSetting.contrast = eftProfile.contrast;
            appSetting.gamma = eftProfile.gamma;
            appSetting.saturation = eftProfile.saturation;

            if (DisplayCombo.SelectedItem != null)
                appSetting.display = (string)DisplayCombo.SelectedItem;

            appSetting.minimizeOnStart = minimizeOnStart;
            appSetting.Save();
        }

        private void UpdateProfileButtons()
        {
            bool isEftProfile = activeProcessTarget == AppSetting.EFT_PROCESS;
            MiscsButton.Checked = isEftProfile;
            ColorButton.Checked = !isEftProfile;
            colorGroupBox.Text = isEftProfile ? "EFT Color" : "EFT: Arena Color";
        }
    }
}
