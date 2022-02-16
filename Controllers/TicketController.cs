namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    using WebApi.Entities;
    using WebApi.Models;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System;
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;

    [Authorize(Roles = "partener, admin")]
    public class TicketController : WebApiController2
    {
        private ComplaintSeriesDbContext complaintSeriesDbContext;
        public TicketController(ComplaintSeriesDbContext complaintSeriesDbContext,
        ILogger<TicketController> logger) : base(logger)
        {
            this.complaintSeriesDbContext = complaintSeriesDbContext;
        }

        [HttpGet("{page}/{pageSize}")]
        public IActionResult GetAll(int page, int pageSize)
        {
            return Ok(new
            {
                count = this.complaintSeriesDbContext.Complaints.Count(),
                complaints = this.complaintSeriesDbContext.Complaints
                        .OrderByDescending(t => t.Id)
                        .Include(t => t.Tickets)
                        .ThenInclude(t => t.codeLinks)
                        .Include(t => t.Tickets)
                         .ThenInclude(t => t.Images)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(t => ComplaintSeriesModel.from(t))
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplaint(ComplaintSeriesModel complaint)
        {
            var dbModel = complaint.toDbModel();

            if (complaint.Id < 1)
                this.complaintSeriesDbContext.Complaints.Add(dbModel);
            else
                this.complaintSeriesDbContext.Complaints.Update(dbModel);


            var ticket = complaint.Tickets[0];

            if (ticket.ToDeleteImages != null)
            {
                foreach (var toDelete in ticket.ToDeleteImages)
                {
                    this.complaintSeriesDbContext.Entry(new Image() { Id = toDelete.Id }).State = EntityState.Deleted;
                }
            }

            if (ticket.ToAddImages != null && ticket.ToAddImages.Count > 0)
            {
                using (HashAlgorithm algorithm = SHA256.Create())
                {
                    Func<string, byte[]> hasher = (inputString) => algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

                    foreach (var toAdd in ticket.ToAddImages)
                    {
                        var hash = GetHashString(toAdd.Data, hasher);
                        Console.WriteLine(hash);

                        DirectoryInfo dir = new DirectoryInfo(string.Format("/photos/{0}", hash.Substring(0, 2)));

                        if (!dir.Exists) dir.Create();

                        var file = new FileInfo(Path.Combine(
                                                dir.FullName, 
                                                string.Format("{0}{1}", hash, Path.GetExtension(toAdd.Title)
                                                ))
                            );

                        if (!file.Exists) // you may not want to overwrite existing files
                        {
                            Stream stream = file.OpenWrite();
                            byte[] _file = System.Convert.FromBase64String(toAdd.Data);
                            stream.WriteAsync(_file, 0, _file.Length).Forget(this.logger, () =>
                                stream.DisposeAsync());
                        }

                        toAdd.Ticket = dbModel.Tickets[0];
                        toAdd.Data = file.FullName;
                        this.complaintSeriesDbContext.Entry(toAdd).State = EntityState.Added;
                    }
                }
            }

            if (ticket.ToDeleteImages != null)
            {
                foreach (var toDelete in ticket.ToDeleteImages)
                {
                    this.complaintSeriesDbContext.Entry(new Image() { Id = toDelete.Id });
                }
            }

            await this.complaintSeriesDbContext.SaveChangesAsync();
            return Ok(ComplaintSeriesModel.from(
                this.complaintSeriesDbContext.Complaints
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.codeLinks)
                         .Include(t => t.Tickets)
                         .ThenInclude(t => t.Images)
                         .AsSplitQuery()
                         .SingleOrDefault(t => t.Id == dbModel.Id)
                         ));
        }
        public static string GetHashString(string inputString, Func<string, byte[]> hasher)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hasher(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}