using CoolFluentHelpers;
using CSharpFunctionalExtensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace CoolUnitTests
{
    public class BasicTests
    {
        public ITestOutputHelper Output { get; }

        public BasicTests(ITestOutputHelper output)
        {
            Output = output;
        }
        [Theory]
        [MemberData(nameof(People))]
        public void Equals_Equivalent(List<Person> people)
        {
            var expression = ExpressionMaker.For<Person>().WithProperty(x => x.Name).Equals("Jane");

            var result = people.AsQueryable().FirstOrDefault(expression);

            result.Should().BeEquivalentTo(people[0]);

            var expression2 = ExpressionMaker.For<Person>().WithProperty(x => x.Name).When(QueryOperation.Equals).Value("Jane");
        }

        [Theory]
        [MemberData(nameof(People))]
        public void Backwards_compatible(List<Person> people)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int?>.Create();
            var expWithProp = fieldList.Bind(x => x.FavoriteNumber, "Favorite #");

            var exp = expWithProp.ThenUseExpression(QueryOperation.Equals, 7);

            var exp2 = expWithProp.ThenUseExpression(QueryOperation.Equals, 7);

            var result = expWithProp.AsExpression();

            result.IsSuccess.Should().BeTrue();

            var list = people.AsQueryable().Where(result.Value).ToList();

            list.Should().NotBeEmpty();
        }

        public static IEnumerable<object[]> People()
        {
            var mom = new Person("Jane", DateTime.Now.AddYears(-30));

            mom.FavoriteNumber = 7;

            var son = new Person("Bob", DateTime.Now.AddYears(-15));

            son.FavoriteNumber = 14;

            var daughter = new Person("Elisa", DateTime.Now.AddYears(-8));

            daughter.FavoriteNumber = 8;

            mom.AddFamily(son, daughter);

            var list = new List<Person> { mom, son, daughter };

            yield return new object[] { list };
        }

        public static IEnumerable<object[]> PeopleWithAge()
        {
            var dad = new Person("John", DateTime.Now.Date.AddYears(-40));

            dad.FavoriteNumber = 3;

            var mom = new Person("Jane", DateTime.Now.Date.AddYears(-30));

            mom.FavoriteNumber = 7;

            var son = new Person("Bob", DateTime.Now.Date.AddYears(-15));

            son.FavoriteNumber = 14;

            var daughter = new Person("Elisa", DateTime.Now.Date.AddYears(-8));

            daughter.FavoriteNumber = 8;

            var kiddo = new Person("Bart", DateTime.Now.Date.AddYears(-4));

            daughter.FavoriteNumber = 3;

            mom.AddFamily(son, daughter, dad);

            dad.AddFamily(son, daughter, mom);

            var list = new List<Person> { dad, mom, son, daughter, kiddo };

            yield return new object[] { list, QueryOperation.Equals, 30 };
            yield return new object[] { list, QueryOperation.GreaterThan, 15 };
            yield return new object[] { list, QueryOperation.GreaterThanOrEqual, 15 };
            yield return new object[] { list, QueryOperation.LessThanOrEqual, 8 };
            yield return new object[] { list, QueryOperation.LessThan, 8 };

        }

        [Theory]
        [MemberData(nameof(PeopleWithAge))]
        public void Bind_through_list_test_with_and_clause(List<Person> people, QueryOperation operation, int age)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int?>.Create();

            fieldList.Bind(x => x.Age, "Age")
                .WithAndExpression(operation, age);

            fieldList.Bind(x => x.FavoriteNumber, "Favorite #")
                .WithAndExpression(QueryOperation.LessThanOrEqual, 100)
                .WithAndExpression(QueryOperation.GreaterThan, 0);

            foreach (var field in fieldList.ToList())
            {
                var result = field.AsExpression();

                result.IsSuccess.Should().BeTrue();

                var queryResult = people.AsQueryable().Where(result.Value).ToList();

                queryResult.Should().NotBeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(PeopleWithAge))]
        public void Bind_through_list_test_with_and_clause_result(List<Person> people,QueryOperation operation, int age)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int?>.Create();

            fieldList.Bind(x=>x.Age,"Age")
                .WithAndExpression(operation,age);
            
            fieldList.Bind(x=>x.FavoriteNumber,"Favorite #")
                .WithAndExpression(QueryOperation.LessThanOrEqual,100)
                .WithAndExpression(QueryOperation.GreaterThan,0);

            foreach (var field in fieldList.ToList())
            {
                var result = field.AsExpression();

                result.IsSuccess.Should().BeTrue();

                var queryResult = people.AsQueryable().Where(result.Value).ToList();

                queryResult.Should().NotBeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(PeopleWithAge))]
        public void Bind_through_list_test_with_or_clause(List<Person> people, QueryOperation operation, int age)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int?>.Create();

            fieldList.Bind(x => x.FavoriteNumber, "Favorite #")
                .WithOrExpression(QueryOperation.LessThanOrEqual, 100)
                .WithOrExpression(QueryOperation.GreaterThan, 0);

            foreach (var field in fieldList.ToList())
            {
                var result = field.AsExpression();

                result.IsSuccess.Should().BeTrue();

                var queryResult = people.AsQueryable().Where(result.Value).ToList();

                queryResult.Should().NotBeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(People))]
        public void Equals_Equivalent2(List<Person> people)
        {
            var builder = ExpressionBuilder<Person>.Create();
            var agePropertyExp = builder
                .ForProperty(x => x.Age, "Age");

            var expression = agePropertyExp
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .AsExpressionResult().Value;

            Expression<Func<Person, bool>> expression2 = x => x.Age > 18;

            expression.Should().BeEquivalentTo(expression2);

            var expression3 = builder.ForProperty(x => x.FavoriteNumber, "Favorite Number #")
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .AsExpressionResult().Value;

            Expression<Func<Person, bool>> expression4 = x => x.FavoriteNumber > 18;

            expression3.Should().BeEquivalentTo(expression4);

        }


        public static IEnumerable<object[]> PeopleWithDiffAge()
        {
            var dad = new Person("John", DateTime.Now.Date.AddYears(-40));

            dad.FavoriteNumber = 3;

            var mom = new Person("Jane", DateTime.Now.Date.AddYears(-30));

            mom.FavoriteNumber = 7;

            var son = new Person("Bob", DateTime.Now.Date.AddYears(-15));

            son.FavoriteNumber = 14;

            var daughter = new Person("Elisa", DateTime.Now.Date.AddYears(-8));

            daughter.FavoriteNumber = 8;

            var kiddo = new Person("Bart", DateTime.Now.Date.AddYears(-4));

            daughter.FavoriteNumber = 3;

            mom.AddFamily(son, daughter, dad);

            dad.AddFamily(son, daughter, mom);

            var list = new List<Person> { dad, mom, son, daughter, kiddo };
            Expression<Func<Person, bool>> expression18 = x => x.Age == 18;
            Expression<Func<Person, bool>> expression15 = x => x.Age > 15;
            Expression<Func<Person, bool>> expressionGE15 = x => x.Age >= 15;
            Expression<Func<Person, bool>> expressionLE8 = x => x.Age <= 8;
            Expression<Func<Person, bool>> expressionL8 = x => x.Age < 8;

            yield return new object[] { list, QueryOperation.Equals, 18, expression18 };
            yield return new object[] { list, QueryOperation.GreaterThan, 15, expression15 };
            yield return new object[] { list, QueryOperation.GreaterThanOrEqual, 15, expressionGE15 };
            yield return new object[] { list, QueryOperation.LessThanOrEqual, 8, expressionLE8 };
            yield return new object[] { list, QueryOperation.LessThan, 8, expressionL8 };

        }

        [Theory]
        [MemberData(nameof(PeopleWithDiffAge))]
        public void Multiple_equivalent_expressions(List<Person> people, QueryOperation queryOperation, int value, Expression<Func<Person, bool>> expectedExpression)
        {
            var builder = ExpressionBuilder<Person>.Create();
            var agePropertyExp = builder
                .ForProperty(x => x.Age, "Age");

            var expression = agePropertyExp
                .Compare(queryOperation)
                .WithAnyValue(value)
                .AsExpressionResult().Value;

            var normalResult = people.AsQueryable().Where(expectedExpression);

            var queryResult = people.AsQueryable().Where(expression);

            queryResult.Should().BeEquivalentTo(normalResult);
        }

        [Theory]
        [MemberData(nameof(People))]
        public void We_can_aggregate_expressions(List<Person> people)
        {
            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age, "Age");

            var expressionResult = agePropertyExp
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .AndAlso()
                .Compare(QueryOperation.LessThan)
                .WithAnyValue(100)
                .AsExpressionResult();

            expressionResult.IsSuccess.Should().BeTrue();

            var expression = expressionResult.Value;

            Expression<Func<Person, bool>> expectedExpression = x => x.Age > 18 && x.Age < 100;

            expression.Should().BeEquivalentTo(expectedExpression);

            var normalResult = people.AsQueryable().Where(expectedExpression);

            var queryResult = people.AsQueryable().Where(expression);

            queryResult.Should().BeEquivalentTo(normalResult);
        }

        [Theory]
        [MemberData(nameof(PeopleWithDiffAge))]
        public void We_can_find_and_evaluate_a_property(List<Person> people, QueryOperation queryOperation, object value, Expression<Func<Person, bool>> expectedExpression)
        {
            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age, "Age");

            //act 
            var result = builder.FindByPropertyByDisplayName("Age");

            result.IsSuccess.Should().BeTrue();

            var property = result.Value;

            property.Should().BeEquivalentTo(agePropertyExp);

            var expressionResult = property
                .Compare(queryOperation)
                .WithAnyValue(value)
                .AsExpressionResult();

            //assert
            expressionResult.IsSuccess.Should().BeTrue();

            var expression = expressionResult.Value;

            expression.Should().BeEquivalentTo(expectedExpression);

            var normalResult = people.AsQueryable().Where(expectedExpression);

            var queryResult = people.AsQueryable().Where(expression);

            queryResult.Should().BeEquivalentTo(normalResult);
        }

        [Theory]
        [MemberData(nameof(People))]
        public void A_property_can_have_a_predefined_comparison(List<Person> people)
        {
            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age, "Age");

            agePropertyExp.Compare(QueryOperation.GreaterThan);

            //act 
            var result = builder.FindByPropertyByDisplayName("Age");

            result.IsSuccess.Should().BeTrue();

            var property = result.Value;

            property.Should().BeEquivalentTo(agePropertyExp);

            var expressionResult = property
                .CompareWithDefault()
                .WithAnyValue(18)
                .AsExpressionResult();

            //assert
            expressionResult.IsSuccess.Should().BeTrue();

            var expression = expressionResult.Value;

            Expression<Func<Person, bool>> expectedExpression = x => x.Age > 18;

            expression.Should().BeEquivalentTo(expectedExpression);

            var normalResult = people.AsQueryable().Where(expectedExpression);

            var queryResult = people.AsQueryable().Where(expression);

            queryResult.Should().BeEquivalentTo(normalResult);
        }

        [Theory]
        [MemberData(nameof(People))]
        public void When_property_display_name_a_default_value_is_given(List<Person> people)
        {
            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age);

            agePropertyExp.Compare(QueryOperation.GreaterThan);

            Expression<Func<Person, int>> propDerivedName = x => x.Age;  

            //act 
            var result = builder.FindByPropertyByDisplayName(propDerivedName.Body.ToString());

            result.IsSuccess.Should().BeTrue();

            var property = result.Value;

            property.PropertyDisplayName.Should().BeEquivalentTo(propDerivedName.Body.ToString());
        }

        [Theory]
        [MemberData(nameof(People))]
        public void properties_queries_can_have_Or_clauses(List<Person> people)
        {

            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age)
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .CombineWith(QueryClause.Or)
                .Compare(QueryOperation.LessThan)
                .WithAnyValue(80);

            Expression<Func<Person,bool>> expression = x => x.Age > 18 || x.Age < 80;

            //act

            var expressionResult = agePropertyExp.AsExpressionResult();

            //assert

            expressionResult.IsSuccess.Should().BeTrue();

            var expression2 = expressionResult.Value;

            expression2.Should().BeEquivalentTo(expression);

            var normalResult = people.AsQueryable().Where(expression);

            var queryResult = people.AsQueryable().Where(expression2);

            normalResult.Should().BeEquivalentTo(queryResult);
        }

        [Theory]
        [MemberData(nameof(People))]
        public void properties_queries_can_have_And_clauses(List<Person> people)
        {

            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age)
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .CombineWith(QueryClause.And)
                .Compare(QueryOperation.LessThan)
                .WithAnyValue(80);

            Expression<Func<Person, bool>> expression = x => x.Age > 18 && x.Age < 80;

            //act

            var expressionResult = agePropertyExp.AsExpressionResult();

            //assert

            expressionResult.IsSuccess.Should().BeTrue();

            var expression2 = expressionResult.Value;

            expression2.Should().BeEquivalentTo(expression);

            var normalResult = people.AsQueryable().Where(expression);

            var queryResult = people.AsQueryable().Where(expression2);
        }
    }



}