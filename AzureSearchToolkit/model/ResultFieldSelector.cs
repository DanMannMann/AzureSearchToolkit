using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class ResultFieldSelector : IFormattable
    {
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; } = true;

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

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ValueType ValueType { get; set; }
    }

    public interface IFormattable
    {
        public string NumberFormat { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DateTimeDisplayFormat DateTimeFormat { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ValueType ValueType { get; set; }
    }
}
