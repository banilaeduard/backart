using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Contexts
{
    public class ImportsDbContext : BaseContext
    {
        public ImportsDbContext(DbContextOptions<ImportsDbContext> ctxBuilder, IBaseContextAccesor baseContextAccesor) : base(ctxBuilder, baseContextAccesor)
        {
        }

        public DbSet<ComandaVanzareEntry> ComandaVanzare { get; set; }
        public DbSet<DataKeyLocation> DataKeyLocation { get; set; }

        public async Task SetNewLocations(IList<ComandaVanzareEntry> entries)
        {
            var locs = entries.Select(t => t.CodLocatie).ToList();
            var locations = DataKeyLocation.Where(t => locs.Contains(t.locationCode)).ToList();

            Dictionary<string, string> items = new();

            foreach (var entry in entries)
            {
                var loc = locations.Where(t => t.locationCode == entry.CodLocatie).FirstOrDefault();
                if (loc == null)
                {
                    items[entry.CodLocatie] = entry.NumeLocatie;
                }
            }

            foreach (var (cod, nume) in items)
            {
                DataKeyLocation.Add(new DataKeyLocation()
                {
                    locationCode = cod,
                    name = nume
                });
            }

            await this.SaveChangesAsync();

            locations = DataKeyLocation.ToList();
            foreach (var entry in entries)
            {
                var loc = locations.Where(t => t.locationCode == entry.CodLocatie).FirstOrDefault();
                entry.DataKeyId = loc.Id;
                entry.DataKey = loc;
            }
        }

        public async Task AddUniqueEntries(IList<ComandaVanzareEntry> entries)
        {
            var docIds = entries.Select(t => t.DocId).ToList();
            var existing = ComandaVanzare.Where(t => docIds.Contains(t.DocId)).ToList();
            existing.ForEach(t => Entry(t).State = EntityState.Detached);

            foreach (var entry in entries.Where(t => !existing.Any(x => x.DocId == t.DocId)))
            {
                ComandaVanzare.Add(entry);
            }

            await this.SaveChangesAsync();
        }

        protected override void BeforeSave(EntityEntry entityEntry, string correlationId)
        {
        }
    }
}
