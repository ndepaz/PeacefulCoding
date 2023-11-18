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
    //IExpressionBuilder<TModel>
    public class ExpressionBuilderForCollection<TModel,TValue> : IExpressionBuilderForCollection<TModel,TValue>
    {
        private readonly List<ICompareExpression<TModel>> _expressions = new();
        private Expression<Func<TModel, IEnumerable<TValue>>> expression;

        public ExpressionBuilderForCollection(Expression<Func<TModel, IEnumerable<TValue>>> expression, string displayName)
        {
            this.expression = expression;
            DisplayName = displayName;
        }

        public IReadOnlyList<ICompareExpression<TModel>> Expressions => _expressions.AsReadOnly();

        public string DisplayName { get; }

        public static IExpressionBuilder<TValue> Create<TModel, TValue>(Expression<Func<TModel, IEnumerable<TValue>>> propertyExpression, string displayName = null)
        {
            if (displayName is null)
            {
                displayName = propertyExpression.Body.ToString();
            }
            //TODO: to reimplement a version of the builder which can be used for collections
            return ExpressionBuilder<TValue>.Create();
        }
    }

    public interface IExpressionBuilderForCollection<Model, TCollection>
    {
        static abstract IExpressionBuilder<TValue> Create<TModel, TValue>(Expression<Func<TModel, IEnumerable<TValue>>> propertyExpression, string displayName = null);
    }
}
