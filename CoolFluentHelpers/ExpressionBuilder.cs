using CoolFluentHelpers;
using CSharpFunctionalExtensions;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoolFluentHelpers
{
    public enum QueryClause
    {
        And,
        Or
    }

    public enum QueryOperation
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        StartsWith,
        EndsWith,
        Contains,
    }
    public class NestedExpressionBuilder<T, TValue> : ExpressionBuilder<TValue>
    {
        private readonly Expression<Func<T, IEnumerable<TValue>>> _enumerablePropertyExpression;
        private readonly string _displayName;
        private readonly ExpressionBuilder<T> _parentBuilder;
        private readonly string _nestedBasePropertyExpressionPath;

        private NestedExpressionBuilder(Expression<Func<T, IEnumerable<TValue>>> propertyExpression, string displayName, ExpressionBuilder<T> parentBuilder)
        {
            _enumerablePropertyExpression = propertyExpression;
            _displayName = displayName;
            _parentBuilder = parentBuilder;
        }

        public static NestedExpressionBuilder<T, TValue> Create(Expression<Func<T, IEnumerable<TValue>>> propertyExpression, string displayName, ExpressionBuilder<T> parentBuilder)
        {
            return new NestedExpressionBuilder<T, TValue>(propertyExpression, displayName, parentBuilder);
        }

        public static Expression<Func<TItem, bool>> PropertyContains<TItem, TProperty>(
            Expression<Func<TItem, IEnumerable<TProperty>>> selectList,
            Expression<Func<TProperty, bool>> whereClause)
                {
                    var listParameter = selectList.Parameters[0];
                    var propPredicate = whereClause.Body;

                    var listSelector = selectList.Body;

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(typeof(TProperty));

                    var anyExpression = Expression.Call(anyMethod, listSelector, whereClause);

                    return Expression.Lambda<Func<TItem, bool>>(anyExpression, listParameter);
        }

        public ICompareExpression<T, TValue> ForNestedProperty<TNestedValue>(Expression<Func<TValue, TNestedValue>> nestedPropertyExpression, string nestedDisplayName = null)
        {
            if (nestedDisplayName is null)
            {
                nestedDisplayName = nestedPropertyExpression.Body.ToString();
            }

            ICompareExpression<T, TValue> nestedExpressionComparison = ExpressionComparison<T, TValue, TNestedValue>.Create(nestedDisplayName, nestedPropertyExpression, _enumerablePropertyExpression);

            return nestedExpressionComparison;

        }
    }

}

    public class ExpressionBuilder<T> : IExpressionBuilder<T>
    {

        private List<ICompareExpression<T>> compareExpressions = new();

        protected ExpressionBuilder()
        {

        }

        public static ExpressionBuilder<T> Create()
        {
            return new ExpressionBuilder<T>();
        }

        private List<IExpressionBuilder<T>> nestedBuilders = new();

        public ICompareExpression<T> ForProperty<TValue>(Expression<Func<T, TValue>> propertyExpression, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = propertyExpression.Body.ToString();
            }

            var expressionComparison = ExpressionComparison<T, TValue>.Create(displayName, propertyExpression);

            compareExpressions.Add(expressionComparison);

            return expressionComparison;
        }
        public NestedExpressionBuilder<T, TValue> ForList<TValue>(Expression<Func<T, IEnumerable<TValue>>> propertyExpression, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = propertyExpression.Body.ToString();
            }
        
            var nestedBuilder = NestedExpressionBuilder<T, TValue>.Create(propertyExpression, displayName, this);
            
            return nestedBuilder;
        }

        public IResult<ICompareExpression<T>> FindByPropertyByDisplayName(string propertyDisplayName)
        {
            var expressionComparison = compareExpressions.FirstOrDefault(x => x.PropertyDisplayName == propertyDisplayName);

            if (expressionComparison != null)
            {
                return Result.Success(expressionComparison);
            }

            foreach (var nestedBuilder in nestedBuilders)
            {
                var nestedResult = nestedBuilder.FindByPropertyByDisplayName(propertyDisplayName);
                if (nestedResult.IsSuccess)
                {
                    return nestedResult;
                }
            }

            return Result.Failure<ICompareExpression<T>>("Property was not found");
        }

        public IReadOnlyList<ICompareExpression<T>> FindPropertiesByDisplayName(string propertyDisplayName)
        {
            return compareExpressions.Where(x => x.PropertyDisplayName == propertyDisplayName).ToList();
        }
    }

    public interface IExpressionBuilder<T>
    {
        IResult<ICompareExpression<T>> FindByPropertyByDisplayName(string propertyDisplayName);
        IReadOnlyList<ICompareExpression<T>> FindPropertiesByDisplayName(string propertyDisplayName);
        ICompareExpression<T> ForProperty<TValue>(Expression<Func<T, TValue>> propertyExpression, string displayName = null);
    }

    public class ExpressionComparison<T, TValue> : ICompareExpression<T>
    {
        internal Expression<Func<T, TValue>> PropertyExpression { get; }
        public string PropertyDisplayName { get; }

        internal QueryOperation QueryOperation { get; private set; }

        internal TValue Value { get; private set; }

        internal bool IsAnd { get; private set; } = true;

        private bool _isOnlyIf = true;

        private List<Expression<Func<T, bool>>> _expressionAndList = new();
        private List<Expression<Func<T, bool>>> _expressionOrList = new();

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression, string propertyDisplayName)
        {
            PropertyExpression = propertyExpression;
            PropertyDisplayName = propertyDisplayName;

        }

        private ExpressionComparison(Expression<Func<T, TValue>> propertyExpression, string propertyDisplayName, TValue value, QueryOperation queryOperation)
        {
            PropertyExpression = propertyExpression;
            PropertyDisplayName = propertyDisplayName;
            QueryOperation = queryOperation;
            Value = value;
        }

        internal static ExpressionComparison<T, TValue> Copy(ExpressionComparison<T, TValue> expressionComparison)
        {
            return new ExpressionComparison<T, TValue>(
                expressionComparison.PropertyExpression,
                expressionComparison.PropertyDisplayName,
                expressionComparison.Value,
                expressionComparison.QueryOperation)
                .ChangeCurrentAndOrClause(expressionComparison.IsAnd);
        }

        internal static ExpressionComparison<T, TValue> Create(string propertyDisplayName, Expression<Func<T, TValue>> propertyExpression)
        {
            return new ExpressionComparison<T, TValue>(propertyExpression, propertyDisplayName);
        }


        public ICompareValue<T> CompareWithDefault()
        {
            return ExpressionValue<T, TValue>.Create(this);
        }

        public ICompareValue<T> Compare(QueryOperation queryOperation)
        {
            QueryOperation = queryOperation;

            return ExpressionValue<T, TValue>.Create(this);
        }

        private ExpressionComparison<T, TValue> ChangeCurrentAndOrClause(bool useAnd)
        {
            IsAnd = useAnd;

            return this;
        }

        internal ExpressionComparison<T, TValue> ChangeCurrentToAnd()
        {
            return ChangeCurrentAndOrClause(true);
        }

        internal ExpressionComparison<T, TValue> ChangeCurrentToOr()
        {
            return ChangeCurrentAndOrClause(false);
        }

        internal void SetValue(TValue value)
        {
            this.Value = value;
        }

        public ICompareExpression<T> OnlyIf(bool condition)
        {
            _isOnlyIf = condition;

            return this;
        }

        private bool MeetOnlyCondition()
        {
            return _isOnlyIf;
        }

        internal IResult<Expression<Func<T, bool>>> AsExpression()
        {
            if (!MeetOnlyCondition())
            {
                return Result.Failure<Expression<Func<T, bool>>>("OnlyIf condition was not met");
            }

            var andExpressionsList = _expressionAndList;

            var orExpressionsList = _expressionOrList;

            return ExpressionCombiner.CombineExpressions(andExpressionsList, orExpressionsList);
        }

        internal ICompareExpression<T> AddExpressionsToAndList(ExpressionComparison<T, TValue> expressionComparison)
        {

            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.QueryOperation, expressionComparison.Value);

            _expressionAndList.Add(expression);

            return this;
        }

        internal ICompareExpression<T> AddExpressionsToOrList(ExpressionComparison<T, TValue> expressionComparison)
        {
            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.QueryOperation, expressionComparison.Value);

            _expressionOrList.Add(expression);

            return this;
        }
    }

