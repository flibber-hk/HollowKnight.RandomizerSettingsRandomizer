﻿using MenuChanger;
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

        private static MenuHolder _instance = null;
        internal static MenuHolder Instance => _instance ?? (_instance = new MenuHolder());

        public static void OnExitMenu()
        {
            _instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(Instance.ConstructMenu, Instance.HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            JumpButton = new(landingPage, Localize("Randomize Settings"));
            JumpButton.AddHideAndShowEvent(landingPage, SettingsRandoPage);
            SetTopLevelButtonColour();

            button = JumpButton;
            return true;
        }

        private void SetTopLevelButtonColour()
        {
            if (JumpButton != null)
            {
                JumpButton.Text.color = SettingsRandomizer.GS.IsEnabled() ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
        }

        private void ConstructMenu(MenuPage landingPage)
        {
            SettingsRandoPage = new MenuPage(Localize("Randomize Settings"), landingPage);
            SelectButton = new(SettingsRandoPage, "Settings Profile", SettingsRandomizer.FileNames);
            SelectButton.SetValue(SettingsRandomizer.CurrentChoice);

            // Have to subscribe in this order
            SelectButton.ValueChanged += v => SettingsRandomizer.CurrentChoice = v;
            SelectButton.ValueChanged += v => SetTopLevelButtonColour();

            SettingsRandoPage.AddToNavigationControl(SelectButton);

            Localize(SelectButton);
        }
    }
}