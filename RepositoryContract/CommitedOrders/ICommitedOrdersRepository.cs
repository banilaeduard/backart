﻿using EntityDto;

namespace RepositoryContract.CommitedOrders
{
    public interface ICommitedOrdersRepository
    {
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders(Func<DispozitieLivrareEntry, bool> expr);
        Task<List<DispozitieLivrareEntry>> GetCommitedOrders();
        Task<DateTime?> GetLastSyncDate();
        Task DeleteCommitedOrders(List<DispozitieLivrareEntry> items);
        Task InsertCommitedOrder(DispozitieLivrareEntry item);
        Task SetDelivered(int internalNumber);
        Task ImportCommitedOrders(IList<DispozitieLivrare> items, DateTime when);
    }
}
