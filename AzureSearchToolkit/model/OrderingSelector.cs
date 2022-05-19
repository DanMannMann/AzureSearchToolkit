using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class OrderingSelector
    {
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SortOrder Order { get; set; } = SortOrder.None;
    }
}
