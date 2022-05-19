using Marsman.Reflekt;
using System;
using System.Collections;
using System.Linq.Expressions;

namespace Marsman.AzureSearchToolkit
{
    public class RangeSpecification<T> : FacetSpecification<T>
    {
        internal IEnumerable RangeMarkers { get; }
        internal RangeSpecification(Expression<Func<T, double>> selector, double[] rangeMarkers)
            : base(Reflekt<T>.Property(selector))
        {
            RangeMarkers = rangeMarkers;
        }
        internal RangeSpecification(Expression<Func<T, DateTimeOffset>> selector, DateTimeOffset[] rangeMarkers)
            : base(Reflekt<T>.Property(selector))
        {
            RangeMarkers = rangeMarkers;
        }

        internal override object GetValue() => RangeMarkers;
    }
    public static class RangeSpecification
    {
        public static RangeSpecification<T> Create<T>(Expression<Func<T, double>> selector, double[] rangeMarkers) =>
            new RangeSpecification<T>(selector, rangeMarkers);
        public static RangeSpecification<T> Create<T>(Expression<Func<T, DateTimeOffset>> selector, DateTimeOffset[] rangeMarkers) =>
            new RangeSpecification<T>(selector, rangeMarkers);
    }
}
