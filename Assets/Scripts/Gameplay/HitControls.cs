using System;
using UnityEngine.InputSystem;

namespace RhythmRehab
{
    // One shared mapping for gameplay, hold notes and the rhythm preview.
    public static class HitControls
    {
        public static bool IsAssignable(int value) => value != (int)Key.None &&
            value != (int)Key.Escape && Enum.IsDefined(typeof(Key), value);
        public static bool Held(LocalSettings settings) => Down(settings.hitKeyPrimary, false) || Down(settings.hitKeySecondary, false);
        public static bool Pressed(LocalSettings settings) => Down(settings.hitKeyPrimary, true) || Down(settings.hitKeySecondary, true);
        private static bool Down(int key, bool pressed)
        {
            if (key == 0 || Keyboard.current == null || !Enum.IsDefined(typeof(Key), key)) return false;
            var control = Keyboard.current[(Key)key];
            return control != null && (pressed ? control.wasPressedThisFrame : control.isPressed);
        }
        public static string Label(int key) => key == 0 ? "NONE" : ((Key)key).ToString().ToUpperInvariant();
        public static string Label(LocalSettings settings) => Label(settings.hitKeyPrimary) +
            (settings.hitKeySecondary == 0 ? "" : " / " + Label(settings.hitKeySecondary));
    }
}
