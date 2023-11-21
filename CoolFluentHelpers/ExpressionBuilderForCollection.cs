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
    public class ExpressionBuilderForCollection<RootModel> : IExpressionBuilderForCollection<RootModel>
    {
        public ExpressionBuilderForCollection()
        {
        }

        internal static IExpressionBuilderForCollection<RootModel1> Create<RootModel1>()
        {
            return new ExpressionBuilderForCollection<RootModel1>();
        }

        public IForCollectionBuilder<RootModel,T> ForCollection<T>(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, string rootDisplayName)
        {
            var forCollectionBuilder = ForCollectionBuilder<RootModel,T>.Create(collectionSelector, rootDisplayName);
            
            return forCollectionBuilder;
        }
    }

    public class ForCollectionBuilder<RootModel,TCollectionModel> : IForCollectionBuilder<RootModel, TCollectionModel>
    {
        protected List<ICompareExpressionForCollection<RootModel>> compareExpressions = new();
        private Expression<Func<RootModel, IEnumerable<TCollectionModel>>> collectionSelector;
        private readonly string rootDisplayName;

        public ForCollectionBuilder(Expression<Func<RootModel, IEnumerable<TCollectionModel>>> propertyExpression, string rootDisplayName)
        {
            this.collectionSelector = propertyExpression;
            this.rootDisplayName = rootDisplayName;
        }

        internal static ForCollectionBuilder<RootModel, TCollectionModel> Create(Expression<Func<RootModel, IEnumerable<TCollectionModel>>> collectionSelector, string rootDisplayName)
        {
            return new ForCollectionBuilder<RootModel,TCollectionModel>(collectionSelector,rootDisplayName);
        }

        public IReadOnlyList<ICompareExpressionForCollection<RootModel>> FindPropertiesByDisplayName(string propertyDisplayName)
        {
            return compareExpressions.Where(x => x.PropertyDisplayName == propertyDisplayName).ToList();
        }

        public Result<ICompareExpressionForCollection<RootModel>> FirstPropertyByDisplayName(string propertyDisplayName)
        {
            var expressionComparison = compareExpressions.FirstOrDefault(x => x.PropertyDisplayName == propertyDisplayName);

            return Result.SuccessIf(expressionComparison is not null, expressionComparison, "Property was not found");
        }
        public ICompareExpressionForCollection<RootModel> ForProperty<TValue>(Expression<Func<TCollectionModel, TValue>> propertyExpression, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = $"{rootDisplayName} {propertyExpression.Body.ToString()}";
            }

            var expressionComparison = ExpressionComparisonForCollection<RootModel, TCollectionModel, TValue>.Create(collectionSelector, displayName, propertyExpression);

            compareExpressions.Add(expressionComparison);

            return expressionComparison;
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
    }

    public interface IForCollectionBuilder<RootModel, TCollectionModel>
    {
        IReadOnlyList<ICompareExpressionForCollection<RootModel>> FindPropertiesByDisplayName(string propertyDisplayName);
        Result<ICompareExpressionForCollection<RootModel>> FirstPropertyByDisplayName(string propertyDisplayName);
        ICompareExpressionForCollection<RootModel> ForProperty<TValue>(Expression<Func<TCollectionModel, TValue>> propertyExpression, string displayName = null);
        Dictionary<string, QueryOperation[]> GetPropertiesSupportedOperations();
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

    public interface ICompareExpressionForCollection<RootModel>
    {
        string PropertyDisplayName { get; }

        ICompareValueForCollection<RootModel> Compare(QueryOperation queryOperation);
        ICompareValueForCollection<RootModel> CompareWithDefault();
        ICompareExpressionForCollection<RootModel> OnlyIf(bool condition);
        public QueryOperation GetQueryOperation();
    }

    public class ExpressionComparisonForCollection<RootModel,T, TValue> : ICompareExpressionForCollection<RootModel>
    {
        internal Expression<Func<T, TValue>> PropertyExpression { get; }
        public string PropertyDisplayName { get; }

        internal QueryOperation CurrentQueryOperation { get; private set; }

        internal TValue Value { get; private set; }

        internal QueryClause CurrentQueryClause { get; private set; }

        private bool _isOnlyIf = true;

        private List<ClauseWithExpression<T>> _expressionList = new();

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

        public ICompareValueForCollection<RootModel> CompareWithDefault()
        {
            return ExpressionValueForCollection<RootModel,T, TValue>.Create(collectionSelector,this);
        }

        public ICompareValueForCollection<RootModel> Compare(QueryOperation queryOperation)
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

        public ICompareExpressionForCollection<RootModel> OnlyIf(bool condition)
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

            var andExpressionsList = _expressionList;

            return ExpressionCombiner.CombineExpressionsInOrder(andExpressionsList);
        }

        internal ICompareExpressionForCollection<RootModel> AddExpressionsList(ExpressionComparisonForCollection<RootModel,T, TValue> expressionComparison)
        {

            var expression = ExpressionBuilder.BuildPredicate(expressionComparison.PropertyExpression, expressionComparison.CurrentQueryOperation, expressionComparison.Value);

            _expressionList.Add(ClauseWithExpression<T>.Create(expression, CurrentQueryClause));

            return this;
        }

        public Type GetPropertyType()
        {
            return typeof(TValue);
        }

        public QueryOperation GetQueryOperation()
        {
            return CurrentQueryOperation;
        }
    }

    public interface ICompareAndOrForCollection<RootModel>
    {
        public ICompareExpressionForCollection<RootModel> OrElse();
        public ICompareExpressionForCollection<RootModel> AndAlso();
        /// <summary>
        /// Combine the current expression with the next expression using the clause
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        ICompareExpressionForCollection<RootModel> CombineWith(QueryClause clause);
        /// <summary>
        /// Returns the combined expressions as a single expression
        /// </summary>
        /// <returns></returns>
        public Result<Expression<Func<RootModel, bool>>> AsExpressionResult();
    }
    public interface ICompareValueForCollection<RootModel>
    {
        ICompareAndOrForCollection<RootModel> WithAnyValue(object value);
    }
    
    public class ExpressionValueForCollection<RootModel,T, TValue> : ICompareValueForCollection<RootModel>, ICompareAndOrForCollection<RootModel>
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

        public ICompareAndOrForCollection<RootModel> WithAnyValue(object value)
        {

            _value = value;
            
            return this;
        }

        public ICompareExpressionForCollection<RootModel> CombineWith(QueryClause clause)
        {
            if (clause is QueryClause.And)
            {
                return AndAlso();
            }
                                                                                                                   
            return OrElse();
        }

        public ICompareExpressionForCollection<RootModel> OrElse()
        {
            var instance = _expressionComparison.AddExpressionsList(WithValue((TValue)_value));

            _expressionComparison.ChangeCurrentClause(QueryClause.Or);

            return instance;
        }

        public ICompareExpressionForCollection<RootModel> AndAlso()
        {
            var instance = _expressionComparison.AddExpressionsList(WithValue((TValue)_value));

            _expressionComparison.ChangeCurrentClause(QueryClause.And);

            return instance;
        
        }

        public Result<Expression<Func<RootModel, bool>>> AsExpressionResult()
        {
            _expressionComparison.AddExpressionsList(WithValue((TValue)_value));

            var result = _expressionComparison.AsExpression();

            if (result.IsFailure)
            {
                return Result.Failure<Expression<Func<RootModel, bool>>>(result.Error);
            }

            return Result.Success(PredicateBuilder.BuildCollectionPredicate(collectionSelector, result.Value, false));
        }
    }

    public interface IExpressionBuilderForCollection<RootModel>
    {
        IForCollectionBuilder<RootModel,T> ForCollection<T>(Expression<Func<RootModel, IEnumerable<T>>> collectionSelector, string rootDisplayName = null);
    }
}
