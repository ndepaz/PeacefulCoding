using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoolFluentHelpers
{
    //IExpressionBuilder<RootModel>
    public class ExpressionBuilderForCollection<RootModel,TValue> : IExpressionBuilderForCollection<RootModel,TValue>
    {
        private readonly List<ICompareExpression<RootModel>> _expressions = new();
        private Expression<Func<RootModel, IEnumerable<TValue>>> expression;

        public ExpressionBuilderForCollection(Expression<Func<RootModel, IEnumerable<TValue>>> expression, string displayName)
        {
            this.expression = expression;
            DisplayName = displayName;
        }

        public IReadOnlyList<ICompareExpression<RootModel>> Expressions => _expressions.AsReadOnly();

        public string DisplayName { get; }

        public static IForCollectionBuilder<RootModel, TValue> Create<RootModel, TValue>(Expression<Func<RootModel, IEnumerable<TValue>>> collectionSelector, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = collectionSelector.Body.ToString();
            }
            //TODO: to reimplement a version of the builder which can be used for collections
            return ForCollectionBuilder<RootModel,TValue>.Create(collectionSelector, displayName);
        }
    }

    public class ForCollectionBuilder<RootModel,T> : IForCollectionBuilder<RootModel, T>
    {
        protected List<ICompareExpressionForCollection<RootModel, T>> compareExpressions = new();
        private Expression<Func<RootModel, IEnumerable<T>>> collectionSelector;
        private readonly string rootDisplayName;

        public ForCollectionBuilder(Expression<Func<RootModel, IEnumerable<T>>> propertyExpression, string rootDisplayName)
        {
            this.collectionSelector = propertyExpression;
            this.rootDisplayName = rootDisplayName;
        }

        public static ForCollectionBuilder<RootModel,T> Create(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, string rootDisplayName)
        {
            return new ForCollectionBuilder<RootModel,T>(collectionSelector,rootDisplayName);
        }

        public IResult<ICompareExpressionForCollection<RootModel, T>> FindByPropertyByDisplayName(string propertyDisplayName)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<ICompareExpressionForCollection<RootModel, T>> FindPropertiesByDisplayName(string propertyDisplayName)
        {
            throw new NotImplementedException();
        }

        public IResult<ICompareExpressionForCollection<RootModel, T>> FirstPropertyByDisplayName(string propertyDisplayName)
        {
            throw new NotImplementedException();
        }

        public ICompareExpressionForCollection<RootModel, T> WithProperty<TValue>(Expression<Func<T, TValue>> propertyExpression, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = $"{rootDisplayName} {propertyExpression.Body.ToString()}";
            }

            var expressionComparison = ExpressionComparisonForCollection<RootModel,T, TValue>.Create(collectionSelector,displayName, propertyExpression);

            compareExpressions.Add(expressionComparison);

            return expressionComparison;
        }

        public Dictionary<string, QueryOperation[]> GetPropertiesSupportedOperations()
        {
            throw new NotImplementedException();
        }
    }

    public interface IForCollectionBuilder<RootModel, T>
    {
        ICompareExpressionForCollection<RootModel, T> WithProperty<TValue>(Expression<Func<T, TValue>> propertyExpression, string displayName = null);
    }

    public class ClauseWithExpression<T>
    {
        public Expression<Func<T, bool>> Expression { get; set; }
        public QueryClause Clause { get; set; }
        private ClauseWithExpression(Expression<Func<T, bool>> expression, QueryClause clause)
        {
            Expression = expression;
            Clause = clause;
        }

        public static ClauseWithExpression<T> Create(Expression<Func<T, bool>> expression, QueryClause clause)
        {
            return new ClauseWithExpression<T>(expression, clause);
        }
    }

    public interface ICompareExpressionForCollection<RootModel, T>
    {
        ICompareValueForCollection<RootModel, T> Compare(QueryOperation queryOperation);
        ICompareValueForCollection<RootModel, T> CompareWithDefault();
        ICompareExpressionForCollection<RootModel, T> OnlyIf(bool condition);
    }

    public class ExpressionComparisonForCollection<RootModel,T, TValue> : ICompareExpressionForCollection<RootModel, T>
    {
        internal Expression<Func<T, TValue>> PropertyExpression { get; }
        public string PropertyDisplayName { get; }

        internal QueryOperation CurrentQueryOperation { get; private set; }

        internal TValue Value { get; private set; }

        internal QueryClause CurrentQueryClause { get; private set; }

        private bool _isOnlyIf = true;

        private List<ClauseWithExpression<T>> _expressionAndList = new();
        //private List<Expression<Func<T, bool>>> _expressionOrList = new();
        private Expression<Func<RootModel, IEnumerable<T>>> collectionSelector;

        private ExpressionComparisonForCollection(Expression<Func<T, TValue>> propertyExpression,
                string propertyDisplayName,
                TValue value,
                QueryOperation queryOperation
            )
        {
            PropertyExpression = propertyExpression;
            PropertyDisplayName = propertyDisplayName;
            CurrentQueryOperation = queryOperation;
            Value = value;
        }

        public ExpressionComparisonForCollection(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, Expression<Func<T, TValue>> propertyExpression, string propertyDisplayName)
        {
            this.collectionSelector = collectionSelector;
            PropertyExpression = propertyExpression;
            PropertyDisplayName = propertyDisplayName;
        }

        internal static ExpressionComparisonForCollection<RootModel,T, TValue> StoreComparisonAndQueryInfo(ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        {
            return new ExpressionComparisonForCollection<RootModel,T, TValue>(
                expressionComparison.PropertyExpression,
                expressionComparison.PropertyDisplayName,
                expressionComparison.Value,
                expressionComparison.CurrentQueryOperation
                )
                .ChangeCurrentClause(expressionComparison.CurrentQueryClause);
        }

        internal static ExpressionComparisonForCollection<RootModel,T, TValue> Create(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, string propertyDisplayName, Expression<Func<T, TValue>> propertyExpression)
        {
            return new ExpressionComparisonForCollection<RootModel,T, TValue>(collectionSelector,propertyExpression, propertyDisplayName);
        }

        public ICompareValueForCollection<RootModel,T> CompareWithDefault()
        {
            return ExpressionValueForCollection<RootModel,T, TValue>.Create(collectionSelector,this);
        }

        public ICompareValueForCollection<RootModel, T> Compare(QueryOperation queryOperation)
        {
            CurrentQueryOperation = queryOperation;

            return ExpressionValueForCollection<RootModel,T, TValue>.Create(collectionSelector,this);
        }

        internal ExpressionComparisonForCollection<RootModel,T, TValue> ChangeCurrentClause(QueryClause queryClause)
        {
            CurrentQueryClause = queryClause;

            return this;
        }

        internal ExpressionComparisonForCollection<RootModel,T, TValue> ChangeCurrentToAnd()
        {
            CurrentQueryClause = QueryClause.And;
            return this;
        }

        internal ExpressionComparisonForCollection<RootModel,T, TValue> ChangeCurrentToOr()
        {
            CurrentQueryClause = QueryClause.Or;
            return this;
        }

        internal void SetValue(TValue value)
        {
            this.Value = value;
        }

        public ICompareExpressionForCollection<RootModel, T> OnlyIf(bool condition)
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

            //var orExpressionsList = _expressionOrList;

            return ExpressionCombiner.CombineExpressionsInOrder(andExpressionsList);
        }

        internal ICompareExpressionForCollection<RootModel, T> AddExpressionsToList(ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        {

            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.CurrentQueryOperation, expressionComparison.Value);

            _expressionAndList.Add(ClauseWithExpression<T>.Create(expression, CurrentQueryClause));

            return this;
        }

        //internal ICompareExpressionForCollection<RootModel, T> AddExpressionsToOrList(ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        //{
        //    var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.CurrentQueryOperation, expressionComparison.Value);

        //    _expressionAndList.Add(expression);

        //    return this;
        //}

        public Type GetPropertyType()
        {
            return typeof(TValue);
        }

        public QueryOperation GetQueryOperation()
        {
            return CurrentQueryOperation;
        }
    }

    public interface ICompareAndOrForCollection<RootModel,T>
    {
        public ICompareExpressionForCollection<RootModel, T> OrElse();
        public ICompareExpressionForCollection<RootModel, T> AndAlso();
        /// <summary>
        /// Combine the current expression with the next expression using the clause
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        ICompareExpressionForCollection<RootModel, T> CombineWith(QueryClause clause);
        /// <summary>
        /// Returns the combined expressions as a single expression
        /// </summary>
        /// <returns></returns>
        public Result<Expression<Func<RootModel, bool>>> AsExpressionResult();
    }
    public interface ICompareValueForCollection<RootModel, T>
    {
        ICompareAndOrForCollection<RootModel, T> WithAnyValue(object value);
    }
    
    public class ExpressionValueForCollection<RootModel,T, TValue> : ICompareValueForCollection<RootModel,T>, ICompareAndOrForCollection<RootModel, T>
    {
        private object _value;
        private readonly Expression<Func<RootModel, IEnumerable<T>>> collectionSelector;

        private ExpressionValueForCollection(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        {
            this.collectionSelector = collectionSelector;
            _expressionComparison = expressionComparison;
        }

        private ExpressionComparisonForCollection<RootModel,T, TValue> _expressionComparison { get; }

        internal static ExpressionValueForCollection<RootModel,T, TValue> Create(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        {
            return new ExpressionValueForCollection<RootModel,T, TValue>(collectionSelector,expressionComparison);
        }

        internal ExpressionComparisonForCollection<RootModel,T, TValue> WithValue(TValue value)
        {
            _expressionComparison.SetValue(value);

            return ExpressionComparisonForCollection<RootModel,T, TValue>.StoreComparisonAndQueryInfo(_expressionComparison);
        }

        public ICompareAndOrForCollection<RootModel, T> WithAnyValue(object value)
        {

            _value = value;
            //SetToOriginClause();

            return this;
        }

        public ICompareExpressionForCollection<RootModel, T> CombineWith(QueryClause clause)
        {
            if (clause is QueryClause.And)
            {
                return AndAlso();
            }

            return OrElse();
        }

        public ICompareExpressionForCollection<RootModel, T> OrElse()
        {


            var instance = _expressionComparison.AddExpressionsToList(WithValue((TValue)_value));
            _expressionComparison.ChangeCurrentClause(QueryClause.Or);

            return instance;
        }

        public ICompareExpressionForCollection<RootModel, T> AndAlso()
        {
            var instance = _expressionComparison.AddExpressionsToList(WithValue((TValue)_value));

            _expressionComparison.ChangeCurrentClause(QueryClause.And);

            return instance;
        
        }

        public Result<Expression<Func<RootModel, bool>>> AsExpressionResult()
        {
            _expressionComparison.AddExpressionsToList(WithValue((TValue)_value));

            var result = _expressionComparison.AsExpression();

            if (result.IsFailure)
            {
                return Result.Failure<Expression<Func<RootModel, bool>>>(result.Error);
            }

            return Result.Success(PredicateBuilder.BuildCollectionPredicate(collectionSelector, result.Value, false));
        }
    }

    public interface IExpressionBuilderForCollection<Model, TCollection>
    {
        static abstract IForCollectionBuilder<RootModel, TValue> Create<RootModel, TValue>(Expression<Func<RootModel, IEnumerable<TValue>>> propertyExpression, string displayName = null);
    }
}
