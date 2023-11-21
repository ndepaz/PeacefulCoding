using CSharpFunctionalExtensions;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
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
        NotEqual
    }

    public class ExpressionBuilder<T> : IExpressionBuilder<T>
    {

        protected List<ICompareExpression<T>> compareExpressions = new();

        protected ExpressionBuilder()
        {

        }

        public static ExpressionBuilder<T> Create()
        {
            return new ExpressionBuilder<T>();
        }
        public void AddExpression(ICompareExpression<T> compareExpression)
        {
            compareExpressions.Add(compareExpression);
        }
        public Dictionary<string, QueryOperation[]> GetPropertiesSupportedOperations()
        {
            var result = new Dictionary<string, QueryOperation[]>();

            foreach (var expression in compareExpressions)
            {
                if (result.ContainsKey(expression.PropertyDisplayName))
                {
                    result[expression.PropertyDisplayName] = result[expression.PropertyDisplayName].Append(expression.GetQueryOperation()).ToArray();
                }
                else
                {
                    result.Add(expression.PropertyDisplayName, new[] { expression.GetQueryOperation() });
                }
            }
            return result;
        }
        public static IExpressionBuilderForCollection<T> ForCollections()
        {
            return new ExpressionBuilderForCollection<T>();
        }

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

        public IResult<ICompareExpression<T>> FirstPropertyByDisplayName(string propertyDisplayName)
        {
            var expressionComparison = compareExpressions.FirstOrDefault(x => x.PropertyDisplayName == propertyDisplayName);

            return Result.SuccessIf(expressionComparison is not null, expressionComparison, "Property was not found");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use FirstPropertyByDisplayName instead. Obsolete due to Typo")]
        public IResult<ICompareExpression<T>> FindByPropertyByDisplayName(string propertyDisplayName)
        {
            var expressionComparison = compareExpressions.FirstOrDefault(x => x.PropertyDisplayName == propertyDisplayName);

            return Result.SuccessIf(expressionComparison is not null, expressionComparison, "Property was not found");
        }

        public IReadOnlyList<ICompareExpression<T>> FindPropertiesByDisplayName(string propertyDisplayName)
        {
            return compareExpressions.Where(x => x.PropertyDisplayName == propertyDisplayName).ToList();
        }


    }

    public interface IExpressionBuilder<T>
    {
        abstract static IExpressionBuilderForCollection<T> ForCollections();
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use FirstPropertyByDisplayName instead. Obsolete due to Typo")]
        IResult<ICompareExpression<T>> FindByPropertyByDisplayName(string propertyDisplayName);
        IReadOnlyList<ICompareExpression<T>> FindPropertiesByDisplayName(string propertyDisplayName);
        IResult<ICompareExpression<T>> FirstPropertyByDisplayName(string propertyDisplayName);
        /// <summary>
        /// Key is the property display name, Value is the supported operations for that property
        /// </summary>
        /// <returns></returns>
        Dictionary<string, QueryOperation[]> GetPropertiesSupportedOperations();
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

        private List<ClauseWithExpression<T>> _expressionsList = new();
        internal QueryClause CurrentQueryClause { get; private set; }
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

        internal static ExpressionComparison<T, TValue> StoreComparisonAndQueryInfo(ExpressionComparison<T, TValue> expressionComparison)
        {
            return new ExpressionComparison<T, TValue>(
                expressionComparison.PropertyExpression,
                expressionComparison.PropertyDisplayName,
                expressionComparison.Value,
                expressionComparison.QueryOperation)
                .ChangeCurrentClause(expressionComparison.CurrentQueryClause);
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
        internal ExpressionComparison<T, TValue> ChangeCurrentClause(QueryClause queryClause)
        {
            CurrentQueryClause = queryClause;

            return this;
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

            return ExpressionCombiner.CombineExpressionsInOrder(_expressionsList);
        }

        internal ICompareExpression<T> AddExpressionsList(ExpressionComparison<T, TValue> expressionComparison)
        {

            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.QueryOperation, expressionComparison.Value);

            _expressionsList.Add(ClauseWithExpression<T>.Create(expression, CurrentQueryClause));

            return this;
        }

        public Type GetPropertyType()
        {
            return typeof(TValue);
        }

        public QueryOperation GetQueryOperation()
        {
            return QueryOperation;
        }
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

        Type GetPropertyType();
        /// <summary>
        /// All properties are initialized to QueryOperation Equals unless specified.
        /// </summary>
        /// <returns></returns>
        public QueryOperation GetQueryOperation();
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

            return ExpressionComparison<T, TValue>.StoreComparisonAndQueryInfo(_expressionComparison);
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
            var instance = _expressionComparison.AddExpressionsList(WithValue((TValue)_value));
            
            _expressionComparison.ChangeCurrentClause(QueryClause.Or);

            return instance;
        }

        public ICompareExpression<T> AndAlso()
        {
            var instance = _expressionComparison.AddExpressionsList(WithValue((TValue)_value));

            _expressionComparison.ChangeCurrentClause(QueryClause.And);

            return instance;
        }

        public IResult<Expression<Func<T, bool>>> AsExpressionResult()
        {
            _expressionComparison.AddExpressionsList(WithValue((TValue)_value));

            return _expressionComparison.AsExpression();
        }
    }

    public static class ExpressionCombiner
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use CombineExpressionsInOrder instead")]
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

        public static IResult<Expression<Func<T, bool>>> CombineExpressionsInOrder<T>(List<ClauseWithExpression<T>> clauseWithExpressions)
        {
            Expression<Func<T, bool>> combinedExpression = null;

            var index = 0;
            foreach (var currentClause in clauseWithExpressions)
            {
                index++;

                if (index % 2 == 0)
                {
                    var previousClause = clauseWithExpressions[index - 2];

                    Expression<Func<T, bool>> left = previousClause.Expression;
                    Expression<Func<T, bool>> right = currentClause.Expression;

                    var parameter = left.Parameters[0]; // single parameter expected for Func<T, bool>

                    // Create an instance of the ReplaceExpressionVisitor to replace the right parameter with the left one
                    var visitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
                    var rightBodyWithReplacedParameter = visitor.Visit(right.Body);

                    Func<Expression, Expression, BinaryExpression> leftCombiner = previousClause.Clause == QueryClause.And ? Expression.AndAlso : Expression.OrElse;
                    Func<Expression, Expression, BinaryExpression> rightCombiner = currentClause.Clause == QueryClause.And ? Expression.AndAlso : Expression.OrElse;

                    var combinedBody = rightCombiner(left.Body, rightBodyWithReplacedParameter);
                    var dualExpression = Expression.Lambda<Func<T, bool>>(combinedBody, parameter);

                    if (combinedExpression is null)
                    {
                        combinedExpression = dualExpression;
                    }
                    else
                    {
                        // Use the visitor here as well to ensure that expression parameters match
                        visitor = new ReplaceExpressionVisitor(dualExpression.Parameters[0], combinedExpression.Parameters[0]);
                        var combinedBodyWithReplacedParameter = visitor.Visit(dualExpression.Body);
                        combinedExpression = Expression.Lambda<Func<T, bool>>(
                            leftCombiner(combinedExpression.Body, combinedBodyWithReplacedParameter), combinedExpression.Parameters);
                    }
                }
                else
                {
                    if (index == clauseWithExpressions.Count)
                    {
                        Expression<Func<T, bool>> left = combinedExpression;
                        Expression<Func<T, bool>> right = currentClause.Expression;

                        if(combinedExpression == null)
                        {
                            combinedExpression = right;
                        }
                        else
                        {

                            // Replace parameters before combining expressions
                            var visitor = new ReplaceExpressionVisitor(right.Parameters[0], left.Parameters[0]);
                            var rightBodyWithReplacedParameter = visitor.Visit(right.Body);

                            Func<Expression, Expression, BinaryExpression> rightCombiner = currentClause.Clause == QueryClause.And ? Expression.AndAlso : Expression.OrElse;

                            var combinedBody = rightCombiner(left.Body, rightBodyWithReplacedParameter);
                            combinedExpression = Expression.Lambda<Func<T, bool>>(combinedBody, left.Parameters);
                        }
                    }
                }
            }

            if (combinedExpression == null)
            {
                return Result.Failure<Expression<Func<T, bool>>>("No expressions found.");
            }

            return Result.Success(combinedExpression);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use CombineExpressionsInOrder instead")]
        private static Expression<Func<T, bool>> CombineExpression<T>(Expression<Func<T, bool>> initialExpression, Expression<Func<T, bool>> newExpression, Func<Expression, Expression, BinaryExpression> combiner)
        {
            if (initialExpression == null)
            {
                return newExpression;
            }

            // Create a parameter expression to represent the lambda parameter 'x'
            ParameterExpression parameter = newExpression.Parameters[0];

            // Replace parameter expressions in the second expression with the parameter from the first expression
            Expression body = new ReplaceExpressionVisitor(newExpression.Parameters[0], initialExpression.Parameters[0]).Visit(newExpression.Body);
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
}
