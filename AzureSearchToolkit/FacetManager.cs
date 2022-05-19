using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Marsman.Reflekt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Marsman.AzureSearchToolkit
{
    public class FacetManager<T>
    {
        private static List<(PropertyInfo Property, SearchToolkitFacetAttribute FacetAttribute)> props =
            typeof(T).GetTypeInfo()
                    .DeclaredProperties
                    .Where(IsFacet)
                    .Select(x => 
                    (
                    Property: x,
                    // facetables with no [Facet] attribute are assumed to be simple value facets
                    FacetAttribute: x.GetCustomAttribute<SearchToolkitFacetAttribute>() ?? new SearchToolkitFacetAttribute() 
                    ))
                    .ToList();

        private static bool IsFacet(PropertyInfo x) =>
            (x.GetCustomAttribute<SearchableFieldAttribute>() ?? x.GetCustomAttribute<SimpleFieldAttribute>())?.IsFacetable ?? false;

        static FacetManager()
        {
            props.ForEach(p =>
            {
                var attr = p.Property.GetCustomAttribute<SearchableFieldAttribute>() ?? p.Property.GetCustomAttribute<SimpleFieldAttribute>();
                if (attr == null) throw new InvalidOperationException($"Property {p.Property.Name} of {typeof(T).FullName} has a [Facet] attribute but no [SearchableField] or [SimpleField] attribute");
                if (!attr.IsFacetable) throw new InvalidOperationException($"Property {p.Property.Name} of {typeof(T).FullName} has a [Facet] attribute but the field attribute has IsFacetable=false");
            });
        }

        /// <summary>
        /// Returns a model of the facets in an initial search result. Those
        /// facets can be used to further navigate the result set by using <see cref="FacetSet.BuildFacetQuery"/>
        /// or <see cref="BuildFacetFilter"/> after allowing the user to select some facet values.
        /// </summary>
        public static IList<FacetSet> GetFacetsModel(SearchResults<T> initialQuery) =>
            initialQuery?.Facets?.Select(x =>
            {
                (var prop, var attr) = props.FirstOrDefault(p => p.Property.GetSearchFieldName() == x.Key);
                var displayAttr = prop.GetCustomAttribute<SearchToolkitDisplayAttribute>();
                var facetSet = new FacetSet();
                facetSet.FacetName = x.Key;
                facetSet.DisplayName = prop.GetDisplayName();
                facetSet.FacetType = attr?.FacetType ?? FacetType.Value;
                facetSet.NumberFormat = displayAttr?.NumberFormat;
                facetSet.DateTimeFormat = displayAttr?.DateTimeFormat ?? DateTimeDisplayFormat.DateTime;
                facetSet.ValueType = prop.PropertyType.GetValueType();

                facetSet.IsCollection = prop == null
                                            ? false
                                            : prop.PropertyType != typeof(string) &&
                                              typeof(IEnumerable).IsAssignableFrom(prop.PropertyType);

                var facetResult = x.Value.FirstOrDefault();
                var isInterval = attr?.FacetType == FacetType.Range && (facetResult == null ? attr.FacetSpecType == FacetSpecType.Interval : facetResult.FacetType == FacetType.Value);
                var isValues = attr?.FacetType == FacetType.Range && (facetResult == null ? attr.FacetSpecType == FacetSpecType.Values : facetResult.FacetType == FacetType.Range);
                facetSet.FacetRangeType = isInterval
                                        ? FacetRangeType.Interval
                                        : isValues
                                            ? FacetRangeType.Range
                                            : FacetRangeType.Value;

                facetSet.Values = x.Value.Select(y => new FacetValue()
                {
                    Value = isValues ? y.From : y.Value,
                    ValueTo = isValues ? y.To : (isInterval ? attr.AddInterval(y.Value) : null),
                    Selected = false,
                    Count = y.Count ?? 0,
                    FilteredCount = y.Count ?? 0
                }).ToList();
                return facetSet;
            }).ToList();

        public static IList<string> GetFacetDeclarations() =>
            props.Where(x => x.FacetAttribute.Valid())
                 .Select(x => x.FacetAttribute.GetFacetDeclaration(x.Property))
                 .ToList();

        public static IList<string> GetFacetDeclarations(params FacetSpecification<T>[] facetRanges) =>
            props.Where(x => x.FacetAttribute.Valid(facetRanges))
                 .Select(x => x.FacetAttribute.GetFacetDeclaration(x.Property, facetRanges?.FirstOrDefault(fr => fr.Property == x.Property).GetValue()))
                 .ToList();

        public static string BuildFacetFilter(IList<FacetSet> facets, LogicalOperator facetOp, LogicalOperator valueOp) =>
            facets == null
                ? string.Empty
                : facets.Count(x => x.Selected) < 2
                    ? facets.FirstOrDefault(x => x.Selected)?.BuildFacetQuery(valueOp) ?? string.Empty
                    : $"({string.Join($") {facetOp.ToString().ToLower()} (", facets.Where(x => x.Selected).Select(x => x.BuildFacetQuery(valueOp)))})";
    }
}
