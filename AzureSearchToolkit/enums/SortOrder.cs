using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum SortOrder
    {
        Descending = -1,
        None = 0,
        Ascending = 1
    }
}
