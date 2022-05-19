using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Marsman.AzureSearchToolkit
{
    public static class SearchConfigManager
    {
        public const int DefaultPageSize = 50;
    }
    public static class SearchConfigManager<T>
    {
        internal static readonly List<(PropertyInfo Property, SimpleFieldAttribute FieldAttribute)> Props =
                     typeof(T).GetTypeInfo()
                              .DeclaredProperties
                              .Where(x => x.GetCustomAttribute<SearchableFieldAttribute>() != null || x.GetCustomAttribute<SimpleFieldAttribute>() != null)
                              .Select(x => (Property: x, FieldAttribute: x.GetCustomAttribute<SearchableFieldAttribute>() ?? x.GetCustomAttribute<SimpleFieldAttribute>()))
                              .ToList();

        public static SearchConfig GetEmptySearchConfigWithFacets(SearchResults<T> initialQuery)
        {
            var result = GetEmptySearchConfig();
            result.Facets = FacetManager<T>.GetFacetsModel(initialQuery);
            result.TotalResults = initialQuery.TotalCount.HasValue ? initialQuery.TotalCount.Value : 0;
            result.TotalPages = initialQuery.TotalCount.HasValue ? initialQuery.TotalCount.Value / SearchConfigManager.DefaultPageSize : 0;
            return result;
        }

        public static SearchConfig GetEmptySearchConfig()
        {
            var filterSelectors = GetFilterSelectors();
            return new SearchConfig
            {
                OrderingFields = GetOrderingSelectors(),
                SearchFields = GetSearchFieldSelectors(),
                SelectFields = GetFieldSelectors(),
                Page = 1, // always 1 for an empty config
                ResultsPerPage = SearchConfigManager.DefaultPageSize, // default value for an empty config
                StringFieldFilters = filterSelectors.Where(x => x is StringFieldFilter).Cast<StringFieldFilter>().ToList(),
                NumericFieldFilters = filterSelectors.Where(x => x is NumericFieldFilter).Cast<NumericFieldFilter>().ToList(),
                BoolFieldFilters = filterSelectors.Where(x => x is BoolFieldFilter).Cast<BoolFieldFilter>().ToList(),
                DateTimeFieldFilters = filterSelectors.Where(x => x is DateTimeOffsetFieldFilter).Cast<DateTimeOffsetFieldFilter>().ToList(),
            };
        }

        /// <summary>
        /// Use this if you have a custom config model which extends <see cref="SearchConfig"/> and you want to have the base
        /// properties set
        /// </summary>
        public static Tconfig HydrateEmptySearchConfig<Tconfig>(Tconfig searchConfig) where Tconfig : SearchConfig
        {
            var filterSelectors = GetFilterSelectors();
            searchConfig.OrderingFields ??= GetOrderingSelectors();
            searchConfig.SearchFields ??= GetSearchFieldSelectors();
            searchConfig.SelectFields ??= GetFieldSelectors();
            searchConfig.Page ??= 1;
            searchConfig.ResultsPerPage ??= SearchConfigManager.DefaultPageSize;
            searchConfig.StringFieldFilters ??= filterSelectors.Where(x => x is StringFieldFilter).Cast<StringFieldFilter>().ToList();
            searchConfig.NumericFieldFilters ??= filterSelectors.Where(x => x is NumericFieldFilter).Cast<NumericFieldFilter>().ToList();
            searchConfig.BoolFieldFilters ??= filterSelectors.Where(x => x is BoolFieldFilter).Cast<BoolFieldFilter>().ToList();
            searchConfig.DateTimeFieldFilters ??= filterSelectors.Where(x => x is DateTimeOffsetFieldFilter).Cast<DateTimeOffsetFieldFilter>().ToList();
            return searchConfig;
        }

        /// <summary>
        /// Use this if you have a custom config model which extends <see cref="SearchConfig"/> and you want to have the base
        /// properties set
        /// </summary>
        public static Tconfig HydrateEmptySearchConfigWithFacets<Tconfig>(Tconfig searchConfig, SearchResults<T> initialQuery) where Tconfig : SearchConfig
        {
            var filterSelectors = GetFilterSelectors();
            searchConfig.Facets ??= FacetManager<T>.GetFacetsModel(initialQuery);
            searchConfig.OrderingFields ??= GetOrderingSelectors();
            searchConfig.SearchFields ??= GetSearchFieldSelectors();
            searchConfig.SelectFields ??= GetFieldSelectors();
            searchConfig.TotalResults ??= initialQuery.TotalCount;
            searchConfig.TotalPages ??= initialQuery.TotalCount.HasValue ? initialQuery.TotalCount.Value / SearchConfigManager.DefaultPageSize : null;
            searchConfig.Page ??= 1;
            searchConfig.ResultsPerPage ??= SearchConfigManager.DefaultPageSize;
            searchConfig.StringFieldFilters ??= filterSelectors.Where(x => x is StringFieldFilter).Cast<StringFieldFilter>().ToList();
            searchConfig.NumericFieldFilters ??= filterSelectors.Where(x => x is NumericFieldFilter).Cast<NumericFieldFilter>().ToList();
            searchConfig.BoolFieldFilters ??= filterSelectors.Where(x => x is BoolFieldFilter).Cast<BoolFieldFilter>().ToList();
            searchConfig.DateTimeFieldFilters ??= filterSelectors.Where(x => x is DateTimeOffsetFieldFilter).Cast<DateTimeOffsetFieldFilter>().ToList();
            return searchConfig;
        }

        internal static void ValidateFieldNames(SearchConfig config)
        {
            var select = config.SelectFields.Select(x => x.FieldName);

            var allFieldNames = config.SelectFields
                                      .EmptyIfNull()
                                      .Select(x => x.FieldName)
                                      .IfNotNull(config.SearchFields, x => x.Concat(config.SearchFields.Select(x => x.FieldName)))
                                      .IfNotNull(config.OrderingFields, x => x.Concat(config.OrderingFields.Select(x => x.FieldName)))
                                      .IfNotNull(config.FieldFilters, x => x.Concat(config.FieldFilters.Select(x => x.FieldName)))
                                      .IfNotNull(config.Facets, x => x.Concat(config.Facets.Select(x => x.FacetName)));

            if (allFieldNames.Any(x => !Props.Any(p => p.Property.GetSearchFieldName() == x)))
            {
                throw new InvalidOperationException("One of the specified field names is not a valid field name in the schema");
            }
        }

        private static IList<ResultFieldSelector> GetFieldSelectors() =>
            Props.Select(x => new ResultFieldSelector 
            { 
                FieldName = x.Property.GetSearchFieldName(),
                NumberFormat = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.NumberFormat,
                DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName,
                DateTimeFormat = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DateTimeFormat ?? DateTimeDisplayFormat.DateTime,
                ValueType = x.Property.PropertyType.GetValueType()
            }).ToList();

        private static IList<SearchFieldSelector> GetSearchFieldSelectors() =>
            Props.Where(x => x.FieldAttribute is SearchableFieldAttribute).Select(x => new SearchFieldSelector 
                { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() }).ToList();

        private static IList<OrderingSelector> GetOrderingSelectors() =>
            Props.Where(x => x.FieldAttribute.IsSortable).Select(x => new OrderingSelector { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() }).ToList();

        private static IList<FieldFilter> GetFilterSelectors() =>
            Props.Where(x => x.FieldAttribute.IsFilterable).Select(GetFilterSelector).ToList();

        private static FieldFilter GetFilterSelector((PropertyInfo Property, SimpleFieldAttribute FieldAttribute) x) =>
            x.Property.PropertyType.UnwrapNullable() switch
            {
                var t when Is<long>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() }, // matches any integer type < 64 bit
                var t when Is<double>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() }, // matches double or float
                var t when Is<decimal>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() },
                var t when Is<DateTimeOffset>(t) => new DateTimeOffsetFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() },
                var t when Is<bool>(t) => new BoolFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() },
                var t when Is<string>(t) => new StringFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() },

                var t when IsMany<long>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },
                var t when IsMany<double>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },
                var t when IsMany<decimal>(t) => new NumericFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },
                var t when IsMany<DateTimeOffset>(t) => new DateTimeOffsetFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },
                var t when IsMany<bool>(t) => new BoolFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },
                var t when IsMany<string>(t) => new StringFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName(), IsCollection = true },

                _ => new StringFieldFilter { DisplayName = x.Property.GetCustomAttribute<SearchToolkitDisplayAttribute>()?.DisplayName, FieldName = x.Property.GetSearchFieldName() }
            };


        private static bool Is<Tin>(Type t1) => typeof(Tin).IsAssignableFrom(t1);

        private static bool IsMany<Tin>(Type t1) => typeof(IEnumerable<Tin>).IsAssignableFrom(t1);
    }
}
