using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Marsman.AzureSearchToolkit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

[assembly: FunctionsStartup(typeof(TestApi.Startup))]
namespace TestApi
{
    public class SearchFunctions
    {
        private const string IndexName = "text-index";
        private const string SearchUriConfigKey = "SearchUri";
        private const string SearchKeyConfigKey = "SearchKey";
        private readonly ILogger<SearchFunctions> _logger;
        private readonly SearchIndexClient indexClient;
        private readonly SearchClient searchClient;

        public SearchFunctions(ILogger<SearchFunctions> log)
        {
            _logger = log;

            indexClient = new SearchIndexClient(
                new Uri(Environment.GetEnvironmentVariable(SearchUriConfigKey)),
                new Azure.AzureKeyCredential(Environment.GetEnvironmentVariable(SearchKeyConfigKey)));
            searchClient = indexClient.GetSearchClient(IndexName);
        }

        [FunctionName(nameof(ResetIndex))]
        [OpenApiOperation(operationId: nameof(ResetIndex), tags: new[] { "name" })]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "The OK response")]
        public async Task<IActionResult> ResetIndex(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reset")] HttpRequest req)
        {
            await indexClient.DeleteIndexAsync(IndexName);
            await indexClient.CreateOrUpdateIndexAsync(new SearchIndex(IndexName)
            {
                Fields = new FieldBuilder().Build(typeof(User))
            });
            var data = GenerateFakeData(numRecords: 5000, numRoles: 30, numTags: 50);
            await searchClient.MergeOrUploadDocumentsAsync(data);

            return new OkResult();
        }

        [FunctionName(nameof(GetInitialView))]
        [OpenApiOperation(operationId: nameof(GetInitialView), tags: new[] { "name" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SearchModel<User>), Description = "The OK response")]
        public async Task<IActionResult> GetInitialView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "initial")] HttpRequest req)
        {
            var result = await QueryManager<User>.InitialSearch(searchClient);
            return new OkObjectResult(result.Model);
        }

        [FunctionName(nameof(Search))]
        [OpenApiOperation(operationId: nameof(Search), tags: new[] { "name" })]
        [OpenApiRequestBody("application/json", typeof(SearchConfig))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SearchModel<User>), Description = "The OK response")]
        public async Task<IActionResult> Search(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "search")] SearchConfig searchModel, HttpRequest req)
        {
            var result = await QueryManager<User>.Search(
                searchClient, 
                searchModel, 
                opts => opts.QueryType = SearchQueryType.Full, // use full mode to enable fuzzy search
                (c,o) => string.IsNullOrWhiteSpace(c.Search) ? c.Search : $"{c.Search}~"); // add the fuzzy search switch to the search term, if there's a search term
            return new OkObjectResult(result.Model);
        }

        private static List<User> GenerateFakeData(int numRecords, int numRoles, int numTags)
        {
            var faker = SetUpFaker(numRoles, numTags);

            // seed some data
            var data = new List<User>();
            var fakeDataGenerator = faker.GenerateForever();
            var i = 0;
            foreach (var fakeData in fakeDataGenerator)
            {
                data.Add(fakeData);
                i++;
                if (i == numRecords) break;
            }
            var tags = data.SelectMany(x => x.Tags).Count();
            var distinctTags = data.SelectMany(x => x.Tags).Distinct().Count();
            var distinctRoles = data.Select(x => x.Role).Distinct().Count();
            return data;
        }

        private static Bogus.Faker<User> SetUpFaker(int numRoles, int numTags)
        {
            var faker = new Bogus.Faker<User>();
            var outerFaker = new Bogus.Faker();
            var rolePool = Enumerable.Range(0, numRoles).Select(x => outerFaker.Internet.DomainWord()).ToList();
            var tagPool = Enumerable.Range(0, numTags).Select(x => outerFaker.Internet.DomainWord()).ToList();
            faker.RuleFor(x => x.FirstName, (f, u) => f.Name.FirstName(f.Person.Gender));
            faker.RuleFor(x => x.LastName, (f, u) => f.Name.LastName(f.Person.Gender));
            faker.RuleFor(x => x.Balance, f => f.Random.Double(0, 1000000));
            faker.RuleFor(x => x.Description, f => f.Rant.Review());
            faker.RuleFor(x => x.Role, f => f.PickRandom(rolePool));
            faker.RuleFor(x => x.Enabled, f => f.Random.Bool());
            faker.RuleFor(x => x.Tags, f => Enumerable.Range(0, 3).Select(x => f.PickRandom(tagPool)).ToArray());
            faker.RuleFor(x => x.JoinedDate, f => f.Date.PastOffset(3));
            faker.RuleFor(x => x.Id, f => Guid.NewGuid().ToString());
            return faker;
        }
    }

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMvcCore().AddNewtonsoftJson(options => {
                options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
                options.SerializerSettings.FloatParseHandling = FloatParseHandling.Double;
            });
        }
    }
}

