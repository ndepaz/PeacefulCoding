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

    public class ExpressionBuilder<T>
    {
        private ExpressionBuilder()
        {

        }

        public static ExpressionBuilder<T> Create()
        {
            return new ExpressionBuilder<T>();
        }

        public ExpressionComparison<T, TValue> ForProperty<TValue>(Expression<Func<T, TValue>> propertyExpression)
            where TValue : struct
        {
            return ExpressionComparison<T, TValue>.Create(propertyExpression);
        }

        public ExpressionComparison<T, TValue?> ForProperty<TValue>(Expression<Func<T, TValue?>> propertyExpression)
            where TValue : struct
        {
            return ExpressionComparison<T, TValue?>.Create(propertyExpression);
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

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression, QueryOperation queryOperation)
        {
            PropertyExpression = propertyExpression;
            QueryOperation = queryOperation;
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

            return ExpressionValue<T,TValue>.Create(this);
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
    }

    internal static class ExpressionValueExt
    {
        internal static ExpressionComparison<T, TValue> WithValue<T, TValue>(this ExpressionComparison<T,TValue> expressionComparison,TValue value)
        {
            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.QueryOperation, value);
            
            expressionComparison.SetExpression(expression);
            
            return expressionComparison;
        }
    }

    internal static class ExpresssionComparisonExt {

        internal static List<ExpressionComparison<T, TValue>> CreateList<T, TValue>( this ExpressionComparison<T, TValue> expressionComparison)
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
    //public class ModelPropertyList<Model>
    //{

    //}
    //public class ModelPropertyList<Model, Property>
    //{
    //    private Dictionary<string, ExpressionMakerField<Model, Property>> _expressionsMakerFields = new();

    //    public static ModelPropertyList<Model, Property> Create(ExpressionMakerField<Model, Property> expressionMakerField = null)
    //    {
    //        return new ModelPropertyList<Model, Property>().Add(expressionMakerField);
    //    }


    //    public ModelPropertyList<Model, Property> Add(ExpressionMakerField<Model, Property> expressionMakerField)
    //    {
    //        if (expressionMakerField == null)
    //            return this;

    //        _expressionsMakerFields.Add(expressionMakerField.DisplayName, expressionMakerField);

    //        return this;
    //    }

    //    public ModelPropertyList<Model, Property> AddRange(params ExpressionMakerField<Model, Property>[] expressionMakerFields)
    //    {
    //        foreach (var expressionMakerField in expressionMakerFields)
    //        {
    //            Add(expressionMakerField);
    //        }

    //        return this;
    //    }

    //    public ExpressionMakerField<Model, Property> Bind(Expression<Func<Model, Property>> expression, string displayName, params QueryOperation[] queryOperations)
    //    {
    //        var expressionMakerField = ExpressionMakerField<Model, Property>.Bind(expression, displayName, queryOperations);

    //        AddRange(expressionMakerField);

    //        return expressionMakerField;
    //    }

    //    public Result<ExpressionMakerField<Model, Property>> FindBy(string displayName)
    //    {
    //        if (!_expressionsMakerFields.ContainsKey(displayName))
    //            return Result.Failure<ExpressionMakerField<Model, Property>>($"Field {displayName} not found");

    //        return Result.Success(_expressionsMakerFields[displayName]);

    //    }

    //    public IReadOnlyList<ExpressionMakerField<Model, Property>> ToList()
    //    {
    //        return _expressionsMakerFields.Values.ToList();
    //    }
    //}
}
