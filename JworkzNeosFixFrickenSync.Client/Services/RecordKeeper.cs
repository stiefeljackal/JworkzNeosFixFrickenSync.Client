using FrooxEngine;
using JworkzNeosMod.Client.Models;
using JworkzNeosMod.Client.Patches;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client.Services
{
    public class RecordKeeper
    {
        public static readonly RecordKeeper Instance = new RecordKeeper();

        private ConcurrentDictionary<string, RecordKeeperEntry> _recordKeeperEntries = new ConcurrentDictionary<string, RecordKeeperEntry>();

        public IEnumerable<RecordKeeperEntry> Entries => _recordKeeperEntries.Values;

        public int CompletedSyncs { get; private set; }

        public int CurrentFailedSyncs => _recordKeeperEntries.Values.Count(r => r.IsSuccessfulSync.HasValue && !r.IsSuccessfulSync.Value);

        public int CurrentSuccessfulSyncs => _recordKeeperEntries.Values.Count(r => r.IsSuccessfulSync.HasValue && r.IsSuccessfulSync.Value);

        public event EventHandler<RecordKeeperEntry> EntryMarkedCompleted;

        public void AddRecord(Record record)
        {
            if (HasRecord(record)) {  return; }

            var recordKeeperEntry = new RecordKeeperEntry(record);
            var recordId = record.RecordId;
            
            _recordKeeperEntries.TryAdd(recordId, recordKeeperEntry);
        }

        public bool HasRecord(Record record) => HasRecord(record.RecordId);

        public bool HasRecord(string recordId) => _recordKeeperEntries.ContainsKey(recordId);

        public RecordKeeperEntry GetRecordEntry(Record record) => GetRecordEntry(record.RecordId);

        public RecordKeeperEntry GetRecordEntry(string recordId)
        {
            _ = _recordKeeperEntries.TryGetValue(recordId, out var recordKeeperEntry);
            return recordKeeperEntry;
        }

        public Record RemoveRecord(Record record) => RemoveRecord(record.RecordId);

        public Record RemoveRecord(string recordId)
        {
            if (!HasRecord(recordId)) { return null; }

            _recordKeeperEntries.TryRemove(recordId, out var recordKeeperEntry);
        
            return recordKeeperEntry.Record;
        }

        public void MarkRecordComplete(Record record, UploadProgressState state, bool isSuccessful = true)
        {
            var recordEntry = GetRecordEntry(record);
            recordEntry.MarkComplete(state, isSuccessful);
            CompletedSyncs++;

            OnEntryMarkedCompleted(recordEntry);
        }

        private void OnEntryMarkedCompleted(RecordKeeperEntry entry)
        {
            EntryMarkedCompleted?.Invoke(this, entry);
        }
    }
}
