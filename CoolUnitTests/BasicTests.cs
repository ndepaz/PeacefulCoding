using CoolFluentHelpers;
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


            var fieldList = ModelFieldList<Person, int>.Create();
            var expWithProp = fieldList.Bind(x => x.FavoriteNumber, "Favorite #");
            
            var exp = expWithProp.ThenUseExpression(QueryOperation.Equals, 7);

            var exp2 = expWithProp.ThenUseExpression(QueryOperation.Equals, 7);

            var list = people.AsQueryable().Where(expWithProp.AsExpression()).ToList();

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

            mom.AddFamily(son,daughter);

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

            var list = new List<Person> { dad, mom, son, daughter,kiddo };

            yield return new object[] { list, QueryOperation.Equals, 30 };
            yield return new object[] { list, QueryOperation.GreaterThan, 15 };
            yield return new object[] { list, QueryOperation.GreaterThanOrEqual, 15 };
            yield return new object[] { list, QueryOperation.LessThanOrEqual, 8 };
            yield return new object[] { list, QueryOperation.LessThan, 8 };

        }

        [Theory]
        [MemberData(nameof(PeopleWithAge))]
        public void Bind_through_list_test_with_and_clause(List<Person> people,QueryOperation operation, int age)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int>.Create();

            fieldList.Bind(x=>x.Age,"Age")
                .WithAndExpression(operation,age);
            
            fieldList.Bind(x=>x.FavoriteNumber,"Favorite #")
                .WithAndExpression(QueryOperation.LessThanOrEqual,100)
                .WithAndExpression(QueryOperation.GreaterThan,0);

            foreach (var field in fieldList.ToList())
            {
                var queryResult = people.AsQueryable().Where(field.AsExpression()).ToList();

                queryResult.Should().NotBeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(PeopleWithAge))]
        public void Bind_through_list_test_with_or_clause(List<Person> people, QueryOperation operation, int age)
        {
            var fields = new List<ExpressionMakerField<Person, int>>();


            var fieldList = ModelFieldList<Person, int>.Create();

            fieldList.Bind(x => x.FavoriteNumber, "Favorite #")
                .WithOrExpression(QueryOperation.LessThanOrEqual, 100)
                .WithOrExpression(QueryOperation.GreaterThan, 0);

            foreach (var field in fieldList.ToList())
            {
                var queryResult = people.AsQueryable().Where(field.AsExpression()).ToList();

                queryResult.Should().NotBeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(People))]
        public void Equals_Equivalent2(List<Person> people)
        {
            var builder = ExpressionBuilder<Person>.Create();

            var expression2 = builder.Compare(x=>x.Age,
                AsQuery.Number<int>(QueryNumberOperation.GreaterThan),18);
            
            Expression<Func<Person, bool>> expression3 = x => x.Age > 18;
            
            //expression2.Should().BeEquivalentTo(expression3);

            //var fav1 = builder.Compare(x => x.FavoriteNumber,
            //    AsQuery.Number(QueryNumberOperation.GreaterThan), 18);

            //Expression<Func<Person, bool>> fav2 = x => x.FavoriteNumber > 18;

            //fav1.Should().BeEquivalentTo(fav2);

            var expression = builder.Compare(x => x.Name,
                AsQuery.String(QueryStringOperation.Equals), "Jane");

            var result = people.AsQueryable().FirstOrDefault(expression);

            result.Should().BeEquivalentTo(people[0]);

        }
    }



}