using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RandomizerCore.Extensions;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using Random = System.Random;

namespace SettingsRandomizer
{
    public class SettingsRandomizer : Mod, IGlobalSettings<SettingsRandomizer.GlobalSettings>
    {
        public class GlobalSettings
        {
            public string CurrentChoice = NoSettingsRandomization;
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
        

        public SettingsRandomizer() : base(null)
        {
            instance = this;
        }

        public override string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
        
        public override void Initialize()
        {
            Log("Initializing Mod...");

            MenuHolder.Hook();
            RequestBuilder.OnUpdate.Subscribe(-10000f, RandomizeSettings);
            RandomizerMod.Logging.SettingsLog.AfterLogSettings += AddSettingsToLog;

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

        private static void AddSettingsToLog(RandomizerMod.Logging.LogArguments args, TextWriter tw)
        {
            tw.WriteLine($"Settings Randomization Profile: {CurrentChoice}");
            tw.WriteLine();
        }

        private void RandomizeSettings(RequestBuilder rb)
        {
            ItemChanger.Finder.Serialize("sa.txt", rb.gs);

            if (CurrentChoice == NoSettingsRandomization) return;
            int seed = rb.gs.Seed;
            GenerationSettings orig = rb.gs.Clone() as GenerationSettings;

            rb.gs.Randomize(rb.rng);
            ApplyCustomRandomization(rb.gs, rb.rng);
            rb.gs.Clamp();

            if (CurrentChoice != FullSettingsRandomization)
            {
                OverwriteRandomizedSettings(rb.gs, orig);
                rb.gs.Clamp();
            }

            rb.gs.Seed = seed;

            ItemChanger.Finder.Serialize("settings.txt", rb.gs);
        }

        private static void OverwriteRandomizedSettings(GenerationSettings randomized, GenerationSettings orig)
        {
            string[] config = File.ReadAllLines(Path.Combine(ModDirectory, CurrentChoice + ".txt"));

            bool including = false;

            for (int i = 0; i < config.Length; i++)
            {
                string configItem = config[i];
                if (configItem.Contains("#"))
                {
                    configItem = configItem.Substring(0, configItem.IndexOf("#"));
                }
                if (configItem.Contains("//"))
                {
                    configItem = configItem.Substring(0, configItem.IndexOf("//"));
                }
                configItem = configItem.TrimEnd();

                if (string.IsNullOrEmpty(configItem))
                {
                    continue;
                }

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

                if (including)
                {
                    orig.CopyTo(randomized);
                }
            }
        }

        private void ApplyCustomRandomization(GenerationSettings gs, Random rng)
        {
            // Start item settings - start geo U(0, 50_000) is probably bad
            int geo = rng.NextBool() ? 0 : (int)Math.Floor(rng.PowerLaw(4, 0, 5000));
            gs.StartItemSettings.MinimumStartGeo = gs.StartItemSettings.MaximumStartGeo = geo;

            // Cost randomization settings - custom clamp function so min==max doesn't have 50% chance
            CostSettings cs = gs.CostSettings;
            cs.Randomize(rng);

            Swap(ref cs.MinimumCharmCost, ref cs.MaximumCharmCost);
            Swap(ref cs.MinimumEggCost, ref cs.MaximumEggCost);
            Swap(ref cs.MinimumEssenceCost, ref cs.MaximumEssenceCost);
            Swap(ref cs.MinimumGrubCost, ref cs.MaximumGrubCost);
            
            // Progression depth settings - randomize float fields according to power law is probably best
            ProgressionDepthSettings ps = gs.ProgressionDepthSettings;
            foreach (FieldInfo fi in Util.GetOrderedFields(typeof(ProgressionDepthSettings)))
            {
                if (fi.FieldType == typeof(float))
                {
                    fi.SetValue(ps, Convert.ToSingle(rng.PowerLaw(2, 0, 10)));
                }
            }
        }

        /// <summary>
        /// If min > max, swap them
        /// </summary>
        private static void Swap<T>(ref T min, ref T max) where T : IComparable<T>
        {
            if (min.CompareTo(max) > 0)
            {
                (min, max) = (max, min);
            }
        }
    }
}