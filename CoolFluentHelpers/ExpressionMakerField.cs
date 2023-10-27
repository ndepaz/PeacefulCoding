using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace CoolFluentHelpers
{
    public class ExpressionMakerField<Model,Property>
    {
        private List<Expression<Func<Model, bool>>> _andBooleanExpression = new();
        private List<Expression<Func<Model, bool>>> _orBooleanExpression = new();

        public QueryOperation[] QueryOperations {get;}

        public string DisplayName {get;}

        private Expression<Func<Model,Property>> _expression {get;}

        private bool _criteria {get;set; } = true;

        private ExpressionMakerField(Expression<Func<Model, Property>> expression,string displayName, QueryOperation[] queryOperations)
        {
            QueryOperations = queryOperations;
            DisplayName = displayName;
            _expression = expression;
        }
    
        public static ExpressionMakerField<Model,Property> Bind(Expression<Func<Model, Property>> expression,string displayName,params QueryOperation[] queryOperations)
        {
            if (queryOperations != null && queryOperations.Any())
                return new ExpressionMakerField<Model, Property>(expression,displayName, queryOperations);
        
            if (typeof(Property) == typeof(string))
            {
                queryOperations = GetQueryOperationsForStrings();
            }
            else
            {
                queryOperations = GetQueryOperationsForOthers();
            }
        
            return new ExpressionMakerField<Model,Property>(expression,displayName,queryOperations);
        }

        public static QueryOperation[] GetQueryOperationsForStrings()
        {
            return new[]
            {
                QueryOperation.Equals,
                QueryOperation.Contains,
                QueryOperation.StartsWith,
                QueryOperation.EndsWith
            };
        }

        public static QueryOperation[] GetQueryOperationsForOthers()
        {
            return new[]
            {
                QueryOperation.Equals,
                QueryOperation.GreaterThan,
                QueryOperation.GreaterThanOrEqual,
                QueryOperation.LessThan,
                QueryOperation.LessThanOrEqual
            };
        }

        public ExpressionMakerField<Model, Property> AndIf(bool condition)
        {
            _criteria = condition;

            return this;
        }

        private bool MeetsCriteria()
        {
            return _criteria;
        }

        public Result<Expression<Func<Model,bool>>> ThenUseExpression(QueryOperation queryOperation, Property value)
        {
            if (!MeetsCriteria())
            {
                return Result.Failure<Expression<Func<Model,bool>>>($"Does not meets criteria");
            }

            if(!QueryOperations.Contains(queryOperation))
                return Result.Failure<Expression<Func<Model,bool>>>($"QueryOperation {queryOperation} is not supported for field {DisplayName}");

            var expression = ExpressionMaker.For<Model>().WithProperty(_expression).When(queryOperation).Value(value);
            
            if (!_andBooleanExpression.Contains(expression))
            {
                _andBooleanExpression.Add(expression);
            }
            
            return Result.Success(expression);
        }
        public ExpressionMakerField<Model, Property> WithAndExpression(QueryOperation queryOperation, Property value)
        {
            if (!MeetsCriteria())
            {
                return this;
            }

            if (!QueryOperations.Contains(queryOperation))
                return this;

            var expression = ExpressionMaker.For<Model>().WithProperty(_expression).When(queryOperation).Value(value);
            
            _andBooleanExpression.Add(expression);
            
            return this;
        }

        public ExpressionMakerField<Model, Property> WithOrExpression(QueryOperation queryOperation, Property value)
        {
            if (!MeetsCriteria())
            {
                return this;
            }

            if (!QueryOperations.Contains(queryOperation))
                return this;

            var expression = ExpressionMaker.For<Model>().WithProperty(_expression).When(queryOperation).Value(value);
            
            _orBooleanExpression.Add(expression);
            
            return this;
        }

        public Expression<Func<Model, bool>> AsExpression()
        {
            if (_andBooleanExpression == null && _orBooleanExpression == null)
                return x=>false;

            Expression<Func<Model, bool>> andExpression = x => true;
            
            var andCount = 0;
            
            foreach (var expression in _andBooleanExpression)
            {
                andCount++;
                andExpression = ExpressionHelperExtensions.AndAlso(andExpression, expression);
            }

            if (_orBooleanExpression == null || _orBooleanExpression.Count == 0)
                return andExpression;

            
            Expression<Func<Model, bool>> orExpression = x => false;
            
            foreach (var expression in _orBooleanExpression)
            {
                orExpression = ExpressionHelperExtensions.OrElse(orExpression, expression);
            }

            if(andCount == 0)
                return orExpression;

            return ExpressionHelperExtensions.AndAlso(andExpression, orExpression);
        }
    }

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
    }
    public class ModelFieldList<Model, Property>
    {
        private Dictionary<string,ExpressionMakerField<Model, Property>> _expressionsMakerFields = new();

        public static ModelFieldList<Model, Property> Create(ExpressionMakerField<Model, Property> expressionMakerField = null)
        {
            return new ModelFieldList<Model, Property>().Add(expressionMakerField);
        }


        public ModelFieldList<Model, Property> Add(ExpressionMakerField<Model, Property> expressionMakerField)
        {
            if(expressionMakerField == null)
                return this;

            _expressionsMakerFields.Add(expressionMakerField.DisplayName, expressionMakerField);

            return this;
        }

        public ModelFieldList<Model,Property> AddRange(params ExpressionMakerField<Model, Property>[] expressionMakerFields)
        {
            foreach(var expressionMakerField in expressionMakerFields)
            {
                Add(expressionMakerField);
            }

            return this;
        }

        public ExpressionMakerField<Model, Property> Bind(Expression<Func<Model, Property>> expression, string displayName, params QueryOperation[] queryOperations)
        {
            var expressionMakerField = ExpressionMakerField<Model, Property>.Bind(expression, displayName, queryOperations);

            AddRange(expressionMakerField);

            return expressionMakerField;
        }

        public Result<ExpressionMakerField<Model, Property>> FindBy(string displayName)
        {
            if (!_expressionsMakerFields.ContainsKey(displayName))
                return Result.Failure<ExpressionMakerField<Model, Property>>($"Field {displayName} not found");

            return Result.Success(_expressionsMakerFields[displayName]);

        }

        public IReadOnlyList<ExpressionMakerField<Model, Property>> ToList()
        {
            return _expressionsMakerFields.Values.ToList();
        }
    }
}
