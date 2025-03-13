using AutoMapper;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using AzureServices;
using EntityDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RepositoryContract;
using RepositoryContract.CommitedOrders;
using RepositoryContract.DataKeyLocation;
using RepositoryContract.Report;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using Services.Storage;
using System.Linq;
using System.Text;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Authorize(Roles = "admin, basic")]
    public class TaskController : WebApiController2
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private ITaskRepository taskRepository;
        private ITicketEntryRepository ticketEntryRepository;
        private IDataKeyLocationRepository keyLocationRepository;
        private ReclamatiiReport reclamatiiReport;
        private StructuraReport structuraReport;
        private IReportEntryRepository reportEntry;
        private BlobAccessStorageService storageService;
        private ICommitedOrdersRepository commitedOrdersRepository;

        public TaskController(
            ILogger<ReportsController> logger,
            ITaskRepository taskRepository,
            ITicketEntryRepository ticketEntryRepository,
            IDataKeyLocationRepository keyLocationRepository,
            ReclamatiiReport reclamatiiReport,
            IReportEntryRepository reportEntry,
            BlobAccessStorageService storageService,
            StructuraReport structuraReport,
            ICommitedOrdersRepository commitedOrdersRepository,
            IMapper mapper) : base(logger, mapper)
        {
            this.taskRepository = taskRepository;
            this.ticketEntryRepository = ticketEntryRepository;
            this.keyLocationRepository = keyLocationRepository;
            this.reclamatiiReport = reclamatiiReport;
            this.structuraReport = structuraReport;
            this.reportEntry = reportEntry;
            this.storageService = storageService;
            this.commitedOrdersRepository = commitedOrdersRepository;
        }

        [HttpGet("{status}")]
        public async Task<IActionResult> GetTasks(string status)
        {
            var taskLists = await taskRepository.GetTasks(Enum.Parse<TaskInternalState>(status));

            var tickets = await ticketEntryRepository.GetAll();
            var synonimLocations = (await keyLocationRepository.GetLocations()).Where(t => taskLists.Any(o => o.LocationCode == t.LocationCode)).ToList();
            return Ok(TaskModel.From(taskLists, tickets, synonimLocations));
        }

        [HttpPost]
        public async Task<IActionResult> SaveTask(TaskModel task)
        {
            var dbTask = task.ToTaskEntry();
            var newTask = await taskRepository.SaveTask(dbTask);

            if (task.ExternalMailReferences?.FirstOrDefault() != null)
            {
                var client = await QueueService.GetClient("movemailto");
                await client.SendMessageAsync(QueueService.Serialize(
                    new MoveToMessage<TableEntityPK>
                    {
                        DestinationFolder = "_PENDING_",
                        Items = task.ExternalMailReferences.SelectMany(x => x.Tickets).Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!))
                    }));
            }

            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
        }

        [HttpDelete("{taskId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTasks(int taskId)
        {
            await taskRepository.DeleteTask(taskId);
            return Ok();
        }

        [HttpDelete("{taskId}/delete/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTaskExternalRef(int taskId, string partitionKey, string rowKey)
        {
            await taskRepository.DeleteTaskExternalRef(taskId, partitionKey, rowKey);
            return Ok();
        }

        [HttpDelete("{taskId}/accept/{partitionKey}/{rowKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AcceptTaskExternalRef(int taskId, string partitionKey, string rowKey)
        {
            await taskRepository.AcceptExternalRef(taskId, partitionKey, rowKey);
            var client = await QueueService.GetClient("movemailto");
            await client.SendMessageAsync(QueueService.Serialize(
                new MoveToMessage<TableEntityPK>
                {
                    DestinationFolder = "_PENDING_",
                    Items = [TableEntityPK.From(partitionKey, rowKey)]
                }));
            return Ok();
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateTask(TaskModel task)
        {
            var newTask = await taskRepository.UpdateTask(task.ToTaskEntry());

            if (newTask.Actions.Any())
            {
                var lastAction = newTask.Actions.OrderByDescending(t => t.Created).First();
                if (lastAction.Description.Contains("Mark as closed"))
                {
                    var client = await QueueService.GetClient("movemailto");
                    await client.SendMessageAsync(QueueService.Serialize(
                        new MoveToMessage<TableEntityPK>
                        {
                            DestinationFolder = "Archive",
                            Items = newTask.ExternalReferenceEntries.Where(t => t.TableName == nameof(TicketEntity)).Select(x => TableEntityPK.From(x.PartitionKey, x.RowKey))
                        }));
                }
            }

            return Ok(TaskModel.From([newTask], await ticketEntryRepository.GetAll(), [.. await keyLocationRepository.GetLocations()]).First());
        }

        [HttpPost("reclamatii")]
        public async Task<IActionResult> ExportReclamatii(ComplaintDocument document)
        {
            BlobLeaseClient leaseObj = null;
            try
            {
                StringBuilder sb = new StringBuilder();
                var locMap = (await reportEntry.GetLocationMapPathEntry("1", t => t.RowKey == document.LocationCode)).FirstOrDefault();

                if (locMap == null)
                {
                    locMap = await reportEntry.AddEntry(new LocationMap()
                    {
                        PartitionKey = "1",
                        RowKey = document.LocationCode,
                        Folder = document.LocationName,
                        Location = document.LocationName
                    }, $"{nameof(LocationMap)}");
                }

                foreach (var item in document.complaintEntries)
                {
                    if (!string.IsNullOrWhiteSpace(item.RefPartitionKey) && !string.IsNullOrWhiteSpace(item.RefRowKey))
                        sb.Append($"{item.RefRowKey}{item.RefPartitionKey}");
                }
                sb.Append(document.complaintEntries.Count());
                var md5 = CreateMD5(sb.ToString());

                leaseObj = storageService.GetLease($"locks/{document.NumarIntern}REC");
                var lease = leaseObj.Acquire(TimeSpan.FromMinutes(1)).Value;

                var fName = $"reclamatii-drafts/{locMap.Folder}/{document.NumarIntern}.docx";
                var metaData = storageService.GetMetadata(fName);

                if (metaData.ContainsKey("md5"))
                {
                    if (md5.Equals(metaData["md5"]))
                    {
                        var content = storageService.AccessIfExists(fName, out var contentType2);
                        return File(content, contentType);
                    }
                }

                var reportBytes = await reclamatiiReport.GenerateReport(document);
                storageService.Upload(fName, new BinaryData(reportBytes));
                metaData["json"] = JsonConvert.SerializeObject(document);
                metaData["md5"] = md5;
                storageService.SetMetadata(fName, null, metaData);

                return File(reportBytes, contentType);
            }
            catch (Exception ex)
            {
                return File(await reclamatiiReport.GenerateReport(document), contentType);
            }
            finally
            {
                leaseObj?.Release();
            }
        }

        [HttpPost("structurareport/{reportName}/{dispozitie}")]
        public async Task<IActionResult> ExportStructuraReport(string reportName, int dispozitie)
        {
            BlobLeaseClient leaseObj = null;
            var items = (await commitedOrdersRepository.GetCommitedOrder(dispozitie)).ToList();
            try
            {
                var locMap = (await reportEntry.GetLocationMapPathEntry("1", t => t.RowKey == items[0].CodLocatie)).FirstOrDefault();
                if (locMap == null)
                {
                    locMap = await reportEntry.AddEntry(new LocationMap()
                    {
                        PartitionKey = "1",
                        RowKey = items[0].CodLocatie,
                        Folder = items[0].NumeLocatie,
                        Location = items[0].NumeLocatie
                    }, $"{nameof(LocationMap)}");
                }

                var fName = $"pv_accesorii/{locMap.Folder}/{dispozitie}.docx";
                var metaData = storageService.GetMetadata(fName);

                List<string> list = new List<string>();
                foreach (var item in items)
                {
                    list.Add(CreateMD5($"{item.CodProdus}{item.NumarComanda}{item.Cantitate}"));
                }
                if (items[0].NumarAviz.HasValue)
                    list.Add(CreateMD5(items[0].NumarAviz!.ToString()));

                var stringToHash = string.Join("", list.Order());
                var md5 = CreateMD5(stringToHash);

                leaseObj = storageService.GetLease($"locks/{dispozitie}PV");
                var lease = leaseObj.Acquire(TimeSpan.FromMinutes(1)).Value;

                if (metaData.ContainsKey("md5"))
                {
                    if (md5.Equals(metaData["md5"]))
                    {
                        var content = storageService.AccessIfExists(fName, out var contentType2);
                        return File(content, contentType);
                    }
                }

                var reportBytes = await structuraReport.GenerateReport(items, reportName);
                storageService.Upload(fName, new BinaryData(reportBytes));
                if (!string.IsNullOrWhiteSpace(items[0].NumarAviz?.ToString()))
                    metaData["aviz"] = items[0].NumarAviz?.ToString();
                metaData["md5"] = md5;
                storageService.SetMetadata(fName, null, metaData);

                return File(reportBytes, contentType);

            }
            catch (Exception ex)
            {
                return File(await structuraReport.GenerateReport(items, reportName), contentType);
            }
            finally
            {
                leaseObj?.Release();
            }
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
