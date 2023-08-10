using BaseX;
using FrooxEngine;
using System;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client.Models
{
    internal class SyncTaskViewModel : IDisposable
    {
        public Sync<color> StatusColor { get; } = new Sync<color>();
        
        public Sync<string> TaskTitle { get; } = new Sync<string>();

        public Sync<string> TaskInventoryPath { get; } = new Sync<string>();

        public Sync<string> TaskStage { get; } = new Sync<string>();

        public Sync<string> RecordName { get; } = new Sync<string>();

        public Sync<string> RecordId { get; } = new Sync<string>();

        public Sync<Uri> ThumbnailUri { get; } = new Sync<Uri>();

        public Sync<bool> IsTaskSuccessful { get; } = new Sync<bool>();

        public Sync<bool> IsTaskFailure { get ; } = new Sync<bool>();

        public Sync<bool> IsTaskCanceled { get; } = new Sync<bool>();

        public Sync<bool> IsTaskConflicting { get; } = new Sync<bool>();

        public Sync<bool> CanTaskRetry { get; } = new Sync<bool>();

        public Sync<float> Progress { get; } = new Sync<float>();

        public SyncTaskType TaskType { get; private set; }

        public Sync<color> TransparentStatusColor { get; } = new Sync<color>();

        public SyncTaskViewModel(IWorldElement we)
        {
            StatusColor.Initialize(we.World, we);
            RecordId.Initialize(we.World, we);
            TaskTitle.Initialize(we.World, we);
            TaskInventoryPath.Initialize(we.World, we);
            RecordName.Initialize(we.World, we);
            ThumbnailUri.Initialize(we.World, we);
            TaskStage.Initialize(we.World, we);
            IsTaskSuccessful.Initialize(we.World, we);
            IsTaskFailure.Initialize(we.World, we);
            IsTaskCanceled.Initialize(we.World, we);
            IsTaskConflicting.Initialize(we.World, we);
            CanTaskRetry.Initialize(we.World, we);
            Progress.Initialize(we.World, we);
        }

        public void UpdateInfo(Record record, color colorStatus, UploadProgressState state)
        {
            var thumbnailUri = record.ThumbnailURI;

            StatusColor.Value = colorStatus;
            RecordId.Value = record.RecordId;
            TaskTitle.Value = $"{record.Name} <size 55%>({record.RecordId})</size>";
            TaskInventoryPath.Value = $"<i>{record.OwnerId} > {(string.IsNullOrEmpty(record.Path) ? $"[{record.RecordType}]" : record.Path)}</i>";
            RecordName.Value = record.Name;
            ThumbnailUri.Value = string.IsNullOrEmpty(thumbnailUri) ? null : new Uri(record.ThumbnailURI);
            TaskStage.Value = state.Stage;
            
            var isTaskSuccessFul = state.Indicator == UploadProgressIndicator.Success;
            var isTaskFailure = state.Indicator == UploadProgressIndicator.Failure;
            var isTaskConflicting = state.Stage.Contains("Conflict"); 

            IsTaskSuccessful.Value = isTaskSuccessFul;
            IsTaskFailure.Value = isTaskFailure;
            IsTaskCanceled.Value = state.Indicator == UploadProgressIndicator.Canceled;
            IsTaskConflicting.Value = isTaskFailure && isTaskConflicting;
            CanTaskRetry.Value = isTaskFailure && !isTaskConflicting;
            Progress.Value = state.Progress;

            switch (record.RecordType)
            {
                case "directory":
                case "link":
                    TaskType = SyncTaskType.Folder;
                    break;
                case "audio":
                    TaskType = SyncTaskType.Audio;
                    break;
                default:
                    TaskType = SyncTaskType.Object;
                    break;
            }
        }

        public void Dispose()
        {
            StatusColor.Dispose();
            RecordId.Dispose();
            TaskTitle.Dispose();
            TaskInventoryPath.Dispose();
            TaskStage.Dispose();
            RecordName.Dispose();
            ThumbnailUri.Dispose();
            IsTaskSuccessful.Dispose();
            IsTaskFailure.Dispose();
            IsTaskCanceled.Dispose();
            IsTaskConflicting.Dispose();
            CanTaskRetry.Dispose();
            Progress.Dispose();
        }
    }
}
