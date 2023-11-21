using CoolFluentHelpers;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Newtonsoft.Json;
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
        public ITestOutputHelper _output { get; }

        public BasicTests(ITestOutputHelper output)
        {
            _output = output;
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
        public void Bind_through_list_test_with_and_clause_result(List<Person> people, QueryOperation operation, int age)
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
            var result = builder.FirstPropertyByDisplayName(propDerivedName.Body.ToString());

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

            Expression<Func<Person, bool>> expression = x => x.Age > 18 || x.Age < 80;

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

        [Theory]
        [MemberData(nameof(People))]
        public void only_if_some_condition_is_true(List<Person> people)
        {

            //arrange

            var builder = ExpressionBuilder<Person>.Create();
            var hasAPet = "does have a pet";

            var result = builder
                .ForProperty(x => x.Age)
                .OnlyIf(hasAPet == "does have a pet")
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .CombineWith(QueryClause.And)
                .Compare(QueryOperation.LessThan)
                .WithAnyValue(80)
                .AsExpressionResult();

            result.IsSuccess.Should().BeTrue();

            var expression = result.Value;

            Expression<Func<Person, bool>> expectedExpression = x => x.Age > 18 && x.Age < 80;

            expression.Should().BeEquivalentTo(expectedExpression);

            var normalResult = people.AsQueryable().Where(expectedExpression);

            var queryResult = people.AsQueryable().Where(expression);

            queryResult.Should().BeEquivalentTo(normalResult);

        }

        [Theory]
        [MemberData(nameof(People))]
        public void only_if_some_condition_is_false(List<Person> people)
        {

            //arrange

            var builder = ExpressionBuilder<Person>.Create();
            var hasAPet = "does NOT have a pet";

            var result = builder
                .ForProperty(x => x.Age)
                .OnlyIf(hasAPet == "does have a pet")
                .Compare(QueryOperation.GreaterThan)
                .WithAnyValue(18)
                .CombineWith(QueryClause.And)
                .Compare(QueryOperation.LessThan)
                .WithAnyValue(80)
                .AsExpressionResult();

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void We_can_retrive_the_property_type()
        {
            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Age);

            var type = agePropertyExp.GetPropertyType();

            type.Should().Be(typeof(int));
        }
        [Fact]
        public void we_can_get_all_properties_display_names()
        {
            var builder = ExpressionBuilder<Person>.Create();

            builder
                .ForProperty(x => x.Age, "Age").Compare(QueryOperation.GreaterThan);

            builder
                .ForProperty(x => x.Age, "Age").Compare(QueryOperation.LessThan);

            builder.ForProperty(x => x.FavoriteNumber, "FavoriteNumber");

            var dictionary = builder.GetPropertiesSupportedOperations();

            dictionary.Should().NotBeEmpty();

            dictionary.Keys.Should().Contain("Age", "FavoriteNumber");

        }

        public static IEnumerable<object[]> DateTimeLookUp()
        {
            var grandpa = new Person("John", DateTime.Now.Date.AddYears(-80));

            grandpa.FavoriteNumber = 3;

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

            mom.AddFamily(son, daughter, dad, grandpa);

            dad.AddFamily(son, daughter, mom, grandpa);

            var list = new List<Person> { dad, mom, son, daughter, kiddo, grandpa };

            yield return new object[] { list, QueryOperation.Equals, dad.Born };
            yield return new object[] { list, QueryOperation.NotEqual, dad.Born };
            yield return new object[] { list, QueryOperation.GreaterThan, son.Born };
            yield return new object[] { list, QueryOperation.GreaterThanOrEqual, son.Born };
            yield return new object[] { list, QueryOperation.LessThanOrEqual, son.Born };
            yield return new object[] { list, QueryOperation.LessThan, son.Born };

        }

        [Theory]
        [MemberData(nameof(DateTimeLookUp))]
        public void DateTime_Query_Operations(List<Person> people, QueryOperation operation, DateTime born)
        {
            //arrange

            var builder = ExpressionBuilder<Person>.Create();

            var agePropertyExp = builder
                .ForProperty(x => x.Born)
                .Compare(operation)
                .WithAnyValue(born);

            Expression<Func<Person, bool>> expression = null;

            switch (operation)
            {
                case QueryOperation.Equals:
                    expression = x => x.Born == born;
                    break;
                case QueryOperation.NotEqual:
                    expression = x => x.Born != born;
                    break;
                case QueryOperation.GreaterThan:
                    expression = x => x.Born > born;
                    break;
                case QueryOperation.GreaterThanOrEqual:
                    expression = x => x.Born >= born;
                    break;
                case QueryOperation.LessThanOrEqual:
                    expression = x => x.Born <= born;
                    break;
                case QueryOperation.LessThan:
                    expression = x => x.Born < born;
                    break;
                default:
                    break;
            }

            //act

            var expressionResult = agePropertyExp.AsExpressionResult();

            //assert

            expressionResult.IsSuccess.Should().BeTrue();

            var expression2 = expressionResult.Value;

            expression2.Should().BeEquivalentTo(expression);

            var normalResult = people.AsQueryable().Where(expression);

            var queryResult = people.AsQueryable().Where(expression2);

            normalResult.Should().BeEquivalentTo(queryResult);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                // Ignore reference loops instead of throwing an exception
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Now use these settings when serializing your object
            string json = JsonConvert.SerializeObject(queryResult.ToList(), settings);

            _output.WriteLine(json);
        }
        public static IEnumerable<object[]> PeopleWithPets()
        {
            var mom = new Person("Jane", DateTime.Now.AddYears(-30));
            mom.Pets.Add(new Pet("Fido1"));

            mom.FavoriteNumber = 7;

            var son = new Person("Bob", DateTime.Now.AddYears(-15));
            son.Pets.Add(new Pet("Fido2"));

            son.FavoriteNumber = 14;

            var daughter = new Person("Elisa", DateTime.Now.AddYears(-8));
            daughter.Pets.Add(new Pet("Fido3"));

            daughter.FavoriteNumber = 8;

            mom.AddFamily(son, daughter);
            mom.Pets.Add(new Pet("Fido4"));

            var list = new List<Person> { mom, son, daughter };

            yield return new object[] { list };
        }
        [Theory]
        [MemberData(nameof(PeopleWithPets))]
        public void Collections_test(List<Person> people)
        {
            //arrange
            var builder = ExpressionBuilder<Person>.ForCollections();
            
            var result = builder
                .ForCollection(x=>x.Pets)
                .ForProperty(x => x.Name)
                .OnlyIf(true)
                .Compare(QueryOperation.StartsWith)
                .WithAnyValue("Fi")
                .AndAlso()
                .Compare(QueryOperation.Contains)
                .WithAnyValue("2")
                .OrElse()
                .Compare(QueryOperation.EndsWith)
                .WithAnyValue("3")
                .AndAlso()
                .Compare(QueryOperation.Equals)
                .WithAnyValue("Fido4")
                .OrElse()
                .Compare(QueryOperation.EndsWith)
                .WithAnyValue("1")
                .AsExpressionResult();

            result.IsSuccess.Should().BeTrue();

            Expression<Func<Person, bool>> expression = x => x.Pets.Any(y=>y.Name.StartsWith("Fi") && y.Name.Contains("2") || y.Name.EndsWith("3") && y.Name.Equals("Fido4") || y.Name.EndsWith("1") );
            
            result.Value.Should().BeEquivalentTo(expression);
            
            var list = people.AsQueryable().Where(result.Value);

            var list2 = people.AsQueryable().Where(expression);

            list.Should().BeEquivalentTo(list2);

        }

        [Theory]
        [MemberData(nameof(PeopleWithPets))]
        public void single_collection_expression(List<Person> people)
        {
            //arrange
            var builder = ExpressionBuilder<Person>.ForCollections();

            var result = builder
                .ForCollection(x => x.Pets)
                .ForProperty(x => x.Name)
                .OnlyIf(true)
                .Compare(QueryOperation.EndsWith)
                .WithAnyValue("4")
                .AsExpressionResult();

            result.IsSuccess.Should().BeTrue();

            Expression<Func<Person, bool>> expression = x => x.Pets.Any(y => y.Name.EndsWith("4") );

            result.Value.Should().BeEquivalentTo(expression);

            var list = people.AsQueryable().Where(result.Value);

            var list2 = people.AsQueryable().Where(expression);

            list.Should().BeEquivalentTo(list2);

        }

        [Fact]
        public void for_collections_we_can_get_all_properties_display_names()
        {
            var builder = ExpressionBuilder<Person>.ForCollections();

            var petsCollection = builder.ForCollection(x => x.Pets);

            petsCollection.ForProperty(x => x.Name, "Pet Name").Compare(QueryOperation.Equals);

            petsCollection.ForProperty(x => x.Name, "Pet name starts with").Compare(QueryOperation.StartsWith);

            var dictionary = petsCollection.GetPropertiesSupportedOperations();

            dictionary.Should().NotBeEmpty();

            dictionary.Keys.Should().Contain("Pet Name");

            dictionary.Keys.Should().Contain("Pet name starts with");
        }


    }
}