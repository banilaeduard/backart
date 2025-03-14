﻿using System.ComponentModel.DataAnnotations.Schema;

namespace RepositoryContract.Tasks
{
    public class TaskEntry
    {
        public int Id { get; set; }

        [ColumnAttribute("Name")]
        public string Name { get; set; }
        public string LocationCode { get; set; }
        public string Details { get; set; }
        public bool IsClosed { get; set; }
        public DateTime Created { get; set; }
        public DateTime TaskDate { get; set; }
        public List<TaskAction> Actions { get; set; }
        public List<ExternalReferenceEntry> ExternalReferenceEntries { get; set; }

        public static TaskEntry From(TaskEntry entry, IList<TaskAction> actions, IList<ExternalReferenceEntry>? externalReferenceEntries)
        {
            entry.Actions = [.. actions.Where(a => a.TaskId == entry.Id)];
            entry.ExternalReferenceEntries = [.. (externalReferenceEntries ?? []).Where(e => e.TaskId == entry.Id)];
            return entry;
        }
    }
}
