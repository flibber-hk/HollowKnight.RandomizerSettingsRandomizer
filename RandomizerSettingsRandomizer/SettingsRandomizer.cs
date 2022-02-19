using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using UnityEngine;

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

            if (CurrentChoice != FullSettingsRandomization)
            {
                OverwriteRandomizedSettings(rb.gs, orig);
                rb.gs.Clamp();
            }

            rb.gs.Seed = seed;

            ItemChanger.Finder.Serialize("settings.txt", rb.gs);
        }

        private static void OverwriteRandomizedSettings(GenerationSettings toModify, GenerationSettings orig)
        {
            List<string> config = File.ReadAllLines(Path.Combine(ModDirectory, CurrentChoice + ".txt"))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            bool included = false;
            if (config[0].StartsWith("INCLU"))
            {
                config.RemoveAt(0);
                included = true;
            }
            else if (config[0].StartsWith("EXCLU"))
            {
                config.RemoveAt(0);
            }

            if (included)
            {
                CopyGenerationSettings(toModify, orig, config);
                orig.CopyTo(toModify);
            }
            else
            {
                CopyGenerationSettings(orig, toModify, config);
            }
        }

        /// <summary>
        /// Copy some generation settings fields from one instance to another according to config.
        /// </summary>
        /// <param name="from">The original settings.</param>
        /// <param name="to">The target.</param>
        /// <param name="config">A list of strings: 
        /// "moduleName" to copy the settings module 
        /// "moduleName.fieldName" to copy a single field
        /// ".fieldName" to copy a field from an unspecified module</param>
        private static void CopyGenerationSettings(GenerationSettings from, GenerationSettings to, List<string> config)
        {
            for (int i = 0; i < config.Count; i++)
            {
                string s = config[i];
                if (s.StartsWith("."))
                {
                    s = Util.GetPath(s.Remove(0, 1));
                }
                object value = from.Get(s);
                to.Set(s, value);
            }
        }
    }
}