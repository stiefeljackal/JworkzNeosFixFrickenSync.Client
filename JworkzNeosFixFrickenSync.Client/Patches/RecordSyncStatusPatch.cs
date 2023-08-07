using System.Collections.Generic;
using HarmonyLib;
using FrooxEngine;
using System.Text.RegularExpressions;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RecordSyncStatus))]
    public static class RecordSyncStatusPatch
    {
        private const long OUT_OF_SPACE_THRESHOLD = 10485760;

        private static readonly Regex SYNC_ERROR_MSG_REGEX = new Regex("\\s<size=50%>.+</size>$", RegexOptions.Compiled);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(RecordSyncStatus.SyncString), MethodType.Getter)]
        private static bool SyncStringGetterPatch(RecordSyncStatus __instance, ref string __result)
        {
            var recordManager = __instance.Engine.RecordManager;

            if (recordManager.SyncingRecordsCount == 0)
            {
                if (recordManager.UploadingVariantsCount > 0)
                {
                    __result = __instance.GenerateUploadingVariantsLocalizedString(recordManager.UploadingVariantsCount);
                }
                else if (string.IsNullOrEmpty(recordManager.LastFailReason))
                {
                    __result = __instance.GenerateAllSyncedLocalizedString();
                }
                else
                {
                    var currentUser = __instance.Cloud.CurrentUser;
                    __result = currentUser.QuotaBytes - currentUser.UsedBytes < OUT_OF_SPACE_THRESHOLD
                        ? __instance.GenerateErrorMessageLocalizedString("Indicator.Sync.OutOfSpace")
                        : __instance.GenerateSyncErrorLocalizedString();
                }
            }
            else
            {
                var currentUploadTask = recordManager.CurrentUploadTask;
                var progress = ((currentUploadTask?.Progress ?? 0f) * 100.0f);
                __result = __instance.GenerateSyncProgressLocalizedString(recordManager.SyncingRecordsCount, progress);
            }

            return false;
        }
        private static string GenerateUploadingVariantsLocalizedString(this RecordSyncStatus __instance, int variantsCount)
        {
            return __instance.GetLocalized("Indicator.Sync.UploadingVariants", $"<color={__instance.UploadingAssetVariantsColor.Value.ToHexString(true)}>{{0}}", new Dictionary<string, object>()
            {
                { "variant_count", variantsCount }
            });
        }

        private static string GenerateAllSyncedLocalizedString(this RecordSyncStatus __instance) =>
            __instance.GetLocalized("Indicator.Sync.AllSynced", $"<color={__instance.FullySyncedColor.Value.ToHexString(true)}>{{0}}", (Dictionary<string, object>) null);

        private static string GenerateSyncErrorLocalizedString(this RecordSyncStatus __instance)
        {
            var errorMessage = __instance.GenerateErrorMessageLocalizedString("Indicator.Sync.SyncError");

            return SYNC_ERROR_MSG_REGEX.Replace(errorMessage, string.Empty);
        }

        private static string GenerateErrorMessageLocalizedString(this RecordSyncStatus __instance, string type) =>
            __instance.GetLocalized(type, $"<color={__instance.ErrorColor.Value.ToHexString(true)}><b>{{0}}", (Dictionary<string, object>) null);

        private static string GenerateSyncProgressLocalizedString(this RecordSyncStatus __instance, int recordCount, float progress) =>
            __instance.GetLocalized("Indicator.Sync.SyncingItems", $"<color={__instance.SyncingRecordsColor.Value.ToHexString(true)}>{{0}}", new Dictionary<string, object>()
            {
                { "item_count", recordCount },
                { "item_percent", progress.ToString("F1") }
            });
    }
}
