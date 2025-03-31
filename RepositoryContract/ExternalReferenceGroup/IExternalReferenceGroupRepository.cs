namespace RepositoryContract.ExternalReferenceGroup
{
    public interface IExternalReferenceGroupRepository
    {
        Task<List<ExternalReferenceGroupEntry>> GetExternalReferences(string whereClause = null);
        /// <summary>
        /// RETURN THE OLD VALUES
        /// WHEN THE ENTRIES ARE NEW THEY WON'T BE PRESENT IN THE OUTPUT
        /// </summary>
        /// <param name="externals"></param>
        /// <returns></returns>
        Task<List<ExternalReferenceGroupEntry>> UpsertExternalReferences(List<ExternalReferenceGroupEntry> externals);
        Task DeleteExternalRefs(int[] externalRefs);
    }
}
