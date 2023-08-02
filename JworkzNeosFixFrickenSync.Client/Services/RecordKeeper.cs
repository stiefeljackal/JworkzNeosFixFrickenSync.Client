using FrooxEngine;
using JworkzNeosMod.Client.Models;
using JworkzNeosMod.Client.Patches;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace JworkzNeosMod.Client.Services
{
    public class RecordKeeper
    {
        public static readonly RecordKeeper Instance = new RecordKeeper();

        private ConcurrentDictionary<string, RecordKeeperEntry> _recordKeeperEntries = new ConcurrentDictionary<string, RecordKeeperEntry>();

        public IEnumerable<RecordKeeperEntry> Entries => _recordKeeperEntries.Values;

        public void AddRecord(Record record)
        {
            var recordKeeperEntry = new RecordKeeperEntry(record);
            var recordId = record.RecordId;
            if (HasRecord(record)) {  return; }
            
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
    }
}
