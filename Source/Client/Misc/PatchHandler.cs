using Shared.Misc;
using System;

namespace GameClient.Misc
{
    public static class PatchHandler
    {
        public static bool BypassFlag { get; private set; }

        private static void EnableBypassFlag()
        {
            if (BypassFlag) Printer.Error("Flag was already true! This should not be happening!");
            else BypassFlag = true;
        }

        private static void DisableBypassFlag()
        {
            if (!BypassFlag) Printer.Error("Flag was already false! This should not be happening!");
            else BypassFlag = false;
        }

        public static void ResetBypassFlag() { BypassFlag = false; }

        public static void ExecuteInBypass(Action toDo)
        {
            EnableBypassFlag();

            try { toDo.Invoke(); }
            catch (Exception e) { Printer.Error(e); }

            DisableBypassFlag();
        }
    }
}
