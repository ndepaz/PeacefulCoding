using System.Linq.Expressions;

namespace CoolFluentHelpers
{
    public static class ExpressionHelperExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(Expression.Invoke(left, parameter), Expression.Invoke(right, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.OrElse(Expression.Invoke(left, parameter), Expression.Invoke(right, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