public interface ICompareExpression<Base,Sub>
{
    string PropertyDisplayName { get; }
    /// <summary>
    /// Deault QueryOperation can be Equals or a predefined QueryOperation by calling Compare(QueryOperation queryOperation) before calling CompareWithDefault()
    /// </summary>
    /// <returns></returns>
    public ICompareValue<Base,Sub> CompareWithDefault();

    /// <summary>
    /// Set the QueryOperation to be used by default
    /// </summary>
    /// <param name="queryOperation"></param>
    /// <returns></returns>
    public ICompareValue<Base, Sub> Compare(QueryOperation queryOperation);
    ICompareExpression<Base> OnlyIf(bool condition);
}

public interface ICompareExpression<T>
    {
        string PropertyDisplayName { get; }
        /// <summary>
        /// Deault QueryOperation can be Equals or a predefined QueryOperation by calling Compare(QueryOperation queryOperation) before calling CompareWithDefault()
        /// </summary>
        /// <returns></returns>
        public ICompareValue<T> CompareWithDefault();

        /// <summary>
        /// Set the QueryOperation to be used by default
        /// </summary>
        /// <param name="queryOperation"></param>
        /// <returns></returns>
        public ICompareValue<T> Compare(QueryOperation queryOperation);
        ICompareExpression<T> OnlyIf(bool condition);
    }

    public interface ICompareAndOr<T>
    {
        public ICompareExpression<T> OrElse();
        public ICompareExpression<T> AndAlso();
        /// <summary>
        /// Combine the current expression with the next expression using the clause
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        ICompareExpression<T> CombineWith(QueryClause clause);
        /// <summary>
        /// Returns the combined expressions as a single expression
        /// </summary>
        /// <returns></returns>
        public IResult<Expression<Func<T, bool>>> AsExpressionResult();
    }

    public interface ICompareValue<T>
    {

        /// <summary>
        /// The value it's automatically converted to the type of the property.
        /// Ideally, convert your value to the type of the property before calling this method.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ICompareAndOr<T> WithAnyValue(object value);
    }

    public class ExpressionValue<T, TValue> : ICompareValue<T>, ICompareAndOr<T>
    {
        private object _value;

        private ExpressionValue(ExpressionComparison<T, TValue> expressionComparison)
        {
            _expressionComparison = expressionComparison;
        }

        private ExpressionComparison<T, TValue> _expressionComparison { get; }

        internal static ExpressionValue<T, TValue> Create(ExpressionComparison<T, TValue> expressionComparison)
        {
            return new ExpressionValue<T, TValue>(expressionComparison);
        }

        internal ExpressionComparison<T, TValue> WithValue(TValue value)
        {
            _expressionComparison.SetValue(value);

            return ExpressionComparison<T, TValue>.Copy(_expressionComparison);
        }

        public ICompareAndOr<T> WithAnyValue(object value)
        {
            _value = value;

            return this;
        }

        public ICompareExpression<T> CombineWith(QueryClause clause)
        {
            if (clause is QueryClause.And)
            {
                return AndAlso();
            }

            return OrElse();
        }
        
        public ICompareExpression<T> OrElse()
        {
            _expressionComparison.ChangeCurrentToOr();

            return _expressionComparison.AddExpressionsToOrList(WithValue((TValue)_value));
        }

        public ICompareExpression<T> AndAlso()
        {
            _expressionComparison.ChangeCurrentToAnd();

            return _expressionComparison.AddExpressionsToAndList(WithValue((TValue)_value));
        }

        public IResult<Expression<Func<T, bool>>> AsExpressionResult()
        {
            if (_expressionComparison.IsAnd)
            {
                AndAlso();
            }
            else
            {
                OrElse();
            }

            return _expressionComparison.AsExpression();
        }
    }

    public static class ExpressionCombiner
    {
        public static IResult<Expression<Func<T, bool>>> CombineExpressions<T>(
            List<Expression<Func<T, bool>>> andExpressions,
            List<Expression<Func<T, bool>>> orExpressions)
        {
            Expression<Func<T, bool>> combinedExpression = null;

            // Combine the predicates in andExpressions using AND operator
            foreach (var expression in andExpressions)
            {
                combinedExpression = CombineExpression(combinedExpression, expression, Expression.AndAlso);
            }

            // Combine the predicates in orExpressions using OR operator
            foreach (var expression in orExpressions)
            {
                combinedExpression = CombineExpression(combinedExpression, expression, Expression.OrElse);
            }

            // If both andExpressions and orExpressions are empty, return an error
            if (combinedExpression == null)
            {
                return Result.Failure<Expression<Func<T, bool>>>("No expressions found.");
            }

            return Result.Success(combinedExpression);
        }

        private static Expression<Func<T, bool>> CombineExpression<T>(Expression<Func<T, bool>> initialExpression, Expression<Func<T, bool>> expression, Func<Expression, Expression, BinaryExpression> combiner)
        {
            if (initialExpression == null)
            {
                return expression;
            }

            // Create a parameter expression to represent the lambda parameter 'x'
            ParameterExpression parameter = expression.Parameters[0];

            // Replace parameter expressions in the second expression with the parameter from the first expression
            Expression body = new ReplaceExpressionVisitor(expression.Parameters[0], initialExpression.Parameters[0]).Visit(expression.Body);

            return Expression.Lambda<Func<T, bool>>(combiner(initialExpression.Body, body), initialExpression.Parameters);
        }
    }

    internal class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _source;
        private readonly Expression _target;

        public ReplaceExpressionVisitor(Expression source, Expression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _source ? _target : base.VisitParameter(node);
        }
    }

