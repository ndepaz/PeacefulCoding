using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CoolFluentHelpers
{
    public class ExpressionBuilder<T>
    {
        private ExpressionBuilder()
        {

        }

        public static ExpressionBuilder<T> Create()
        {
            return new ExpressionBuilder<T>();
        }

        public Expression<Func<T, bool>> Compare<TValue>(Expression<Func<T, TValue>> propertyExpression, AsQuery<TValue> operation, TValue value)
        {
            return ExpressionBuilder.BuildPredicate(propertyExpression, operation.Get(), value);
        }
    }

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
    public enum QueryStringOperation
    {

        StartsWith,
        EndsWith,
        Contains,
        Equals,
    }

    public enum QueryNumberOperation
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum QueryDateOperation
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum QueryBoolOperation
    {
        Equals,
    }

    public class AsQuery
    {
        protected AsQuery(QueryOperation queryOperation)
        {
            QueryOperation = queryOperation;
        }

        public static AsQuery<string> String(QueryStringOperation operation)
        {
            return AsQuery<string>.String<string>(operation);
        }

        public static AsQuery<TValue> Number<TValue>(QueryNumberOperation operation) where TValue : INumber<TValue>
        {
            return AsQuery<TValue>.Number<TValue>(operation);
        }

        public static AsQuery<DateTime> Date(QueryDateOperation operation)
        {
            return AsQuery<DateTime>.Date<DateTime>(operation);
        }

        public static AsQuery<bool> Bool(QueryBoolOperation operation)
        {
            return AsQuery<bool>.Bool<bool>(operation);
        }
        private QueryOperation QueryOperation { get; }
        public QueryOperation Get()
        {
            return QueryOperation;
        }
    }

    public class AsQuery<T> : AsQuery
    {
        private AsQuery(QueryOperation queryOperation) : base(queryOperation)
        {
        }
        
        public static AsQuery<string> String<TValue>(QueryStringOperation operation) where TValue : class
        {
            return new AsQuery<string>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<TValue> Number<TValue>(QueryNumberOperation operation) where TValue : INumber<TValue>
        {
            return new AsQuery<TValue>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<DateTime> Date<TValue>(QueryDateOperation operation) where TValue : struct
        {
            return new AsQuery<DateTime>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<DateTime?> Date(QueryDateOperation operation)
        {
            return new AsQuery<DateTime?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<bool> Bool<TValue>(QueryBoolOperation operation) where TValue : struct
        {
            return new AsQuery<bool>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<bool?> Bool(QueryBoolOperation operation)
        {
            return new AsQuery<bool?>(QueryOperationConverter.Convert(operation));
        }

    }
}
