using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RhythmRehab
{
    // One shared mapping for gameplay, hold notes and the rhythm preview.
    public static class HitControls
    {
        public static bool IsAssignable(int value) => value != (int)Key.None &&
            value != (int)Key.Escape && Enum.IsDefined(typeof(Key), value);
        public static bool Held(LocalSettings settings) => MouseDown(false) ||
            Down(settings.hitKeyPrimary, false) || Down(settings.hitKeySecondary, false);
        public static bool Pressed(LocalSettings settings) => MouseDown(true) ||
            Down(settings.hitKeyPrimary, true) || Down(settings.hitKeySecondary, true);

        private static bool MouseDown(bool pressed)
        {
            var mouse = Mouse.current;
            return mouse != null && (Down(mouse.leftButton, pressed) || Down(mouse.rightButton, pressed));
        }

        private static bool Down(int key, bool pressed)
        {
            if (key == 0 || Keyboard.current == null || !Enum.IsDefined(typeof(Key), key)) return false;
            return Down(Keyboard.current[(Key)key], pressed);
        }
        private static bool Down(ButtonControl control, bool pressed) =>
            control != null && (pressed ? control.wasPressedThisFrame : control.isPressed);

        public static string Label(int key) => key == 0 ? "NONE" : ((Key)key).ToString().ToUpperInvariant();
        public static string Label(LocalSettings settings) => "L/R CLICK / " + Label(settings.hitKeyPrimary) +
            (settings.hitKeySecondary == 0 ? "" : " / " + Label(settings.hitKeySecondary));
    }
}
