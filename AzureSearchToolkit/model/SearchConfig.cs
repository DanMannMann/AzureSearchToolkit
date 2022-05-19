using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class SearchConfig
    {
        public string Search { get; set; }
        public IList<ResultFieldSelector> SelectFields { get; set; }
        public IList<FacetSet> Facets { get; set; }
        public IList<SearchFieldSelector> SearchFields { get; set; }
        public IList<OrderingSelector> OrderingFields { get; set; }

        public IList<NumericFieldFilter> NumericFieldFilters { get; set; }
        public IList<StringFieldFilter> StringFieldFilters { get; set; }
        public IList<BoolFieldFilter> BoolFieldFilters { get; set; }
        public IList<DateTimeOffsetFieldFilter> DateTimeFieldFilters { get; set; }


        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public LogicalOperator CombineFacets { get; set; } = LogicalOperator.And;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public LogicalOperator CombineFacetValues { get; set; } = LogicalOperator.Or;

        public int? Page { get; set; }
        public long? TotalPages { get; set; }
        public int? ResultsPerPage { get; set; } = SearchConfigManager.DefaultPageSize;
        public long? TotalResults { get; set; }
        public long? TotalFilteredResults { get; set; }
        public string FilterHash { get; set; }
        public string SearchHash { get; set; }

        internal List<FieldFilter> FieldFilters =>
            NumericFieldFilters.Cast<FieldFilter>()
                               .Concat(StringFieldFilters.Cast<FieldFilter>())
                               .Concat(BoolFieldFilters.Cast<FieldFilter>())
                               .Concat(DateTimeFieldFilters.Cast<FieldFilter>())
                               .ToList();
    }
}
