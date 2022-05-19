using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Marsman.AzureSearchToolkit
{
    public static class QueryManager<T>
    {
        private static SHA256 hashAlgo = SHA256.Create();
        private static string GetHash(string plaintext) =>
            Convert.ToBase64String(hashAlgo.ComputeHash(Encoding.UTF8.GetBytes(plaintext ?? string.Empty)));

        public static string GenerateFilter(SearchConfig config)
        {
            SearchConfigManager<T>.ValidateFieldNames(config); // Will throw if any field names have been tampered with. Important since they're used unescaped later.
            var sb = new StringBuilder();
            foreach (var item in config.FieldFilters.Where(x => x.IsUsed).Select((x, i) => (filter: x.GetFilter(), i)))
            {
                if (item.i > 0) sb.Append(" and ");
                sb.Append(item.filter);
            }
            var facetFilter = FacetManager<T>.BuildFacetFilter(config.Facets, config.CombineFacets, config.CombineFacetValues);
            if (!string.IsNullOrWhiteSpace(facetFilter))
            {
                if (config.FieldFilters.Any(x => x.IsUsed)) sb.Append(" and ");
                sb.Append(facetFilter);
            }
            return sb.ToString();
        }

        public static SearchOptions GenerateSearchOptions(SearchConfig config, params FacetSpecification<T>[] runtimeSpecs)
        {
            var opts = new SearchOptions();
            opts.Filter = GenerateFilter(config);
            opts.IncludeTotalCount = !config.TotalResults.HasValue;
            opts.Size = config.ResultsPerPage;

            foreach (var field in config.SearchFields.Where(x => x.Selected).Select(x => x.FieldName))
            {
                opts.SearchFields.Add(field);
            }
            foreach (var field in config.OrderingFields.Where(x => x.Order != SortOrder.None))
            {
                opts.OrderBy.Add($"{field.FieldName} {(field.Order == SortOrder.Ascending ? "asc" : "desc")}");
            }
            foreach (var facet in GetFacetDeclarations(runtimeSpecs))
            {
                opts.Facets.Add(facet);
            }
            if (config.SelectFields.All(x => x.Selected)) return opts; // no need for Select if all fields selected

            foreach (var field in config.SelectFields.Where(x => x.Selected).Select(x => x.FieldName))
            {
                opts.Select.Add(field);
            }
            return opts;
        }

        /// <summary>
        /// Performs a wildcard search over the index with unmodified config and returns a model
        /// which can be used to control the search alongside the first page of results.
        /// </summary>
        public static Task<SearchOperation<T>> InitialSearch(SearchClient client, params FacetSpecification<T>[] runtimeSpecs) =>
            InitialSearch(client, null, runtimeSpecs);

        /// <summary>
        /// Performs a wildcard search over the index with unmodified config and returns a model
        /// which can be used to control the search alongside the first page of results.
        /// </summary>
        public static Task<SearchOperation<T>> InitialSearch(SearchClient client,
                                                             Action<SearchOptions> optionsModifier,
                                                             params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, SearchConfigManager<T>.GetEmptySearchConfig(), optionsModifier, runtimeSpecs);

        public static Task<SearchOperation<T>> Search(SearchClient client, SearchConfig config, params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, config, null, runtimeSpecs);

        public static Task<SearchOperation<T>> Search(SearchClient client,
                                                            SearchConfig config,
                                                            Action<SearchOptions> optionsModifier,
                                                            params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, config, optionsModifier, (x,y) => x.Search, runtimeSpecs);

        public static async Task<SearchOperation<T>> Search(SearchClient client,
                                                            SearchConfig config,
                                                            Action<SearchOptions> optionsModifier,
                                                            Func<SearchConfig,SearchOptions,string> searchTermFactory,
                                                            params FacetSpecification<T>[] runtimeSpecs)
        {
            var result = await ExecuteSearch(client, config, optionsModifier, runtimeSpecs, searchTermFactory);
            return new SearchOperation<T>
            {
                Model = new SearchModel<T>
                {
                    SearchConfig = config,
                    Results = result.Select(x => x.Document).ToList()
                },
                ResultDetails = result
            };
        }

        /// <summary>
        /// Performs a wildcard search over the index with unmodified config and returns a model
        /// which can be used to control the search alongside the first page of results. Accepts
        /// a metadata factory which can compose metadata (e.g. for highlighting)
        /// for the results in the model.
        /// </summary>
        public static Task<SearchOperation<T, Tmeta>> InitialSearch<Tmeta>(SearchClient client,
                                                                           Func<SearchResult<T>, Tmeta> metadataFactory,
                                                                           params FacetSpecification<T>[] runtimeSpecs) =>
            InitialSearch(client, null, metadataFactory, runtimeSpecs);

        /// <summary>
        /// Performs a wildcard search over the index with unmodified config and returns a model
        /// which can be used to control the search alongside the first page of results. Accepts
        /// a metadata factory which can compose metadata (e.g. for highlighting)
        /// for the results in the model.
        /// </summary>
        public static Task<SearchOperation<T, Tmeta>> InitialSearch<Tmeta>(SearchClient client,
                                                                           Action<SearchOptions> optionsModifier,
                                                                           Func<SearchResult<T>, Tmeta> metadataFactory,
                                                                           params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, SearchConfigManager<T>.GetEmptySearchConfig(), optionsModifier, metadataFactory, runtimeSpecs);

        public static Task<SearchOperation<T, Tmeta>> Search<Tmeta>(SearchClient client,
                                                                    SearchConfig config,
                                                                    Func<SearchResult<T>, Tmeta> metadataFactory,
                                                                    params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, config, null, metadataFactory, runtimeSpecs);

        public static Task<SearchOperation<T, Tmeta>> Search<Tmeta>(SearchClient client,
                                                                          SearchConfig config,
                                                                          Action<SearchOptions> optionsModifier,
                                                                          Func<SearchResult<T>, Tmeta> metadataFactory,
                                                                          params FacetSpecification<T>[] runtimeSpecs) =>
            Search(client, config, optionsModifier, metadataFactory, (x, y) => x.Search, runtimeSpecs);

        public static async Task<SearchOperation<T, Tmeta>> Search<Tmeta>(SearchClient client,
                                                                          SearchConfig config,
                                                                          Action<SearchOptions> optionsModifier,
                                                                          Func<SearchResult<T>, Tmeta> metadataFactory,
                                                                          Func<SearchConfig, SearchOptions, string> searchTermFactory,
                                                                          params FacetSpecification<T>[] runtimeSpecs)
        {
            var result = await ExecuteSearch(client, config, optionsModifier, runtimeSpecs, searchTermFactory);
            return new SearchOperation<T, Tmeta>
            {
                Model = new SearchModel<SearchResultWithMetadata<T, Tmeta>>
                {
                    SearchConfig = config,
                    Results = result.Select(x => new SearchResultWithMetadata<T, Tmeta> { Value = x.Document, Metadata = metadataFactory(x) }).ToList()
                },
                ResultDetails = result
            };
        }

        private static async Task<List<SearchResult<T>>> ExecuteSearch(SearchClient client,
                                                                       SearchConfig config,
                                                                       Action<SearchOptions> optionsModifier,
                                                                       FacetSpecification<T>[] runtimeSpecs,
                                                                       Func<SearchConfig, SearchOptions, string> searchTermFactory)
        {
            var searchOptions = PrepareSearch(config, optionsModifier, runtimeSpecs);
            var result = await client.SearchAsync<T>(searchTermFactory(config, searchOptions), searchOptions);
            UpdateConfigPostSearch(config, searchOptions, result);

            var itemResult = new List<SearchResult<T>>();
            foreach (var item in result.Value.GetResults()) itemResult.Add(item);
            return itemResult;
        }

        private static SearchOptions PrepareSearch(SearchConfig config, Action<SearchOptions> optionsModifier, FacetSpecification<T>[] runtimeSpecs)
        {
            config.Page ??= 1;
            config.ResultsPerPage ??= SearchConfigManager.DefaultPageSize;
            if (config.SearchHash != GetHash(config.Search))
            {
                config.TotalResults = config.TotalPages = null;
                config.Facets = null;
                config.Page = 1;
            }
            var searchOptions = GenerateSearchOptions(config, runtimeSpecs);
            optionsModifier?.Invoke(searchOptions);
            if (config.FilterHash != GetHash(searchOptions.Filter))
            {
                searchOptions.IncludeTotalCount = true;
                config.TotalResults = config.TotalPages = null;
                config.Page = 1;
            }
            if (config.Page.HasValue && config.Page.Value > 1) searchOptions.Skip = (config.Page.Value - 1) * config.ResultsPerPage.Value;
            return searchOptions;
        }

        private static void UpdateConfigPostSearch(SearchConfig config, SearchOptions searchOptions, Azure.Response<SearchResults<T>> result)
        {
            // ensure any unset or stale values that are settable on config are set
            config.FilterHash = GetHash(searchOptions.Filter);
            config.SearchHash = GetHash(config.Search);
            config.TotalResults ??= result.Value.TotalCount;
            config.TotalPages ??= result.Value.TotalCount.HasValue && config.ResultsPerPage.HasValue
                                    ? GetPageCount(result.Value.TotalCount.Value, config.ResultsPerPage.Value)
                                    : null;

            var updatedFacetModel = FacetManager<T>.GetFacetsModel(result.Value);
            if (config.Facets == null)
            {
                config.Facets = updatedFacetModel;
                return;
            }

            foreach (var facet in config.Facets)
            {
                var updatedFacet = updatedFacetModel.FirstOrDefault(x => x.FacetName == facet.FacetName);
                foreach (var value in facet.Values)
                {
                    var updatedValue = updatedFacet.Values.FirstOrDefault(x => x == value);
                    if (updatedValue != null)
                    {
                        value.FilteredCount = updatedValue.Count;
                        if (value.Count < value.FilteredCount) value.Count = value.FilteredCount; // if other filters have been removed, the actual total may increase
                    }
                    else
                    {
                        value.FilteredCount = 0;
                    }
                }

                // Keep any new facets that aren't in the original set
                var newValues = updatedFacet.Values.Where(x => !facet.Values.Contains(x)).ToList();
                newValues.ForEach(facet.Values.Add);
            }
        }

        private static long GetPageCount(long count, int pageSize)
        {
            if (pageSize == 0) return 1;
            // This calculation does rounding up of integer division in a round-down language http://www.cs.nott.ac.uk/~psarb2/G51MPC/slides/NumberLogic.pdf
            // The addition could overflow resulting in a negative page count. That would be an extreme circumstance that is not accounted for here.
            return (count + pageSize - 1) / pageSize;
        }

        private static IList<string> GetFacetDeclarations(FacetSpecification<T>[] runtimeSpecs)
        {
            return runtimeSpecs?.Any(x => x is RangeSpecification<T>) == true
                ? FacetManager<T>.GetFacetDeclarations(GetRangeSpecifications(runtimeSpecs)) // facets with runtime range config
                : FacetManager<T>.GetFacetDeclarations(); // no runtime range config
        }

        private static RangeSpecification<T>[] GetRangeSpecifications(FacetSpecification<T>[] runtimeSpecs)
        {
            return runtimeSpecs.Where(x => x is RangeSpecification<T>).Cast<RangeSpecification<T>>().ToArray();
        }
    }

    public class SearchOperation<T>
    {
        public SearchModel<T> Model { get; set; }
        public IEnumerable<SearchResult<T>> ResultDetails { get; set; }
    }
    public class SearchModel<T>
    {
        public SearchConfig SearchConfig { get; set; }
        public IEnumerable<T> Results { get; set; }
    }

    public class SearchOperation<T,Tmeta>
    {
        public SearchModel<SearchResultWithMetadata<T, Tmeta>> Model { get; set; }
        public IEnumerable<SearchResult<T>> ResultDetails { get; set; }
    }
    public class SearchResultWithMetadata<T,Tmeta>
    {
        public T Value { get; set; }
        public Tmeta Metadata { get; set; }
    }
}
