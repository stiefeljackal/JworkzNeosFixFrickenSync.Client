using HarmonyLib;
using System;
using FrooxEngine;
using BaseX;
using System.Runtime.CompilerServices;
using FrooxEngine.UIX;
using JworkzNeosMod.Patches;
using JworkzNeosMod.Client.Models;
using JworkzNeosMod.Events;
using JworkzNeosMod.Client.Services;
using NeosModLoader;

namespace JworkzNeosMod.Client.Patches
{
    [HarmonyPatch(typeof(UserspaceScreensManager))]

    public static class UserspaceScreensManagerPatch
    {
        private static readonly Uri MAIN_ICON = NeosAssets.Common.Icons.Cloud;

        private static ConditionalWeakTable<Canvas, SyncScreenCanvas> _canvasSyncScreenPair =
            new ConditionalWeakTable<Canvas, SyncScreenCanvas>();

        private static RadiantDashScreen _syncScreen;

        private static SyncScreenCanvas _syncScreenCanvas;

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

            if (_syncScreen != null)
            {
                _syncScreen.Destroy();
                _syncScreen.Dispose();
            }

            _syncScreen = dash.AttachScreen("Sync", SyncScreenCanvas.BTN_COLOR, MAIN_ICON);
            var screenCanvas = _syncScreen.ScreenCanvas;

            _syncScreenCanvas = new SyncScreenCanvas(screenCanvas);
            _canvasSyncScreenPair.Add(screenCanvas, _syncScreenCanvas);
            var screenSlot = _syncScreen.Slot;
            screenSlot.OrderOffset = 55;
            screenSlot.PersistentSelf = false;

            _syncScreenCanvas.InitUI();
         

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
            if (_syncScreenCanvas == null || @event.Record.RecordId == "R-Settings") { return; }

            Userspace.UserspaceWorld.RunSynchronously(() =>
            {
                if (_syncScreenCanvas.HasSyncTaskViewModel(@event.Record))
                {
                    _syncScreenCanvas.UpdateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
                else
                {
                    _syncScreenCanvas.CreateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
            }, true);
        }
    }
}
