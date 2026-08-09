using UnityEngine.InputSystem;

// Keys shared by the menus and story screens
public static class MenuInput
{
    // Enter, Space or J (the fire button) confirms and continues
    public static bool Confirm()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame ||
            keyboard.spaceKey.wasPressedThisFrame ||
            keyboard.jKey.wasPressedThisFrame);
    }

    // Enter held down: hold it to skip a whole cutscene or dialogue
    public static bool SkipHeld()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.enterKey.isPressed ||
            keyboard.numpadEnterKey.isPressed);
    }

    public static bool Back()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame;
    }

    public static bool Up()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.wKey.wasPressedThisFrame ||
            keyboard.upArrowKey.wasPressedThisFrame);
    }

    public static bool Down()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.sKey.wasPressedThisFrame ||
            keyboard.downArrowKey.wasPressedThisFrame);
    }
}
