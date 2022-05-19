using Azure.Search.Documents.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Marsman.AzureSearchToolkit
{
    public enum FacetSpecType
    {
        Interval,
        Values
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SearchToolkitFacetAttribute : Attribute
    {
        public SearchToolkitFacetAttribute()
        {
            FacetType = FacetType.Value;
        }

        public SearchToolkitFacetAttribute(double interval)
        {
            NumericInterval = interval;
            FacetType = FacetType.Range;
            RangeType = RangeType.Numeric;
            FacetSpecType = FacetSpecType.Interval;
        }

        public SearchToolkitFacetAttribute(params double[] rangeMarkers)
        {
            NumericRangeMarkers = rangeMarkers;
            FacetType = FacetType.Range;
            RangeType = RangeType.Numeric;
            FacetSpecType = FacetSpecType.Values;
        }

        public SearchToolkitFacetAttribute(TimeInterval interval)
        {
            TimeInterval = interval;
            FacetType = FacetType.Range;
            RangeType = RangeType.DateTime;
            FacetSpecType = FacetSpecType.Interval;
        }

        public SearchToolkitFacetAttribute(params DateTimeOffset[] rangeMarkers)
        {
            TimeRangeMarkers = rangeMarkers;
            FacetType = FacetType.Range;
            RangeType = RangeType.DateTime;
            FacetSpecType = FacetSpecType.Values;
        }

        public SearchToolkitFacetAttribute(RuntimeSpecType runtimeSpecType)
        {
            switch (runtimeSpecType)
            {
                case RuntimeSpecType.NumericValues:
                    NumericRangeMarkers = new double[0];
                    FacetSpecType = FacetSpecType.Values;
                    FacetType = FacetType.Range;
                    RangeType = RangeType.Numeric;
                    break;
                case RuntimeSpecType.DateTimeValues:
                    TimeRangeMarkers = new DateTimeOffset[0];
                    FacetSpecType = FacetSpecType.Values;
                    FacetType = FacetType.Range;
                    RangeType = RangeType.DateTime;
                    break;
                case RuntimeSpecType.NumericInterval:
                    FacetSpecType = FacetSpecType.Interval;
                    FacetType = FacetType.Range;
                    RangeType = RangeType.Numeric;
                    break;
                case RuntimeSpecType.DateTimeInterval:
                    FacetSpecType = FacetSpecType.Interval;
                    FacetType = FacetType.Range;
                    RangeType = RangeType.DateTime;
                    break;
            }
        }

        internal object SubtractInterval(object input)
        {
            if (TimeInterval.HasValue)
            {
                if (input is not DateTimeOffset dto) throw new InvalidOperationException("input is not a DateTimeOffset");
                return TimeInterval.Value switch
                {
                    AzureSearchToolkit.TimeInterval.Minute => dto.AddMinutes(-1),
                    AzureSearchToolkit.TimeInterval.Hour => dto.AddHours(-1),
                    AzureSearchToolkit.TimeInterval.Week => dto.AddDays(-7),
                    AzureSearchToolkit.TimeInterval.Month => dto.AddMonths(-1),
                    AzureSearchToolkit.TimeInterval.Quarter => dto.AddMinutes(-3),
                    AzureSearchToolkit.TimeInterval.Year => dto.AddYears(-1),
                    _ => dto.AddDays(-1),
                };
            }
            if (NumericInterval.HasValue)
            {
                if (input is not double dbl) throw new InvalidOperationException("input is not a double");
                return dbl - NumericInterval.Value;
            }
            throw new InvalidOperationException("Not an interval facet");
        }

        internal object AddInterval(object input)
        {
            if (TimeInterval.HasValue)
            {
                if (input is not DateTimeOffset dto) throw new InvalidOperationException("input is not a DateTimeOffset");
                return TimeInterval.Value switch
                {
                    AzureSearchToolkit.TimeInterval.Minute => dto.AddMinutes(1),
                    AzureSearchToolkit.TimeInterval.Hour => dto.AddHours(1),
                    AzureSearchToolkit.TimeInterval.Week => dto.AddDays(7),
                    AzureSearchToolkit.TimeInterval.Month => dto.AddMonths(1),
                    AzureSearchToolkit.TimeInterval.Quarter => dto.AddMinutes(3),
                    AzureSearchToolkit.TimeInterval.Year => dto.AddYears(1),
                    _ => dto.AddDays(1),
                };
            }
            if (NumericInterval.HasValue)
            {
                if (input is not double dbl) throw new InvalidOperationException("input is not a double");
                return dbl + NumericInterval.Value;
            }
            throw new InvalidOperationException("Not an interval facet");
        }

        public FacetSpecType FacetSpecType { get; }
        public RangeType RangeType { get; }
        public FacetType FacetType { get; }
        public double? NumericInterval { get; }
        public double[] NumericRangeMarkers { get; }
        public TimeInterval? TimeInterval { get; }
        public DateTimeOffset[] TimeRangeMarkers { get; }
        /// <summary>
        /// Default is 10 (same as Azure service default)
        /// </summary>
        public int Count { get; set; } = 10;

        internal bool Valid(object rangeValue = null)
        {
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.Numeric && (NumericInterval != null || rangeValue is double) => true,
                FacetType.Range when RangeType == RangeType.Numeric && (NumericRangeMarkers?.Any() == true || rangeValue is IEnumerable<double>) => true,
                FacetType.Range when RangeType == RangeType.DateTime && (TimeInterval != null || rangeValue is DateTimeOffset) => true,
                FacetType.Range when RangeType == RangeType.DateTime && (TimeRangeMarkers?.Any() == true || rangeValue is IEnumerable<DateTimeOffset>) => true,
                FacetType.Range => false, // range with no range details supplied
                _ => true // not a range
            };
        }

        internal string GetFacetDeclaration(PropertyInfo property)
        {
            var facetName = property.GetSearchFieldName();
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.Numeric && NumericInterval != null => $"{facetName},interval:{NumericInterval}",
                FacetType.Range when RangeType == RangeType.Numeric && NumericRangeMarkers != null => $"{facetName},values:{string.Join("|", NumericRangeMarkers)}",
                FacetType.Range when RangeType == RangeType.DateTime && TimeInterval != null => $"{facetName},interval:{TimeInterval.Value.ToString().ToLower()}",
                FacetType.Range when RangeType == RangeType.DateTime && TimeRangeMarkers != null => $"{facetName},values:{string.Join("|", TimeRangeMarkers)}",
                _ => $"{facetName},count:{Count}"
            };
        }

        internal string GetFacetDeclaration(PropertyInfo property, object rangeValue) => rangeValue switch
        {
            null => GetFacetDeclaration(property),
            double dblInterval => GetFacetDeclaration(property, dblInterval),
            TimeInterval timeInterval => GetFacetDeclaration(property, timeInterval),
            IEnumerable<DateTimeOffset> markers => GetFacetDeclaration(property, markers),
            IEnumerable<double> markers => GetFacetDeclaration(property, markers),
            _ => throw new InvalidOperationException("unknown range value type")
        };

        private string GetFacetDeclaration(PropertyInfo property, TimeInterval timeInterval)
        {
            var facetName = property.GetSearchFieldName();
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.DateTime => $"{facetName},interval:{timeInterval}",
                _ => throw new InvalidOperationException("Not a datetime-type range marker facet - timeInterval overload should only be called for an appropriate facet")
            };
        }

        private string GetFacetDeclaration(PropertyInfo property, double numericInterval)
        {
            var facetName = property.GetSearchFieldName();
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.Numeric => $"{facetName},interval:{numericInterval}",
                _ => throw new InvalidOperationException("Not a numeric-type range marker facet - numericInterval overload should only be called for an appropriate facet")
            };
        }

        private string GetFacetDeclaration(PropertyInfo property, IEnumerable<DateTimeOffset> timeRangeMarkers)
        {
            var facetName = property.GetSearchFieldName();
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.DateTime && timeRangeMarkers != null => $"{facetName},values:{string.Join("|", timeRangeMarkers)}",
                _ => throw new InvalidOperationException("Not a datetime-type range marker facet - timeRangeMarkers overload should only be called for an appropriate facet")
            };
        }

        private string GetFacetDeclaration(PropertyInfo property, IEnumerable<double> numericRangeMarkers)
        {
            var facetName = property.GetSearchFieldName();
            return FacetType switch
            {
                FacetType.Range when RangeType == RangeType.Numeric && numericRangeMarkers != null => $"{facetName},values:{string.Join("|", numericRangeMarkers)}",
                _ => throw new InvalidOperationException("Not a numeric-type range marker facet - numericRangeMarkers overload should only be called for an appropriate facet")
            };
        }
    }
}
