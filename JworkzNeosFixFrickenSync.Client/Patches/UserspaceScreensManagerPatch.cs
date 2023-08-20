using HarmonyLib;
using System;
using FrooxEngine;
using System.Runtime.CompilerServices;
using FrooxEngine.UIX;
using JworkzNeosMod.Patches;
using JworkzNeosMod.Client.Models;
using JworkzNeosMod.Events;
using JworkzNeosMod.Client.Services;
namespace JworkzNeosMod.Client.Patches
{
    [HarmonyPatch(typeof(UserspaceScreensManager))]

    public static class UserspaceScreensManagerPatch
    {
        public const string SCREEN_NAME = "Sync";

        private static readonly Uri MAIN_ICON = NeosAssets.Common.Icons.Cloud;

        private static ConditionalWeakTable<Canvas, SyncScreenCanvas> _canvasSyncScreenPair =
            new ConditionalWeakTable<Canvas, SyncScreenCanvas>();

        public static RadiantDashScreen SyncScreen { get; private set; }

        public static SyncScreenCanvas SyncScreenCanvas { get; private set; }

        [HarmonyPostfix]
        [HarmonyPatch("SetupDefaults")]
        public static void SetupDefaultsPostfix(UserspaceScreensManager __instance)
        {
            AddSyncScreen(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnLoading")]
        public static void OnLoadingPostfix(UserspaceScreensManager __instance)
        {
            AddSyncScreen(__instance);
        }

        private static void AddSyncScreen(UserspaceScreensManager userspaceScreenManager)
        {
            if (userspaceScreenManager.World != Userspace.UserspaceWorld) { return; }

            var dash = userspaceScreenManager.Slot.GetComponentInParents<RadiantDash>();

            if (SyncScreen != null)
            {
                SyncScreen.Destroy();
                SyncScreen.Dispose();
                SyncScreenCanvas?.Dispose();
            }

            SyncScreen = dash.AttachScreen("Sync", SyncScreenCanvas.BTN_COLOR, MAIN_ICON);

            var screenCanvas = SyncScreen.ScreenCanvas;

            SyncScreenCanvas = new SyncScreenCanvas(screenCanvas);
            _canvasSyncScreenPair.Add(screenCanvas, SyncScreenCanvas);
            var screenSlot = SyncScreen.Slot;
            screenSlot.OrderOffset = 55;
            screenSlot.PersistentSelf = false;

            SyncScreenCanvas.InitUI();
         

            foreach(var entry in RecordKeeper.Instance.Entries)
            {
                OnUploadTaskUpdate(Userspace.UserspaceWorld, new UploadTaskProgressEventArgs(entry.Record, entry.UploadProgress));
            }

            RecordUploadTaskBasePatch.UploadTaskStart += OnUploadTaskUpdate;
            RecordUploadTaskBasePatch.UploadTaskProgress += OnUploadTaskUpdate;
            RecordUploadTaskBasePatch.UploadTaskFailure += OnUploadTaskUpdate;
            RecordUploadTaskBasePatch.UploadTaskSuccess += OnUploadTaskUpdate;
        }

        private static void OnUploadTaskUpdate(object _, UploadTaskEventArgsBase @event)
        {
            if (SyncScreenCanvas == null || @event.Record.RecordId == "R-Settings") { return; }

            Userspace.UserspaceWorld.RunSynchronously(() =>
            {
                if (SyncScreenCanvas.HasSyncTaskViewModel(@event.Record))
                {
                    SyncScreenCanvas.UpdateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
                else
                {
                    SyncScreenCanvas.CreateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
            }, true);
        }
    }
}
