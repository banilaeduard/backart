﻿using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context
{
    public class ImportsDbContext: DbContext
    {
        private string comandaVanzareTable;
        public ImportsDbContext(DbContextOptions ctxBuilder): base(ctxBuilder) {        }

        public DbSet<ComandaVanzareEntry> ComandaVanzare { get; set; }
        //public DbSet<DataKeyLocation> DataKeyLocation { get; set; }

        //public async Task SetNewLocations(IList<(string NumeLocatie, string CodLocatie)> entries)
        //{
        //    var locs = entries.Select(t => t.NumeLocatie);
        //    var locations = DataKeyLocation.Where(t => locs.Contains(t.name)).ToList();

        //    Dictionary<string, string> items = new();

        //    foreach (var entry in entries)
        //    {
        //        var loc = locations.Where(t => t.name == entry.NumeLocatie).FirstOrDefault();
        //        if (loc == null)
        //        {
        //            items[entry.NumeLocatie] = entry.CodLocatie;
        //        }
        //    }

        //    foreach (var (nume, cod) in items)
        //    {
        //        DataKeyLocation.Add(new DataKeyLocation()
        //        {
        //            locationCode = cod,
        //            name = nume
        //        });
        //    }

        //    await SaveChangesAsync();
        //}

        //public async Task AddUniqueEntries(IList<ComandaVanzareEntry> entries)
        //{
        //    foreach (var entry in entries)
        //    {
        //        ComandaVanzare.Add(entry);
        //    }

        //    await Database.ExecuteSqlRawAsync($"DELETE FROM {comandaVanzareTable} WHERE TenantId = '{entries[0].TenantId}';");
        //    await SaveChangesAsync();
        //}
    }
}
