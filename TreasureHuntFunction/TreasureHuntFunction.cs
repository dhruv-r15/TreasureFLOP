using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Blobs;
using System.ComponentModel.DataAnnotations.Schema;

namespace TreasureHuntFunction
{
    public static class TreasureHuntFunction
    {
        [FunctionName("SubmitTreasure")]
        public static async Task<IActionResult> SubmitTreasure(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Table("treasureHunt")] IAsyncCollector<TreasureSubmission> treasureTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(requestBody);
            string teamName = data?.teamName ?? string.Empty;
            string treasureId = data?.treasureId ?? string.Empty;

            if (string.IsNullOrEmpty(teamName) || string.IsNullOrEmpty(treasureId))
            {
                return new BadRequestObjectResult("Please provide both teamName and treasureId in the request body");
            }

            var submission = new TreasureSubmission
            {
                PartitionKey = teamName,
                RowKey = Guid.NewGuid().ToString(),
                TreasureId = treasureId
            };

            await treasureTable.AddAsync(submission);

            return new OkObjectResult($"Submission recorded for team {teamName}");
        }

        [FunctionName("GetLeaderboard")]
        public static async Task<IActionResult> GetLeaderboard(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Table("treasureHunt")] CloudTable treasureTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            TableQuery<TreasureSubmission> query = new TableQuery<TreasureSubmission>();
            var submissions = await treasureTable.ExecuteQuerySegmentedAsync(query, null);

            var leaderboard = submissions.Results
                .GroupBy(s => s.PartitionKey)
                .Select(g => new
                {
                    TeamName = g.Key,
                    TreasuresFound = g.Select(s => s.TreasureId).Distinct().Count(),
                    LastSubmission = g.Max(s => s.Timestamp)
                })
                .OrderByDescending(t => t.TreasuresFound)
                .ThenBy(t => t.LastSubmission)
                .ToList();

            return new OkObjectResult(JsonConvert.SerializeObject(leaderboard));
        }

        [FunctionName("GetTreasureHint")]
        public static async Task<IActionResult> GetTreasureHint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string treasureId = req.Query["treasureId"].ToString();

            if (string.IsNullOrEmpty(treasureId))
            {
                return new BadRequestObjectResult("Please provide a treasureId query parameter");
            }

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
            string containerName = "treasure-hints";
            string blobName = $"{treasureId}.txt";

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadContentAsync();
                string hint = response.Value.Content.ToString();
                return new OkObjectResult(hint);
            }
            else
            {
                return new NotFoundObjectResult("No hint found for this treasure");
            }
        }
    }

    public class TreasureSubmission : TableEntity
    {
        public string TreasureId { get; set; } = string.Empty;
    }
}
