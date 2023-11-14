using CSharpFunctionalExtensions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CoolFluentHelpers
{
    public class ExpressionMaker
    {
        public static ExpressionBuilderHelper<Model> For<Model>()
        {
            return new ExpressionBuilderHelper<Model>();
        }
    }

    public class ExpressionBuilderHelper<T> : ExpressionMaker
    {
        internal ExpressionBuilderHelper()
        {

        }
        /// <summary>
        /// It has the same behaviour as WithProperty just different name to sound a bit more natural
        /// Use WithProperty instead.
        /// </summary>
        /// <typeparam name="ModelPropValue"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public ExpressionBuilderProperty<T, ModelPropValue> On<ModelPropValue>(Expression<Func<T, ModelPropValue>> property)
        {
            return new ExpressionBuilderProperty<T, ModelPropValue>(property);
        }
        /// <summary>
        /// Used to start building the expression for a given property.
        /// </summary>
        /// <typeparam name="ModelPropValue"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public ExpressionBuilderProperty<T, ModelPropValue> WithProperty<ModelPropValue>(Expression<Func<T, ModelPropValue>> property)
        {
            return new ExpressionBuilderProperty<T, ModelPropValue>(property);
        }

        public class ExpressionBuilderProperty<M, V>
        {
            internal readonly Expression<Func<M, V>> PropertyExpression;

            public ExpressionBuilderProperty(Expression<Func<M, V>> property)
            {
                PropertyExpression = property;
            }

            public ExpressionBuilderOperatorValue<M, V> When(QueryOperation operation)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, operation);
            }

            public Expression<Func<M, bool>> Equals(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.Equals).Value(value);
            }

            public Expression<Func<M, bool>> LessThan(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.LessThan).Value(value);
            }

            public Expression<Func<M, bool>> LessThanOrEqual(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.LessThanOrEqual).Value(value);
            }

            public Expression<Func<M, bool>> GreaterThan(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.GreaterThan).Value(value);
            }

            public Expression<Func<M, bool>> GreaterThanOrEqual(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.GreaterThanOrEqual).Value(value);
            }

            public Expression<Func<M, bool>> StartsWith(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.StartsWith).Value(value);
            }

            public Expression<Func<M, bool>> EndsWith(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.EndsWith).Value(value);
            }

            public Expression<Func<M, bool>> Contains(V value)
            {
                return new ExpressionBuilderOperatorValue<M, V>(this, QueryOperation.Contains).Value(value);
            }

            public Expression<Func<M, bool>> In(params V[] values)
            {
                var parameter = Expression.Parameter(typeof(M), "x");
                var memberExpression = Expression.PropertyOrField(parameter, PropertyExpression.GetPropertyPath());
                var someValue = Expression.Constant(values, typeof(V[]));
                var containsMethodExp = Expression.Call(memberExpression, typeof(Enumerable).GetMethod("Contains", new[] { typeof(IEnumerable<V>), typeof(V) }), someValue);

                return Expression.Lambda<Func<M, bool>>(containsMethodExp, parameter);
            }

        }

        public class ExpressionBuilderOperatorValue<M, V>
        {
            private readonly ExpressionBuilderProperty<M, V> _predicateBuilderProperty;
            private readonly QueryOperation Operation;

            internal ExpressionBuilderOperatorValue(ExpressionBuilderProperty<M, V> predicateBuilderProperty, QueryOperation operation)
            {
                _predicateBuilderProperty = predicateBuilderProperty;
                Operation = operation;
            }

            public Expression<Func<M, bool>> Value(V input)
            {
                return ExpressionBuilder.BuildPredicate(_predicateBuilderProperty.PropertyExpression, this.Operation, input);
            }

        }
    }
    internal static class PredicateBuilder
    {
        public static Expression<Func<Model, bool>> BuildPredicate<Model, ModelPropValue>(
            Expression<Func<Model, ModelPropValue>> propertySelector,
            ExpressionType operation,
            ModelPropValue value)
        {
            var propertyName = propertySelector.GetPropertyPath();
            var parameterExp = propertySelector.Parameters[0];
            var propertyExp = Expression.Property(parameterExp, propertyName);

            // Create the param with a value

            Expression<Func<ModelPropValue>> valueSelector = () => value;

            var valueBody = valueSelector.Body;

            var convertedValue = Expression.Convert(valueBody, propertyExp.Type);

            // Build the binary expression based on the specified operation
            BinaryExpression binaryExp = Expression.MakeBinary(operation, propertyExp, convertedValue);

            // Construct the lambda expression and return it
            return Expression.Lambda<Func<Model, bool>>(binaryExp, parameterExp);
        }
    }

    internal static class ExpressionBuilder
    {
        internal static ExpressionType GetOperation(QueryOperation operation)
        {
            switch (operation)
            {
                case QueryOperation.Equals:
                    return ExpressionType.Equal;
                case QueryOperation.NotEqual:
                    return ExpressionType.NotEqual;
                case QueryOperation.GreaterThan:
                    return ExpressionType.GreaterThan;
                case QueryOperation.LessThan:
                    return ExpressionType.LessThan;
                case QueryOperation.GreaterThanOrEqual:
                    return ExpressionType.GreaterThanOrEqual;
                case QueryOperation.LessThanOrEqual:
                    return ExpressionType.LessThanOrEqual;
                default:
                    throw new NotSupportedException($"The query operation '{operation}' is not supported.");
            }
        }

        internal static Expression<Func<Model, bool>> BuildPredicate<Model, ModelPropValue>(Expression<Func<Model, ModelPropValue>> propertySelector, QueryOperation operation, ModelPropValue value)
        {
            var propertyName = propertySelector.GetPropertyPath();

            var parameter = Expression.Parameter(typeof(Model), "x");
            var memberExpression = GetNestedProperty(parameter, propertyName);
            var resultMethod = GetMethod(memberExpression.Type, operation);

            if (resultMethod.IsFailure)
            {
                return PredicateBuilder.BuildPredicate(propertySelector, GetOperation(operation), value);
            }

            Expression<Func<ModelPropValue>> valueSelector = () => value;

            var valueBody = valueSelector.Body;

            var convertedValue = Expression.Convert(valueBody, memberExpression.Type);

            var containsMethodExp = Expression.Call(memberExpression, resultMethod.Value, convertedValue);

            return Expression.Lambda<Func<Model, bool>>(containsMethodExp, parameter);
        }

        private static MemberExpression GetNestedProperty(Expression expression, string propertyName)
        {
            foreach (var property in propertyName.Split('.'))
            {
                expression = Expression.PropertyOrField(expression, property);
            }
            return (MemberExpression)expression;
        }

        private static Result<MethodInfo> GetMethod(Type propertyType, QueryOperation operation)
        {
            if (propertyType != typeof(string))
            {
                return Result.Failure<MethodInfo>($"Operation {operation} is not supported");
            }

            MethodInfo method = operation switch
            {
                QueryOperation.StartsWith => propertyType.GetMethod("StartsWith", new[] { propertyType }),
                QueryOperation.EndsWith => propertyType.GetMethod("EndsWith", new[] { propertyType }),
                QueryOperation.Contains => propertyType.GetMethod("Contains", new[] { propertyType }),
                QueryOperation.Equals => propertyType.GetMethod("Equals", new[] { propertyType }),
                _ => null
            };

            return Result.SuccessIf(method is not null, method, $"Operation {operation} is not supported");
        }
    }
    internal static class ExpressionExtension
    {
        public static MemberExpression GetMemberExpression(Expression expression)
        {
            if (expression is MemberExpression)
            {
                return (MemberExpression)expression;
            }

            if (expression is LambdaExpression)
            {
                var lambdaExpression = expression as LambdaExpression;
                if (lambdaExpression.Body is MemberExpression)
                {
                    return (MemberExpression)lambdaExpression.Body;
                }
                else if (lambdaExpression.Body is UnaryExpression)
                {
                    return ((MemberExpression)((UnaryExpression)lambdaExpression.Body).Operand);
                }
            }
            return null;
        }

        private static string GetPropertyPathCore(Expression expr)
        {
            var path = new StringBuilder();
            MemberExpression memberExpression = GetMemberExpression(expr);
            do
            {
                if (path.Length > 0)
                {
                    path.Insert(0, ".");
                }
                path.Insert(0, memberExpression.Member.Name);
                memberExpression = GetMemberExpression(memberExpression.Expression);
            }
            while (memberExpression != null);
            return path.ToString();
        }

        public static object GetPropertyValue(this object obj, string propertyPath)
        {
            object propertyValue = null;
            if (propertyPath.IndexOf(".") < 0)
            {
                var objType = obj.GetType();
                propertyValue = objType.GetProperty(propertyPath).GetValue(obj, null);
                return propertyValue;
            }
            var properties = propertyPath.Split('.').ToList();
            var midPropertyValue = obj;
            while (properties.Count > 0)
            {
                var propertyName = properties.First();
                properties.Remove(propertyName);
                propertyValue = midPropertyValue.GetPropertyValue(propertyName);
                midPropertyValue = propertyValue;
            }
            return propertyValue;
        }

        public static string GetPropertyPath<TObj, V>(this Expression<Func<TObj, V>> expr)
        {
            return GetPropertyPathCore(expr);
        }
    }
}
