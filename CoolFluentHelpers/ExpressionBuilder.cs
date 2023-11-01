using CSharpFunctionalExtensions;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoolFluentHelpers
{
    public enum QueryOperation
    {

        StartsWith,
        EndsWith,
        Contains,
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }
    public enum QueryString
    {

        StartsWith,
        EndsWith,
        Contains,
        Equals,
    }

    public enum QueryNumber
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum QueryDate
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum QueryBool
    {
        Equals,
    }

    public class PropertyDisplayNameAndType : IEquatable<PropertyDisplayNameAndType>
    {
        protected PropertyDisplayNameAndType(string displayName, Type type)
        {
            DisplayName = displayName;
            Type = type;
        }

        public string DisplayName { get; }
        public Type Type { get; }

        public static PropertyDisplayNameAndType Create(string displayName, Type type)
        {
            return new PropertyDisplayNameAndType(displayName, type);
        }

        //When DisplayName and Type are equal, then they are equal
        public bool Equals(PropertyDisplayNameAndType other)
        {
            if (other is null)
                return false;

            return DisplayName == other.DisplayName && Type == other.Type;
        }

    }

    internal class PropertyDisplayObjectAndType : PropertyDisplayNameAndType,IEquatable<PropertyDisplayObjectAndType>
    {
        private PropertyDisplayObjectAndType(object obj, Type type, string displayName) : base(displayName, type)
        {
            Obj = obj;
        }
        public object Obj { get; }

        public static PropertyDisplayObjectAndType Create(object obj, Type type, string displayName)
        {
            return new PropertyDisplayObjectAndType(obj, type, displayName);
        }

        public bool Equals(PropertyDisplayObjectAndType other)
        {
            if (other is null)
                return false;

            return DisplayName == other.DisplayName && Obj == other.Obj && Type == other.Type;
        }
    }

    public class ExpressionBuilder<T>
    {
        private List<PropertyDisplayObjectAndType> _expressionComparison = new();

        public string DisplayName;

        private ExpressionBuilder()
        {

        }

        public static ExpressionBuilder<T> Create()
        {
            return new ExpressionBuilder<T>();
        }

        public ExpressionComparison<T, TValue> ForProperty<TValue>(Expression<Func<T, TValue>> propertyExpression, string displayName)
        {
            var expressionComparison = ExpressionComparison<T, TValue>.Create(propertyExpression);

            _expressionComparison.Add(PropertyDisplayObjectAndType.Create(expressionComparison, expressionComparison.GetType(), displayName));

            return expressionComparison;
        }

        public IReadOnlyList<ExpressionComparison<T, TValue>> GetProperties<TValue>(string displayName)
        {
            var displayObjectAndTypes = _expressionComparison.Where(x => x.DisplayName == displayName);

            return displayObjectAndTypes.Select(x => (ExpressionComparison<T, TValue>)x.Obj).ToList();
        }

        public IReadOnlyList<object> GetProperties(string displayName)
        {
            var displayObjectAndTypes = _expressionComparison.Where(x => x.DisplayName == displayName);

            return displayObjectAndTypes.Select(x => x.Obj).ToList();
        }

        public IReadOnlyList<ExpressionComparison<T, TValue>> GetProperties<TValue>(string displayName, Expression<Func<T, TValue>> propertyExpression)
        {
            var displayObjectAndTypes = _expressionComparison.Where(x => x.Type == typeof(ExpressionComparison<T, TValue>) && x.DisplayName == displayName);

            return displayObjectAndTypes.Select(x => (ExpressionComparison<T, TValue>)x.Obj).ToList();
        }

        public IReadOnlyList<ExpressionComparison<T, TValue>> GetProperties<TValue>(string displayname, TValue value)
        {
            var displayObjectAndTypes = _expressionComparison.Where(x => x.DisplayName == displayname && x.Type == typeof(ExpressionComparison<T, TValue>));

            return displayObjectAndTypes.Select(x => (ExpressionComparison<T, TValue>)x.Obj).ToList();
        }
    }

    public class ExpressionComparison<T, TValue>
    {
        internal Expression<Func<T, TValue>> PropertyExpression { get; }

        internal QueryOperation QueryOperation;

        private Expression<Func<T, bool>> _expression;

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression)
        {
            PropertyExpression = propertyExpression;
        }

        internal static ExpressionComparison<T, TValue> Create(Expression<Func<T, TValue>> propertyExpression)
        {
            return new ExpressionComparison<T, TValue>(propertyExpression);
        }

        public ExpressionValue<T, TValue> Compare(QueryOperation queryOperation)
        {
            QueryOperation = queryOperation;

            return ExpressionValue<T, TValue>.Create(this);
        }

        public ExpressionValue<T, TValue> Compare(AsQuery<TValue> asQuery)
        {
            QueryOperation = asQuery.Get();

            return ExpressionValue<T, TValue>.Create(this);
        }

        internal ExpressionComparison<T, TValue> SetExpression(Expression<Func<T, bool>> expression)
        {
            _expression = expression;

            return this;
        }

        public IResult<Expression<Func<T, bool>>> AsExpression()
        {
            return Result.Success(_expression);
        }
    }
    public class ExpressionValue<T, TValue>
    {
        private ExpressionValue(ExpressionComparison<T, TValue> expressionComparison)
        {
            _expressionComparison = expressionComparison;
        }

        private ExpressionComparison<T, TValue> _expressionComparison { get; }

        internal static ExpressionValue<T, TValue> Create(ExpressionComparison<T, TValue> expressionComparison)
        {
            return new ExpressionValue<T, TValue>(expressionComparison);
        }

        public ExpressionComparison<T, TValue> WithValue(TValue value)
        {
            return _expressionComparison.WithValue(value);
        }

        /// <summary>
        /// Performs a cast to TValue and uses the WithValue
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ExpressionComparison<T, TValue> WithAnyValue(object value)
        {
            return WithValue((TValue)value);
        }
    }

    internal static class ExpressionValueExt
    {
        internal static ExpressionComparison<T, TValue> WithValue<T, TValue>(this ExpressionComparison<T, TValue> expressionComparison, TValue value)
        {
            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.QueryOperation, value);

            expressionComparison.SetExpression(expression);

            return expressionComparison;
        }
    }

    internal static class ExpresssionComparisonExt
    {

        internal static List<ExpressionComparison<T, TValue>> CreateList<T, TValue>(this ExpressionComparison<T, TValue> expressionComparison)
        {
            return new List<ExpressionComparison<T, TValue>> { expressionComparison };
        }
    }

    public class AsQuery
    {
        protected AsQuery(QueryOperation queryOperation)
        {
            QueryOperation = queryOperation;
        }

        public static AsQuery<string> String(QueryString operation)
        {
            return AsQuery<string>.String<string>(operation);
        }

        public static AsQuery<TValue> Number<TValue>(QueryNumber operation) where TValue : INumber<TValue>
        {
            return AsQuery<TValue>.Numeric<TValue>(operation);
        }

        public static AsQuery<TValue?> NullableValue<TValue>(QueryNumber operation) where TValue : struct
        {
            return AsQuery<TValue?>.NullableValue<TValue>(operation);
        }

        public static AsQuery<DateTime> Date(QueryDate operation)
        {
            return AsQuery<DateTime>.Date<DateTime>(operation);
        }

        public static AsQuery<bool> Bool(QueryBool operation)
        {
            return AsQuery<bool>.Bool<bool>(operation);
        }
        private QueryOperation QueryOperation { get; }
        public QueryOperation Get()
        {
            return QueryOperation;
        }
    }
}
