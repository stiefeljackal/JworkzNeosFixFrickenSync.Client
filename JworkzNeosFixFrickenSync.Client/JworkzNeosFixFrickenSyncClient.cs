﻿using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using JworkzNeosMod.Patches;
using JworkzNeosMod.Client.Services;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client
{
    public class JworkzNeosFixFrickenSyncClient : NeosMod
    {
        public override string Name => nameof(JworkzNeosFixFrickenSyncClient);
        public override string Author => "Stiefel Jackal";
        public override string Version => "1.0.2";
        public override string Link => "https://github.com/stiefeljackal/JworkzNeosFixFrickenSync.Client";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_ENABLE =
            new ModConfigurationKey<bool>("enabled", $"Enables the {nameof(JworkzNeosFixFrickenSyncClient)} mod.", () => true);

        private static ModConfiguration Config;

        public static ModConfiguration.ConfigurationChangedEventHandler OnBaseModConfigurationChanged;

        private Harmony _harmony;

        private bool _isPrevEnabled;

        public static bool IsEnabled => Config?.GetValue(KEY_ENABLE) ?? false;

        static JworkzNeosFixFrickenSyncClient()
        {
            RecordUploadTaskBasePatch.UploadTaskStart += (_, @event) =>
                RecordKeeper.Instance.AddRecord(@event.Record);

            RecordUploadTaskBasePatch.UploadTaskProgress += (_, @event) =>
                RecordKeeper.Instance.AddRecord(@event.Record);

            RecordUploadTaskBasePatch.UploadTaskSuccess += (_, @event) =>
            {
                RecordKeeper.Instance.MarkRecordComplete(@event.Record, @event.ProgressState);
            };

            RecordUploadTaskBasePatch.UploadTaskFailure += (_, @event) =>
            {
                RecordKeeper.Instance.MarkRecordComplete(@event.Record, @event.ProgressState, UploadProgressIndicator.Failure);
            };
        }

        /// <summary>
        /// Defines the metadata for the mod and other mod configurations.
        /// </summary>
        /// <param name="builder">The mod configuration definition builder responsible for building and adding details about this mod.</param>
        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(Version)
                .AutoSave(false);
        }

        /// <summary>
        /// Called when the engine initializes.
        /// </summary>
        public override void OnEngineInit()
        {
            _harmony = new Harmony($"jworkz.sjackal.{Name}");
            Config = GetConfiguration();
            Config.OnThisConfigurationChanged += OnConfigurationChanged;
            JworkzNeosFixFrickenSync.OnBaseModConfigurationChanged += OnConfigurationChanged;
            Engine.Current.OnReady += OnCurrentNeosEngineReady;

            _harmony.PatchAll();
        }

        /// <summary>
        /// Refreshes the current state of the mod.
        /// </summary>
        private void RefreshMod()
        {
            var isEnabled = Config.GetValue(KEY_ENABLE);
            ToggleHarmonyPatchState(JworkzNeosFixFrickenSync.IsEnabled && isEnabled);
        }

        /// <summary>
        /// Toggls the Enabled and Disabled state of the mod depending on the passed state.
        /// </summary>
        /// <param name="isEnabled">true if the mod should be enabled; otherwise, false if the mod should be disabled.</param>
        private void ToggleHarmonyPatchState(bool isEnabled)
        {
            if (isEnabled == _isPrevEnabled) { return; }

            _isPrevEnabled = isEnabled;


            if (!IsEnabled)
            {
                TurnOffMod();
            }
            else
            {
                TurnOnMod();
            }
        }

        /// <summary>
        /// Enables the mod.
        /// </summary>
        private void TurnOnMod()
        {
            _harmony.PatchAll();
        }

        /// <summary>
        /// Disables the mod.
        /// </summary>
        private void TurnOffMod()
        {
            _harmony.UnpatchAll(_harmony.Id);
        }

        /// <summary>
        /// Called when the configuration is changed.
        /// </summary>
        /// <param name="event">The event information that details the configuration change.</param>
        private void OnConfigurationChanged(ConfigurationChangedEvent @event)
        {
            RefreshMod();
            OnBaseModConfigurationChanged(@event);
        }

        /// <summary>
        /// Called when the Neos Engine is ready.
        /// </summary>
        private void OnCurrentNeosEngineReady() {
            RefreshMod();
        }
    }
}
