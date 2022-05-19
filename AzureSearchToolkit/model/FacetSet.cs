using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class FacetSet : IFormattable
    {
        public string FacetName { get; set; }

        public string DisplayName { get; set; }

        public IList<FacetValue> Values { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FacetType FacetType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ValueType ValueType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FacetRangeType FacetRangeType { get; set; }

        /// <summary>
        /// Accepts any string that contains "0.000...0" where the number
        /// of zeroes is the number of decimal places wanted. e.g. "$0.00 USD" would
        /// render "$3.50 USD" if we gave it 3.504398433.
        /// <para>
        /// </para>
        /// <para>
        /// ...well, it was about that time I realised the format string was
        /// actually a 50ft tall creature from the paleozoeic era so I said
        /// get outta hear ya god damn Loch Ness monster I ain't giving you 
        /// no 3.504398433</para>
        /// </summary>
        public string NumberFormat { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DateTimeDisplayFormat DateTimeFormat { get; set; }

        public bool IsCollection { get; set; }

        internal bool Selected => Values?.Any(f => f.Selected) == true;

        public string BuildFacetQuery(LogicalOperator op)
        {
            if (FacetType == FacetType.Range)
            {
                return string.Join($" {op.ToString().ToLower()} ",
                      Values.Where(x => x.Selected)
                            .Select(x =>
                            {
                                if (x.Value == null)
                                    return SearchFilter.Create(
                                        $"([FacetName] lt {x.ValueTo})").Replace("[FacetName]", FacetName);
                                if (x.ValueTo == null)
                                    return SearchFilter.Create(
                                        $"([FacetName] ge {x.Value})").Replace("[FacetName]", FacetName);
                                return SearchFilter.Create(
                                    $"([FacetName] ge {x.Value} and [FacetName] lt {x.ValueTo})").Replace("[FacetName]", FacetName);
                            }));
            }

            if (IsCollection)
                return string.Join($" {op.ToString().ToLower()} ",
                      Values.Where(x => x.Selected)
                            .Select(x => SearchFilter.Create(
                                $"([FacetName]/any(t: t eq {x.Value}))").Replace("[FacetName]", FacetName)));

            return string.Join($" {op.ToString().ToLower()} ",
                  Values.Where(x => x.Selected)
                        .Select(x => SearchFilter.Create(
                            $"([FacetName] eq {x.Value})").Replace("[FacetName]", FacetName)));
        }
    }
}
