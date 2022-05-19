using Azure.Search.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public abstract class FieldFilter
    {
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FilterOperation Operation { get; set; }
        public bool IsCollection { get; set; }
        internal bool IsUsed => GetValue() != null && !(GetValue() is string sr && string.IsNullOrWhiteSpace(sr)) && (GetValueTo() != null || Operation != FilterOperation.Between);

        internal abstract object GetValue();
        internal virtual object GetValueTo() => GetValue();
        internal virtual string GetFilter()
        {
            var val = GetValue();
            var val2 = GetValueTo();
            if (val == null || val is string sr && string.IsNullOrWhiteSpace(sr)) return string.Empty;
            if (val2 != null && val is IComparable ic)
            {
                try
                {
                    if (ic.CompareTo(val2) > 0) // val is greater than val2, swap them
                    {
                        (val2, val) = (val, val2);
                    }
                }
                catch { } // no worries, they couldn't be compared so leave them as they are
            }
            if (IsCollection)
            {
                return GetCollectionFilter(val, val2);
            }
            else
            {
                return GetScalarFilter(val, val2);
            }
        }

        private string GetCollectionFilter(object val, object val2)
        {
            return Operation switch
            {
                FilterOperation.GreaterThan => 
                    SearchFilter.Create($"[FieldName]/any(t: t gt {val})").Replace("[FieldName]", FieldName),

                FilterOperation.LessThan => 
                    SearchFilter.Create($"[FieldName]/any(t: t lt {val})").Replace("[FieldName]", FieldName),

                FilterOperation.GreaterThanOrEqual =>
                    SearchFilter.Create($"[FieldName]/any(t: t ge {val})").Replace("[FieldName]", FieldName),

                FilterOperation.LessThanOrEqual => 
                    SearchFilter.Create($"[FieldName]/any(t: t le {val})").Replace("[FieldName]", FieldName),

                FilterOperation.Between => 
                    SearchFilter.Create($"[FieldName]/any(t: t ge {val} and t lt {val2})").Replace("[FieldName]", FieldName),

                _ => SearchFilter.Create($"[FieldName]/any(t: t eq {val})").Replace("[FieldName]", FieldName),
            };
        }

        private string GetScalarFilter(object val, object val2)
        {
            return Operation switch
            {
                FilterOperation.GreaterThan => 
                    SearchFilter.Create($"[FieldName] gt {val}").Replace("[FieldName]", FieldName),

                FilterOperation.LessThan => 
                    SearchFilter.Create($"[FieldName] lt {val}").Replace("[FieldName]", FieldName),

                FilterOperation.GreaterThanOrEqual =>   
                    SearchFilter.Create($"[FieldName] ge {val}").Replace("[FieldName]", FieldName),

                FilterOperation.LessThanOrEqual => 
                    SearchFilter.Create($"[FieldName] le {val}").Replace("[FieldName]", FieldName),

                FilterOperation.Between =>
                    SearchFilter.Create($"[FieldName] ge {val} and [FieldName] lt {val2}").Replace("[FieldName]", FieldName),

                _ => SearchFilter.Create($"[FieldName] eq {val}").Replace("[FieldName]", FieldName),
            };
        }
    }

    public abstract class FieldFilter<T> : FieldFilter
    {
        public T Value { get; set; }

        internal override object GetValue()
        {
            return Value;
        }
    }
    public abstract class RangeFieldFilter<T> : FieldFilter<T>
    {
        public T ValueTo { get; set; }
        internal override object GetValueTo() => ValueTo;
    }
    public class NumericFieldFilter : RangeFieldFilter<double?> { }
    public class BoolFieldFilter : FieldFilter<bool?> { }
    public class DateTimeOffsetFieldFilter : RangeFieldFilter<DateTimeOffset?> { }
    public class StringFieldFilter : FieldFilter<string?> { }
}
