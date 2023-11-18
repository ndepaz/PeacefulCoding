using CSharpFunctionalExtensions;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CoolFluentHelpers
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ExpressionMaker instead of ExpressionBuilder<T>")]
    public class ExpressionMaker
    {
        public static ExpressionBuilderHelper<Model> For<Model>()
        {
            return new ExpressionBuilderHelper<Model>();
        }
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ExpressionMaker instead of ExpressionBuilder<T>")]
    public class ExpressionMaker<Model,Property>
    {
        private List<Expression<Func<Model, bool>>> _andBooleanExpression = new();
        private List<Expression<Func<Model, bool>>> _orBooleanExpression = new();

        public QueryOperation[] QueryOperations {get;}

        public string DisplayName {get;}

        private Expression<Func<Model,Property>> _expression {get;}

        private bool _criteria {get;set; } = true;

        private ExpressionMaker(Expression<Func<Model, Property>> expression,string displayName, QueryOperation[] queryOperations)
        {
            QueryOperations = queryOperations;
            DisplayName = displayName;
            _expression = expression;
        }
    
        public static ExpressionMaker<Model,Property> Bind(Expression<Func<Model, Property>> expression,string displayName,params QueryOperation[] queryOperations)
        {
            if (queryOperations != null && queryOperations.Any())
                return new ExpressionMaker<Model, Property>(expression,displayName, queryOperations);
        
            if (typeof(Property) == typeof(string))
            {
                queryOperations = GetQueryOperationsForStrings();
            }
            else
            {
                queryOperations = GetQueryOperationsForOthers();
            }
        
            return new ExpressionMaker<Model,Property>(expression,displayName,queryOperations);
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("AndIf is deprecated, please use OnlyIf instead for better clarity.")]
        public ExpressionMaker<Model, Property> AndIf(bool condition)
        {
            _criteria = condition;

            return this;
        }

        public ExpressionMaker<Model, Property> OnlyIf(bool condition)
        {
            _criteria = condition;

            return this;
        }

        private bool MeetsCriteria()
        {
            return _criteria;
        }

        public IResult<Expression<Func<Model,bool>>> ThenUseExpression(QueryOperation queryOperation, Property value)
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
        public ExpressionMaker<Model, Property> WithAndExpression(QueryOperation queryOperation, Property value)
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

        public ExpressionMaker<Model, Property> WithOrExpression(QueryOperation queryOperation, Property value)
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

        public IResult<Expression<Func<Model, bool>>> AsExpression()
        {
            if (_andBooleanExpression == null && _orBooleanExpression == null)
                return Result.Failure<Expression<Func<Model, bool>>>("There are no expressions to use");

            Expression<Func<Model, bool>> andExpression = x => true;
            
            var andCount = 0;
            
            foreach (var expression in _andBooleanExpression)
            {
                andCount++;
                andExpression = ExpressionHelperExtensions.AndAlso(andExpression, expression);
            }

            if (_orBooleanExpression == null || _orBooleanExpression.Count == 0)
                return Result.Success(andExpression);

            Expression<Func<Model, bool>> orExpression = x => false;
            
            foreach (var expression in _orBooleanExpression)
            {
                orExpression = ExpressionHelperExtensions.OrElse(orExpression, expression);
            }

            if(andCount == 0)
                return Result.Success(orExpression);

            return Result.Success(ExpressionHelperExtensions.AndAlso(andExpression, orExpression));
        }
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ExpressionMakerField<Model, Property> instead of ExpressionBuilder<T>")]
    public class ExpressionMakerField<Model, Property>
    {
        private List<Expression<Func<Model, bool>>> _andBooleanExpression = new();
        private List<Expression<Func<Model, bool>>> _orBooleanExpression = new();

        public QueryOperation[] QueryOperations { get; }

        public string DisplayName { get; }

        private Expression<Func<Model, Property>> _expression { get; }

        private bool _criteria { get; set; } = true;

        private ExpressionMakerField(Expression<Func<Model, Property>> expression, string displayName, QueryOperation[] queryOperations)
        {
            QueryOperations = queryOperations;
            DisplayName = displayName;
            _expression = expression;
        }

        public static ExpressionMakerField<Model, Property> Bind(Expression<Func<Model, Property>> expression, string displayName, params QueryOperation[] queryOperations)
        {
            if (queryOperations != null && queryOperations.Any())
                return new ExpressionMakerField<Model, Property>(expression, displayName, queryOperations);

            if (typeof(Property) == typeof(string))
            {
                queryOperations = GetQueryOperationsForStrings();
            }
            else
            {
                queryOperations = GetQueryOperationsForOthers();
            }

            return new ExpressionMakerField<Model, Property>(expression, displayName, queryOperations);
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

        [Obsolete("AndIf is deprecated, please use OnlyIf instead for better clarity.")]
        public ExpressionMakerField<Model, Property> AndIf(bool condition)
        {
            _criteria = condition;

            return this;
        }

        public ExpressionMakerField<Model, Property> OnlyIf(bool condition)
        {
            _criteria = condition;

            return this;
        }

        private bool MeetsCriteria()
        {
            return _criteria;
        }

        public IResult<Expression<Func<Model, bool>>> ThenUseExpression(QueryOperation queryOperation, Property value)
        {
            if (!MeetsCriteria())
            {
                return Result.Failure<Expression<Func<Model, bool>>>($"Does not meets criteria");
            }

            if (!QueryOperations.Contains(queryOperation))
                return Result.Failure<Expression<Func<Model, bool>>>($"QueryOperation {queryOperation} is not supported for field {DisplayName}");

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

        public IResult<Expression<Func<Model, bool>>> AsExpression()
        {
            if (_andBooleanExpression == null && _orBooleanExpression == null)
                return Result.Failure<Expression<Func<Model, bool>>>("There are no expressions to use");

            Expression<Func<Model, bool>> andExpression = x => true;

            var andCount = 0;

            foreach (var expression in _andBooleanExpression)
            {
                andCount++;
                andExpression = ExpressionHelperExtensions.AndAlso(andExpression, expression);
            }

            if (_orBooleanExpression == null || _orBooleanExpression.Count == 0)
                return Result.Success(andExpression);

            Expression<Func<Model, bool>> orExpression = x => false;

            foreach (var expression in _orBooleanExpression)
            {
                orExpression = ExpressionHelperExtensions.OrElse(orExpression, expression);
            }

            if (andCount == 0)
                return Result.Success(orExpression);

            return Result.Success(ExpressionHelperExtensions.AndAlso(andExpression, orExpression));
        }
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ModelFieldList<Model, Property> instead of ExpressionBuilder<T>")]
    public class ModelFieldList<Model, Property>
    {
        private Dictionary<string,ExpressionMaker<Model, Property>> _expressionsMakerFields = new();

        public static ModelFieldList<Model, Property> Create(ExpressionMaker<Model, Property> expressionMakerField = null)
        {
            return new ModelFieldList<Model, Property>().Add(expressionMakerField);
        }


        public ModelFieldList<Model, Property> Add(ExpressionMaker<Model, Property> expressionMakerField)
        {
            if(expressionMakerField == null)
                return this;

            _expressionsMakerFields.Add(expressionMakerField.DisplayName, expressionMakerField);

            return this;
        }

        public ModelFieldList<Model,Property> AddRange(params ExpressionMaker<Model, Property>[] expressionMakerFields)
        {
            foreach(var expressionMakerField in expressionMakerFields)
            {
                Add(expressionMakerField);
            }

            return this;
        }

        public ExpressionMaker<Model, Property> Bind(Expression<Func<Model, Property>> expression, string displayName, params QueryOperation[] queryOperations)
        {
            var expressionMakerField = ExpressionMaker<Model, Property>.Bind(expression, displayName, queryOperations);

            AddRange(expressionMakerField);

            return expressionMakerField;
        }

        public IResult<ExpressionMaker<Model, Property>> FindBy(string displayName)
        {
            if (!_expressionsMakerFields.ContainsKey(displayName))
                return Result.Failure<ExpressionMaker<Model, Property>>($"Field {displayName} not found");

            return Result.Success(_expressionsMakerFields[displayName]);

        }

        public IReadOnlyList<ExpressionMaker<Model, Property>> ToList()
        {
            return _expressionsMakerFields.Values.ToList();
        }

        public IReadOnlyList<Expression<Func<Model, bool>>> GetValidExpressions(){
            
            var validExpressions = new List<Expression<Func<Model, bool>>>();

            foreach (var field in ToList())
            {
                var result = field.AsExpression();

                if (result.IsSuccess)
                {
                    validExpressions.Add(result.Value);
                }
            }

            return validExpressions;
        }
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ExpressionBuilderHelper<T> instead of ExpressionBuilder<T>")]
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ExpressionBuilderHelper<T> instead of ExpressionBuilder<T>")]
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
}
