using Azure.Search.Documents.Models;
using System;
using System.Text.Json.Serialization;

namespace Marsman.AzureSearchToolkit
{
    public class FacetValue
    {
        private object fromValue;
        private object toValue;

        public object Value { get => fromValue; set => fromValue = CoerceType(value); }

        public object ValueTo { get => toValue; set => toValue = CoerceType(value); }

        public bool Selected { get; set; }

        public long Count { get; set; }
        public long FilteredCount { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is FacetValue fv)) return false;
            var valEq = (Value?.Equals(fv.Value) ?? fv.Value == null);
            var valToEq = (ValueTo?.Equals(fv.ValueTo) ?? fv.ValueTo == null);
            return valEq && valToEq;
        }

        public static bool operator ==(FacetValue a, FacetValue b)
        {
            return a is null && b is null
                       ? true
                       : a is null
                           ? false
                           : a.Equals(b);
        }

        public static bool operator !=(FacetValue a, FacetValue b)
        {
            return a is null && b is null
                       ? false
                       : a is null
                           ? true
                           : !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, ValueTo);
        }

        private static object CoerceType(object value)
        {
            return value?.IsNumericType() == true
                                            ? Convert.ToDouble(value)
                                            : value is DateTime dt
                                                ? new DateTimeOffset(dt)
                                                : value; // otherwise it's either already a datetimeoffset, or its so wrong we can't help
        }
    }
}
