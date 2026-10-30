using Funkin.Flixel;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>Port of funkin.ui.mainmenu.MainMenuState.</summary>
public sealed class MainMenuState : MenuListState
{
    public override void Create()
    {
        FlxG.BackgroundColor = new Color(28, 28, 40);
        MenuTitle = "MAIN MENU";

        AddItem("STORY MODE", () => FlxG.SwitchState(new StoryMenuState()), "Play through the weeks in order.");
        AddItem("FREEPLAY", () => FlxG.SwitchState(new FreeplayState()), "Play any unlocked song.");
        AddItem("OPTIONS", () => FlxG.SwitchState(new OptionsState()), "Adjust gameplay and audio settings.");
        AddItem("CREDITS", () => FlxG.SwitchState(new CreditsState()), "See who made this port possible.");
        AddItem("EXIT", () => { Save.SaveManager.Instance.Flush(true); System.Environment.Exit(0); }, "Quit the game.");

        BuildMenuTexts();
    }

    protected override void OnBack()
    {
        Save.SaveManager.Instance.Flush();
        FlxG.SwitchState(new TitleState());
    }
}
