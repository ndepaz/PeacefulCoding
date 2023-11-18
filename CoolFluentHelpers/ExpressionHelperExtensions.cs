using CSharpFunctionalExtensions;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

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
        private static MethodBase GetGenericMethod(Type type, string name, Type[] typeArgs, Type[] argTypes, BindingFlags flags)
        {
            int typeArity = typeArgs.Length;
            var methods = type.GetMethods()
                .Where(m => m.Name == name)
                .Where(m => m.GetGenericArguments().Length == typeArity)
                .Select(m => m.MakeGenericMethod(typeArgs));

            return Type.DefaultBinder.SelectMethod(flags, methods.ToArray(), argTypes, null);
        }
        private static bool IsIEnumerable(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static Type GetIEnumerableImpl(Type type)
        {
            // Get IEnumerable implementation. Either type is IEnumerable<T> for some T, 
            // or it implements IEnumerable<T> for some T. We need to find the interface.
            if (IsIEnumerable(type))
                return type;
            Type[] t = type.FindInterfaces((m, o) => IsIEnumerable(m), null);
            Debug.Assert(t.Length == 1);
            return t[0];
        }

        public static Expression CallMethod(MethodInfo methodToUse, Expression collection, Delegate predicate)
        {
            Type cType = GetIEnumerableImpl(collection.Type);
            collection = Expression.Convert(collection, cType);

            Type elemType = cType.GetGenericArguments()[0];
            Type predType = typeof(Func<,>).MakeGenericType(elemType, typeof(bool));

            // Enumerable.Any<T>(IEnumerable<T>, Func<T,bool>)
            return Expression.Call(
                    methodToUse,
                    collection,
                    Expression.Constant(predicate));
        }
    }
}
