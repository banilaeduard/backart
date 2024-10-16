﻿namespace RepositoryContract.Tasks
{
    public class ExternalReferenceEntry
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int TaskActionId { get; set; }
        public string TableName { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public bool IsRemoved { get; set; }
        public DateTime Created { get; set; }
        public int GroupId { get; set; }
        public string ExternalGroupId { get; set; }
        public DateTime Date { get; set; }
    }
}