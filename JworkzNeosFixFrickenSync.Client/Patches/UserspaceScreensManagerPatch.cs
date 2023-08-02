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
        private static readonly color BTN_COLOR = new color(0.314f, 0.784f, 0.471f);

        private static readonly Uri MAIN_ICON = NeosAssets.Common.Icons.Cloud;

        private static ConditionalWeakTable<World, Canvas> _worldScreenPair =
            new ConditionalWeakTable<World, Canvas>();

        private static ConditionalWeakTable<Canvas, SyncScreenCanvas> _canvasSyncScreenPair =
            new ConditionalWeakTable<Canvas, SyncScreenCanvas>();

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
            if (userspaceScreenManager.World != Userspace.UserspaceWorld || _worldScreenPair.TryGetValue(Userspace.UserspaceWorld, out _))
            { 
                return;
            }

            var dash = userspaceScreenManager.Slot.GetComponentInParents<RadiantDash>();

            var syncScreen = dash.AttachScreen("Sync", BTN_COLOR, MAIN_ICON);
            var screenCanvas = syncScreen.ScreenCanvas;

            _worldScreenPair.Add(Userspace.UserspaceWorld, screenCanvas);

            var syncScreenCanvas = new SyncScreenCanvas(screenCanvas);
            _canvasSyncScreenPair.Add(screenCanvas, syncScreenCanvas);

            var screenSlot = syncScreen.Slot;
            screenSlot.OrderOffset = 55;
            screenSlot.PersistentSelf = false;

            syncScreenCanvas.InitUI();


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
            var hasCanvas = _worldScreenPair.TryGetValue(Userspace.UserspaceWorld, out var screenCanvas);
            
            if (!hasCanvas) { return; }
            
            var hasScreen = _canvasSyncScreenPair.TryGetValue(screenCanvas, out var syncScreenCanvas);

            if (!hasScreen) { return; } 


            if (@event.Record.RecordId == "R-Settings") { return; }
            Userspace.UserspaceWorld.RunSynchronously(() =>
            {
                if (syncScreenCanvas.HasSyncTaskViewModel(@event.Record))
                {
                    syncScreenCanvas.UpdateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
                else
                {
                    syncScreenCanvas.CreateSyncTaskViewModel(@event.Record, @event.ProgressState);
                }
            }, true);
        }
    }
}
