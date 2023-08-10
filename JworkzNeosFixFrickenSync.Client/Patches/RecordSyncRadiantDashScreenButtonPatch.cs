using BaseX;
using FrooxEngine;
using HarmonyLib;
using JworkzNeosMod.Client.Models;
using JworkzNeosMod.Client.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RadiantDash))]
    public static class RecordSyncRadiantDashScreenButtonPatch
    {
        private const float BTN_ERROR_FADE_SPEED = 0.6f;

        private const float BTN_ERROR_START_POSITION = 0.5f;

        private static readonly SyncRef<RadiantDashScreen> SyncScreen = new SyncRef<RadiantDashScreen>();

        [HarmonyPostfix]
        [HarmonyPatch("BuildScreenButton")]
        private static void BuildScreenButtonPostFix(RadiantDashScreen screen, RadiantDashButton __result)
        {
            if (!SyncScreen.IsInitialized)
            {
                SyncScreen.Initialize(Userspace.Current.World, __result.Dash.RawTarget);
            }
            
            if (screen != UserspaceScreensManagerPatch.SyncScreen) { return; }

            SyncScreen.Value = screen.ReferenceID;
            var syncScreenBtn = __result;
            var syncScreenBtnColorDriver = syncScreenBtn.Button.ColorDrivers.FirstOrDefault();

            if (syncScreenBtnColorDriver == null) { return; }

            var dash = __result.Dash.RawTarget;
            var syncScreenBtnPanner1D = syncScreenBtn.Slot.AttachComponent<Panner1D>();
            var syncScreenBtnValGradientColorDriver = syncScreenBtn.Slot.AttachComponent<ValueGradientDriver<color>>();
            var syncScreenBtnErrorBoolSwitchDriver = syncScreenBtn.Slot.AttachComponent<BooleanValueDriver<color>>();
            var syncScreenRefEqualityDriver = syncScreenBtn.Slot.AttachComponent<ReferenceEqualityDriver<RadiantDashScreen>>();
            var syncScreenBtnActiveBoolSwitchDriver = syncScreenBtn.Slot.AttachComponent<BooleanValueDriver<color>>();

            syncScreenBtnPanner1D.PingPong.Value = true;
            syncScreenBtnPanner1D.Speed = BTN_ERROR_FADE_SPEED;
            syncScreenBtnPanner1D.Target = syncScreenBtnValGradientColorDriver.Progress;
            syncScreenBtnPanner1D.Offset = 0;

            syncScreenBtnValGradientColorDriver.Target.Value = syncScreenBtnErrorBoolSwitchDriver.TrueValue.ReferenceID;
            syncScreenBtnValGradientColorDriver.AddPoint(BTN_ERROR_START_POSITION, RadiantDashButton.DEFAULT_COLOR);
            syncScreenBtnValGradientColorDriver.AddPoint(1f, color.Red);

            syncScreenBtnErrorBoolSwitchDriver.FalseValue.Value = RadiantDashButton.DEFAULT_COLOR;
            syncScreenBtnErrorBoolSwitchDriver.TargetField.Value = syncScreenBtnActiveBoolSwitchDriver.FalseValue.ReferenceID;
            syncScreenBtnErrorBoolSwitchDriver.State.DriveFrom(UserspaceScreensManagerPatch.SyncScreenCanvas.HasFailedSyncs);

            syncScreenRefEqualityDriver.Reference.DriveFrom(dash.CurrentScreen);
            syncScreenRefEqualityDriver.TargetReference.Value = SyncScreen.ReferenceID;
            syncScreenRefEqualityDriver.Target.Value = syncScreenBtnActiveBoolSwitchDriver.State.ReferenceID;


            syncScreenBtnActiveBoolSwitchDriver.TrueValue.Value = screen.ActiveColor.Value;
            syncScreenBtnActiveBoolSwitchDriver.TargetField.Value = syncScreenBtnColorDriver.NormalColor.ReferenceID;
        }
    }
}
