using FrooxEngine;
using JworkzNeosMod.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JworkzNeosMod.Models
{
    public class RecordKeeperEntryEventArgs
    {
        public Record Record { get; private set; }

        public bool IsSuccessfulSync { get; private set; }

        public bool IsFailureSync { get; private set; }

        public bool IsProgressSync { get; private set; }

        public ushort PreviousFailedAttempts { get; private set; }

        public RecordKeeperEntryEventArgs(RecordKeeperEntry entry)
        {
            Record = entry.Record;
            IsSuccessfulSync = entry.IsSuccessfulSync.HasValue && entry.IsSuccessfulSync.Value;
            IsFailureSync = entry.IsSuccessfulSync.HasValue && !entry.IsSuccessfulSync.Value;
            IsProgressSync = !IsSuccessfulSync && !IsFailureSync;
            PreviousFailedAttempts = entry.PreviousFailedAttempts;
        }

    }
}
