using FrooxEngine;
using System;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client.Models
{
    public class RecordKeeperEntry
    {
        public Record Record { get; }

        public string RecordId => Record.RecordId;

        public DateTimeOffset? SyncCompletedDate { get; private set; } = null;

        public bool? IsSuccessfulSync { get; private set; } = null;

        public UploadProgressState UploadProgress { get; private set; }

        public RecordKeeperEntry(Record record, UploadProgressState? state = null)
        {
            Record = record;

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
        }

        public void MarkComplete(UploadProgressState state, bool isSuccessful = true)
        {
            UploadProgress = new UploadProgressState(state.Stage, isSuccessful, 1f);
            SyncCompletedDate = DateTimeOffset.Now;
            IsSuccessfulSync = isSuccessful;
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
            UploadProgress = new UploadProgressState(state.Stage, state.IsSuccessful, state.Progress);
        }

        public override int GetHashCode() => RecordId.GetHashCode();
    }
}
