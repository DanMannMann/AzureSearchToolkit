using System.Reflection;

namespace Marsman.AzureSearchToolkit
{
    public abstract class FacetSpecification<T>
    {
        protected FacetSpecification(PropertyInfo property)
        {
            Property = property;
        }

        internal abstract object GetValue();
        internal PropertyInfo Property { get; }
    }
}
