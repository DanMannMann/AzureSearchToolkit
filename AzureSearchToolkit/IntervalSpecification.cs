using Marsman.Reflekt;
using System;
using System.Linq.Expressions;

namespace Marsman.AzureSearchToolkit
{
    public class IntervalSpecification<T> : FacetSpecification<T>
    {
        internal object Interval { get; }
        internal IntervalSpecification(Expression<Func<T, double>> selector, double interval)
            : base(Reflekt<T>.Property(selector))
        {
            Interval = interval;
        }
        internal IntervalSpecification(Expression<Func<T, DateTimeOffset>> selector, TimeInterval interval)
            : base(Reflekt<T>.Property(selector))
        {
            Interval = interval;
        }
        internal override object GetValue() => Interval;
    }
    public static class IntervalSpecification
    {
        public static IntervalSpecification<T> Create<T>(Expression<Func<T, double>> selector, double interval) =>
            new IntervalSpecification<T>(selector, interval);
        public static IntervalSpecification<T> Create<T>(Expression<Func<T, DateTimeOffset>> selector, TimeInterval interval) =>
            new IntervalSpecification<T>(selector, interval);
    }
}
