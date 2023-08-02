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

        public Sync<Uri> ThumbnailUri { get; } = new Sync<Uri>();

        public Sync<float> Progress { get; } = new Sync<float>();

        public bool IsFolder { get; private set; }


        public Sync<color> TransparentStatusColor { get; } = new Sync<color>();

        public SyncTaskViewModel(IWorldElement we)
        {
            StatusColor.Initialize(we.World, we);
            TaskTitle.Initialize(we.World, we);
            TaskInventoryPath.Initialize(we.World, we);
            TaskStage.Initialize(we.World, we);
            RecordName.Initialize(we.World, we);
            ThumbnailUri.Initialize(we.World, we);
            Progress.Initialize(we.World, we);
        }

        public void UpdateInfo(Record record, color colorStatus, UploadProgressState state)
        {
            var thumbnailUri = record.ThumbnailURI;

            StatusColor.Value = colorStatus;
            TaskTitle.Value = $"{record.Name} <size 55%>({record.RecordId})</size>";
            TaskInventoryPath.Value = $"<i>{record.OwnerId} > {(string.IsNullOrEmpty(record.Path) ? $"[{record.RecordType}]" : record.Path)}</i>";
            RecordName.Value = record.Name;
            ThumbnailUri.Value = string.IsNullOrEmpty(thumbnailUri) ? null : new Uri(record.ThumbnailURI);
            TaskStage.Value = state.Stage;
            Progress.Value = state.Progress;
            IsFolder = record.RecordType == "directory" || record.RecordType == "link";
        }

        public void Dispose()
        {
            StatusColor.Dispose();
            TaskTitle.Dispose();
            TaskInventoryPath.Dispose();
            TaskStage.Dispose();
            RecordName.Dispose();
            ThumbnailUri.Dispose();
            Progress.Dispose();
        }
    }
}
