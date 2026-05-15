using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public static class GameplayCursor
    {
        public static void SetLocked(bool locked)
        {
            var mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
            bool wantVisible = !locked;
            if (Cursor.lockState == mode && Cursor.visible == wantVisible)
                return;
            Cursor.lockState = mode;
            Cursor.visible = wantVisible;
        }
    }
}
