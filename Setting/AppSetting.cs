using System.Collections.Generic;

namespace tarkov_settings.Setting
{
    class ColorProfile
    {
        public double brightness = 0.5;
        public double contrast = 0.5;
        public double gamma = 1.0;
        public int saturation = 0;
    }

    class AppSetting : Settings<AppSetting>
    {
        public const string EFT_PROCESS = "EscapeFromTarkov";
        public const string ARENA_PROCESS = "EscapeFromTarkovArena";

        private static readonly string[] DEFAULT_PROCESS_TARGETS = {
            EFT_PROCESS,
            ARENA_PROCESS
        };

        // Legacy fields kept to migrate settings.json files created before per-game profiles.
        public double brightness = 0.5;
        public double contrast = 0.5;
        public double gamma = 1.0;
        public int saturation = 0;
        public Dictionary<string, ColorProfile> colorProfiles;
        public HashSet<string> pTargets = new HashSet<string>(DEFAULT_PROCESS_TARGETS);
        public string display = @"\\.\DISPLAY1";
        public bool minimizeOnStart = false;

        public void EnsureDefaults()
        {
            if (pTargets == null)
                pTargets = new HashSet<string>();

            foreach (string processTarget in DEFAULT_PROCESS_TARGETS)
                pTargets.Add(processTarget);

            if (colorProfiles == null)
            {
                colorProfiles = new Dictionary<string, ColorProfile>();
                colorProfiles[EFT_PROCESS] = new ColorProfile
                {
                    brightness = brightness,
                    contrast = contrast,
                    gamma = gamma,
                    saturation = saturation
                };
            }

            foreach (string processTarget in DEFAULT_PROCESS_TARGETS)
            {
                if (!colorProfiles.ContainsKey(processTarget) || colorProfiles[processTarget] == null)
                    colorProfiles[processTarget] = new ColorProfile();
            }
        }

        public ColorProfile GetColorProfile(string processTarget)
        {
            EnsureDefaults();
            processTarget = NormalizeProcessTarget(processTarget);
            return colorProfiles[processTarget];
        }

        public void SetColorProfile(string processTarget, double brightness, double contrast, double gamma, int saturation)
        {
            ColorProfile profile = GetColorProfile(processTarget);
            profile.brightness = brightness;
            profile.contrast = contrast;
            profile.gamma = gamma;
            profile.saturation = saturation;
        }

        private static string NormalizeProcessTarget(string processTarget)
        {
            foreach (string defaultProcessTarget in DEFAULT_PROCESS_TARGETS)
            {
                if (string.Equals(processTarget, defaultProcessTarget, System.StringComparison.OrdinalIgnoreCase))
                    return defaultProcessTarget;
            }

            return EFT_PROCESS;
        }
    }
}
