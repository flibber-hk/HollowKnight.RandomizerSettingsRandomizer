using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Modding;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using RandomizerCore.Extensions;
using RandomizerMod.Logging;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using Random = System.Random;
using static RandomizerMod.Localization;

namespace SettingsRandomizer
{
    public class SettingsRandomizer : Mod, IGlobalSettings<SettingsRandomizer.GlobalSettings>
    {
        public class GlobalSettings
        {
            public string CurrentChoice = NoSettingsRandomization;

            public bool IsEnabled() => CurrentChoice != NoSettingsRandomization;
        }

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings gs) => GS = gs;
        public GlobalSettings OnSaveGlobal() => GS;
        public static string CurrentChoice
        {
            get => GS.CurrentChoice;
            set => GS.CurrentChoice = value;
        }


        public static readonly string ModDirectory = Path.GetDirectoryName(typeof(SettingsRandomizer).Assembly.Location);
        internal static SettingsRandomizer instance;

        public const string NoSettingsRandomization = "Disabled";
        public const string FullSettingsRandomization = "Full";
        public static List<string> FileNames = new() { FullSettingsRandomization, NoSettingsRandomization };

        // Initialize after Randomizer loaded its LocalizationData
        public override int LoadPriority()
        {
            return 10;
        }

        public SettingsRandomizer() : base(null)
        {
            instance = this;
        }

