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
        private AsQuery<TValue> AsQuery { get; }
        private Expression<Func<T, TValue>> PropertyExpression { get; }

        private readonly QueryOperation _queryOperation;
        private Expression<Func<T, bool>> _expression;

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression)
        {
            PropertyExpression = propertyExpression;
        }

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression, AsQuery<TValue> asQuery)
        {
            PropertyExpression = propertyExpression;
            AsQuery = asQuery;
        }

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression, QueryOperation queryOperation)
        {
            PropertyExpression = propertyExpression;
            _queryOperation = queryOperation;
        }

        internal static ExpressionComparison<T, TValue> Create(Expression<Func<T, TValue>> propertyExpression)
        {
            return new ExpressionComparison<T, TValue>(propertyExpression);
        }

        private static ExpressionComparison<T, TValue> Create(Expression<Func<T, TValue>> propertyExpression, AsQuery<TValue> asQuery)
        {
            return new ExpressionComparison<T, TValue>(propertyExpression, asQuery);
        }

        private ExpressionComparison<T, TValue> UnsafeCreate(Expression<Func<T, TValue>> propertyExpression, QueryOperation asQuery)
        {
            return new ExpressionComparison<T, TValue>(propertyExpression, asQuery);
        }

        public ExpressionComparison<T, TValue> Compare(QueryOperation queryOperation)
        {
            return UnsafeCreate(PropertyExpression, queryOperation);
        }

        public ExpressionComparison<T, TValue> Compare(AsQuery<TValue> asQuery)
        {
            return Create(PropertyExpression, asQuery);
        }

        public ExpressionComparison<T, TValue> WithValue(TValue value)
        {
            if (AsQuery != null)
            {
                _expression = ExpressionBuilder.BuildPredicate(PropertyExpression, AsQuery.Get(), value);
            }
            else
            {
                _expression = ExpressionBuilder.BuildPredicate(PropertyExpression, _queryOperation, value);
            }

            return this;
        }

        public IResult<Expression<Func<T, bool>>> AsExpression()
        {
            return Result.Success(_expression);
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
            return AsQuery<TValue>.Number<TValue>(operation);
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
