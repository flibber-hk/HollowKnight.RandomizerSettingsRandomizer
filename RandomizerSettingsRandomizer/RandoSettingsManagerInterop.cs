using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;
using System.IO;

namespace SettingsRandomizer
{
    public class RandoSettingsManagerData
    {
        public string ProfileName;
        public string[] ConfigText;

        public static RandoSettingsManagerData CreateNull()
        {
            return new() { ProfileName = SettingsRandomizer.NoSettingsRandomization };
        }

        public static RandoSettingsManagerData Create()
        {
            RandoSettingsManagerData data = new();
            data.ProfileName = SettingsRandomizer.GS.CurrentChoice;
            if (SettingsRandomizer.GS.IsFromFile())
            {
                data.ConfigText = File.ReadAllLines(Path.Combine(SettingsRandomizer.ModDirectory, data.ProfileName + ".txt"));
            }

            return data;
        }

        public void Apply()
        {
            if (SettingsRandomizer.FileNames.Contains(ProfileName))
            {
                SettingsRandomizer.FileNames.Add(ProfileName);
                MenuHolder.Instance.SelectButton.AddItem(ProfileName);
            }

            if (ConfigText is not null)
            {
                string fileName = Path.Combine(SettingsRandomizer.ModDirectory, ProfileName + ".txt");
                File.WriteAllLines(fileName, ConfigText);
            }
            
            SettingsRandomizer.GS.CurrentChoice = ProfileName;
            MenuHolder.Instance.SelectButton.SetValue(ProfileName);
        }
    }

    internal static class RandoSettingsManagerInterop
    {
        public static void Hook()
        {
            RandoSettingsManagerMod.Instance.RegisterConnection(new RandoPlusSettingsProxy());
        }
    }

    internal class RandoPlusSettingsProxy : RandoSettingsProxy<RandoSettingsManagerData, string>
    {
        public override string ModKey => SettingsRandomizer.instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; }
            = new EqualityVersioningPolicy<string>(SettingsRandomizer.instance.GetVersion());

        public override void ReceiveSettings(RandoSettingsManagerData settings)
        {
            settings ??= RandoSettingsManagerData.CreateNull();
            settings.Apply();
        }

        public override bool TryProvideSettings(out RandoSettingsManagerData settings)
        {
            settings = RandoSettingsManagerData.Create();
            return true;
        }
    }
}