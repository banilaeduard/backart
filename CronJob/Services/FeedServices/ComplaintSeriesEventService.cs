﻿using DataAccess;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CronJob.Services.FeedServices
{
    internal class ComplaintSeriesEventService
    {
        private readonly ComplaintSeriesDbContext complaintService;
        public ComplaintSeriesEventService(
            DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            NoFilterBaseContext noFilter)
        {
            complaintService = new ComplaintSeriesDbContext(ctxBuilder, noFilter);
        }
        public async Task FeedComplaints(IProcessor<ComplaintSeries> processor, CancellationToken cancellationToken)
        {
            foreach (var complaint in complaintService.Complaints
                                                .Include(t => t.Tickets).ThenInclude(t => t.Images)
                                                .Include(t => t.Tickets).ThenInclude(t => t.codeLinks)
                                                .Where(t => t.CreatedDate > DateTime.Now.AddDays(-14)).OrderBy(t => t.CreatedDate))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (await processor.shouldProcess(complaint, complaint.Id.ToString()))
                {
                    await processor.process(complaint, complaint.Id.ToString());
                }
                else
                {
                    Console.WriteLine("Skipping: {0}\r\n", complaint.Id);
                }
            }
        }
    }
}