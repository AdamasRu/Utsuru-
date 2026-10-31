using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Utsuru.Capture
{
    /// <summary>
    /// Pembungkus input kamera. Otomatis memakai Input System baru jika aktif,
    /// atau Input Manager lama jika tidak.
    /// C = masuk/keluar mode OpenCamera, Klik Kiri Mouse = shutter (hanya di mode OpenCamera).
    /// </summary>
    internal static class CameraInput
    {
        public static bool ToggleCameraPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.cKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        public static bool ShutterPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
