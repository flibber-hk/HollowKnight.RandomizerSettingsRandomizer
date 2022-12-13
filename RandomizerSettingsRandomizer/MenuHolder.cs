using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.Extensions;
using RandomizerMod.Menu;
using static RandomizerMod.Localization;

namespace SettingsRandomizer
{
    public class MenuHolder
    {
        internal MenuPage SettingsRandoPage;

        internal SmallButton JumpButton;
        internal MenuItem<string> SelectButton;
        internal static MenuHolder Instance { get; private set; }

        public static void OnExitMenu()
        {
            Instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.JumpButton;
            return true;
        }

        private void SetButtonColours()
        {
            if (JumpButton != null)
            {
                JumpButton.Text.color = SettingsRandomizer.GS.IsEnabled() ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
            if (SelectButton != null)
            {
                SelectButton.Text.color = SettingsRandomizer.GS.IsEnabled() ? Colors.TRUE_COLOR : Colors.FALSE_COLOR;
            }
        }

        private static void ConstructMenu(MenuPage landingPage) => Instance = new(landingPage);

        private MenuHolder(MenuPage landingPage)
        {
            SettingsRandoPage = new MenuPage(Localize("Randomize Settings"), landingPage);
            SelectButton = new(SettingsRandoPage, "Settings Profile", SettingsRandomizer.FileNames);
            SelectButton.SetValue(SettingsRandomizer.CurrentChoice);

            // Have to subscribe in this order
            SelectButton.ValueChanged += v => SettingsRandomizer.CurrentChoice = v;
            SelectButton.ValueChanged += v => SetButtonColours();

            SettingsRandoPage.AddToNavigationControl(SelectButton);

            Localize(SelectButton);

            JumpButton = new(landingPage, Localize("Randomize Settings"));
            JumpButton.AddHideAndShowEvent(landingPage, SettingsRandoPage);
            SetButtonColours();
        }
    }
}