using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CollectionMatchType
    {
        MatchAllInputValues,
        MatchAnyInputValue
    }
}
