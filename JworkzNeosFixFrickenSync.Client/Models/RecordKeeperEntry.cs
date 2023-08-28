using FrooxEngine;
using System;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client.Models
{
    public class RecordKeeperEntry
    {
        public Record Record { get; }

        public Record LocalRecord { get; private set; }

        public string RecordId => Record.RecordId;

        public DateTimeOffset? SyncCompletedDate { get; private set; } = null;

        public bool? IsSuccessfulSync { get; private set; } = null;

        public UploadProgressState UploadProgress { get; private set; }

        public ushort PreviousFailedAttempts { get; private set; }

        public RecordKeeperEntry(Record record, UploadProgressState? state = null)
        {
            Record = record;
            var assetUri = record.AssetURI;
            if (!string.IsNullOrEmpty(assetUri) && assetUri.ToLower().StartsWith("local"))
            {
                LocalRecord = record.Clone<Record>();
            }

            if (state.HasValue && !string.IsNullOrEmpty(state?.Stage))
            {
                UploadProgress = state.Value;
            }
            else
            {
                MarkStart();
            }
        }

        public void MarkStart()
        {
            UploadProgress = new UploadProgressState("Starting Sync");
            SyncCompletedDate = null;
            IsSuccessfulSync = null;

            if (LocalRecord != null && Record.AssetURI != LocalRecord.AssetURI)
            {
                Record.AssetURI = LocalRecord.AssetURI;
                Record.NeosDBManifest.Clear();
            }
        }

        public void MarkComplete(UploadProgressState state, UploadProgressIndicator indicator = UploadProgressIndicator.Success)
        {
            UploadProgress = new UploadProgressState(state.Stage, indicator, 1f);
            SyncCompletedDate = DateTimeOffset.Now;
            IsSuccessfulSync = indicator == UploadProgressIndicator.Success;

            if (indicator == UploadProgressIndicator.Failure)
            {
                PreviousFailedAttempts++;
            }
            else if (indicator == UploadProgressIndicator.Success)
            {
                LocalRecord = null;
            }
        }

        public void UpdateUploadProgressState(string stage, bool? isSuccessful = null)
        {
            UpdateUploadProgressState(stage, isSuccessful, UploadProgress.Progress);
        }

        public void UpdateUploadProgressState(float progress, bool? isSuccessful = null)
        {
            UpdateUploadProgressState(UploadProgress.Stage, isSuccessful, progress);
        }

        public void UpdateUploadProgressState(string stage, bool? isSuccessful, float progress)
        {
            UpdateUploadProgressState(stage, isSuccessful, progress);
        }

        public void UpdateUploadProgressState(UploadProgressState state)
        {
            UploadProgress = new UploadProgressState(state.Stage, state.Indicator, state.Progress);
        }

        public override int GetHashCode() => RecordId.GetHashCode();
    }
}