        public override string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }

        private static MethodInfo RandoControllerRun = typeof(RandoController).GetMethod(nameof(RandoController.Run));
        private Hook _hook;

        public override void Initialize()
        {
            Log("Initializing Mod...");

            MenuHolder.Hook();

            // RequestBuilder.OnUpdate.Subscribe(-100_000f, RandomizeSettings);
            RequestBuilder.OnUpdate.Subscribe(-100_000f, AddSettingsExceptionLogger);
            _hook = new Hook(RandoControllerRun, (Action<RandoController> orig, RandoController self) =>
            {
                RandomizeSettings(self.gs, self.rng);
                orig(self);
            });


            SettingsLog.AfterLogSettings += AddSettingsToLog;

            DirectoryInfo main = new(ModDirectory);
            foreach (FileInfo f in main.EnumerateFiles("*.txt"))
            {
                if (!f.Name.EndsWith(".txt"))
                {
                    LogWarn($"Ignoring file {f.Name}");
                    continue;
                }

                string name = f.Name.Substring(0, f.Name.Length - 4);
                if (name == NoSettingsRandomization || name == FullSettingsRandomization)
                {
                    LogWarn($"Ignoring file {f.Name}");
                    continue;
                }

                FileNames.Add(name);
            }
            if (!FileNames.Contains(CurrentChoice))
            {
                CurrentChoice = NoSettingsRandomization;
            }
        }

        private static void AddSettingsToLog(LogArguments args, TextWriter tw)
        {
            string prefix = Localize("Settings Randomization Profile:");
            string setting = Localize(CurrentChoice);
            tw.WriteLine(prefix + " " + setting);
            tw.WriteLine();
        }

        private void RandomizeSettings(RequestBuilder rb)
        {
            RandomizeSettings(rb.gs, rb.rng);
        }

        private void AddSettingsExceptionLogger(RequestBuilder rb)
        {
            if (CurrentChoice == NoSettingsRandomization) return;

            // Log settings if there's an exception, because weird settings are probably the reason
            void dumpSettings(Exception e)
            {
                Log("Settings:\n");
                Log(JsonConvert.SerializeObject(rb.gs, Formatting.Indented));
                rb.rm.OnError -= dumpSettings;
            }
            rb.rm.OnError += dumpSettings;
        }

        private void RandomizeSettings(GenerationSettings gs, Random rng)
        {
            if (CurrentChoice == NoSettingsRandomization) return;
            int seed = gs.Seed;
            GenerationSettings orig = gs.Clone() as GenerationSettings;

            gs.Randomize(rng);
            ApplyCustomRandomization(gs, rng);
            gs.Clamp();

            if (CurrentChoice != FullSettingsRandomization)
            {
                OverwriteRandomizedSettings(gs, orig);
                gs.Clamp();
            }

            gs.Seed = seed;
        }

        private static void OverwriteRandomizedSettings(GenerationSettings randomized, GenerationSettings orig)
        {
            IEnumerable<string> config = File.ReadAllLines(Path.Combine(ModDirectory, CurrentChoice + ".txt"))
                .Select(RemoveComments)
                .Where(x => !string.IsNullOrEmpty(x));

            bool including = false;

            foreach (string s in config)
            {
                string configItem = s;

                if (configItem.StartsWith("INCLU"))
                {
                    including = true;
                }
                else if (configItem.StartsWith("EXCLU"))
                {
                    including = false;
                }
                else
                {
                    if (configItem.StartsWith("."))
                    {
                        configItem = Util.GetPath(configItem.Remove(0, 1));
                    }

                    if (including)
                    {
                        object value = randomized.Get(configItem);
                        orig.Set(configItem, value);
                    }
                    else
                    {
                        object value = orig.Get(configItem);
                        randomized.Set(configItem, value);
                    }
                }
            }

            if (including)
            {
                orig.CopyTo(randomized);
            }
        }

        private static string RemoveComments(string orig)
        {
            return Regex.Replace(orig, @"(#|//).*", "").Trim();
        }

        private void ApplyCustomRandomization(GenerationSettings gs, Random rng)
        {
            // Start item settings - start geo U(0, 50_000) is probably bad
            // The median of the distribution is 64
            int geo = rng.NextBool() ? 0 : (int)Math.Floor(rng.PowerLaw(2, 0, 4096));
            gs.StartItemSettings.MinimumStartGeo = gs.StartItemSettings.MaximumStartGeo = geo;

            // Cost randomization settings - custom clamp function; otherwise there's a 50% chance
            // of Min = Max, which means all costs of that type are equal
            CostSettings cs = gs.CostSettings;
            cs.Randomize(rng);

            if (cs.MinimumCharmCost > cs.MaximumCharmCost)
            {
                (cs.MinimumCharmCost, cs.MaximumCharmCost) = (cs.MaximumCharmCost, cs.MinimumCharmCost);
            }
            if (cs.MinimumEggCost > cs.MaximumEggCost)
            {
                (cs.MinimumEggCost, cs.MaximumEggCost) = (cs.MaximumEggCost, cs.MinimumEggCost);
            }
            if (cs.MinimumEssenceCost > cs.MaximumEssenceCost)
            {
                (cs.MinimumEssenceCost, cs.MaximumEssenceCost) = (cs.MaximumEssenceCost, cs.MinimumEssenceCost);
            }
            if (cs.MinimumGrubCost > cs.MaximumGrubCost)
            {
                (cs.MinimumGrubCost, cs.MaximumGrubCost) = (cs.MaximumGrubCost, cs.MinimumGrubCost);
            }

            // Curse settings - grubs replaced by mimics should be between 0 and 46, say
            gs.CursedSettings.MaximumGrubsReplacedByMimics = rng.Next(0, 46);

            // Progression depth settings - randomize float fields according to power law is probably best;
            // That way it's more likely to be lower than higher
            ProgressionDepthSettings ps = gs.ProgressionDepthSettings;
            foreach (FieldInfo fi in Util.GetOrderedFields(typeof(ProgressionDepthSettings)))
            {
                if (fi.FieldType == typeof(float))
                {
                    fi.SetValue(ps, Convert.ToSingle(rng.PowerLaw(2, 0, 10)));
                }
            }

            // Skills should be randomized if split
            if (gs.NoveltySettings.SplitClaw
                || gs.NoveltySettings.SplitCloak
                || gs.NoveltySettings.SplitSuperdash)
            {
                gs.PoolSettings.Skills = true;
            }

            // Skills, keys, stags and charms should be randomized if start items
            StartItemSettings ss = gs.StartItemSettings;
            if (ss.HorizontalMovement != StartItemSettings.StartHorizontalType.None)
            {
                gs.PoolSettings.Skills = true;
            }
            if (ss.VerticalMovement != StartItemSettings.StartVerticalType.None)
            {
                gs.PoolSettings.Skills = true;
            }
            if (ss.Charms != StartItemSettings.StartCharmType.None)
            {
                gs.PoolSettings.Charms = true;
            }
            if (ss.Stags != StartItemSettings.StartStagType.None)
            {
                gs.PoolSettings.Stags = true;
            }
            if (!gs.PoolSettings.Skills || !gs.PoolSettings.Keys)
            {
                ss.MiscItems = StartItemSettings.StartMiscItems.None;
            }
        }
    }
}