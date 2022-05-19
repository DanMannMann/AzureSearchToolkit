using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class User
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsSortable = true)]
        [SearchToolkitDisplay(DisplayName = "First Name")]
        public string FirstName { get; set; }

        [SearchableField(IsSortable = true)]
        [SearchToolkitDisplay(DisplayName = "Last Name")]
        public string LastName { get; set; }

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnLucene)]
        public string Description { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Role { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] Tags { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool Enabled { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [SearchToolkitFacet(RuntimeSpecType.DateTimeValues)]
        public DateTimeOffset? LastModifiedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [SearchToolkitFacet(TimeInterval.Month)]
        [SearchToolkitDisplay(DateTimeFormat = DateTimeDisplayFormat.Date, DisplayName = "Joined Date")]
        public DateTimeOffset? JoinedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [SearchToolkitFacet(10000,50000,100000,200000,500000)]
        [SearchToolkitDisplay(NumberFormat = "$0.00")]
        public double? Balance { get; set; }
    }
}
