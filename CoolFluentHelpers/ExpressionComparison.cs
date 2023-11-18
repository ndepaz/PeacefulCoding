using System.Linq.Expressions;

namespace CoolFluentHelpers
{
    internal class ExpressionComparison<T, TValue, TNestedValue> : ICompareExpression<T, TValue>
    {
        public string PropertyDisplayName { get; }
        public Expression<Func<TValue, TNestedValue>> NestedPropertyExpression { get; }
        public Expression<Func<T, IEnumerable<TValue>>> EnumerablePropertyExpression { get; }
        public QueryOperation QueryOperation { get; private set; }

        private ExpressionComparison()
        {

        }

        public ExpressionComparison(string nestedDisplayName, Expression<Func<TValue, TNestedValue>> nestedPropertyExpression, Expression<Func<T, IEnumerable<TValue>>> enumerablePropertyExpression)
        {
            PropertyDisplayName = nestedDisplayName;
            NestedPropertyExpression = nestedPropertyExpression;
            EnumerablePropertyExpression = enumerablePropertyExpression;
        }

        internal static ICompareExpression<T, TValue> Create<TValue, TNestedValue, T>(
            string nestedDisplayName,
            Expression<Func<TValue, TNestedValue>> nestedPropertyExpression,
            Expression<Func<T, IEnumerable<TValue>>> enumerablePropertyExpression
            )
        {
            return new ExpressionComparison<T, TValue, TNestedValue>(nestedDisplayName, nestedPropertyExpression, enumerablePropertyExpression);
        }

        public ICompareValue<TValue, TNestedValue> Compare(QueryOperation queryOperation)
        {
            QueryOperation = queryOperation;


        }

        public ICompareValue<TValue> CompareWithDefault()
        {
            
        }

        public ICompareExpression<TValue> OnlyIf(bool condition)
        {
            
        }
    }

    public interface ICompareValue<TValue, TNestedValue>
    {
    }
}