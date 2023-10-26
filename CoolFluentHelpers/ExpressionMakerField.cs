using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace CoolFluentHelpers
{
    public class ExpressionMakerField<Model,Property>
    {
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
            return new[] { QueryOperation.Equals, QueryOperation.Contains, QueryOperation.StartsWith, QueryOperation.EndsWith };
        }

        public static QueryOperation[] GetQueryOperationsForOthers()
        {
            return new[] { QueryOperation.Equals, QueryOperation.GreaterThan, QueryOperation.LessThan };
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

            return Result.Success(ExpressionMaker.For<Model>().WithProperty(_expression).When(queryOperation).Value(value));
        }
    }
}
