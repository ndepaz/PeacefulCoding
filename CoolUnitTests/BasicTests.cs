using CoolFluentHelpers;
using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoolUnitTests
{
    public class BasicTests
    {
        [Theory]
        [MemberData(nameof(People))]
        public void Equals_Equivalent(List<Person> people)
        {
            var expression = ExpressionMaker.For<Person>().WithProperty(x => x.Name).Equals("Jane");

            var result = people.AsQueryable().FirstOrDefault(expression);

            result.Should().BeEquivalentTo(people[0]);

            var expression2 = ExpressionMaker.For<Person>().WithProperty(x => x.Name).When(QueryOperation.Equals).Value("Jane");
        }

        public static IEnumerable<object[]> People()
        {
            var mom = new Person("Jane", DateTime.Now.AddYears(-30));

            var son = new Person("Bob", DateTime.Now.AddYears(-15));

            var daughter = new Person("Elisa", DateTime.Now.AddYears(-8));

            mom.AddRelative(son,daughter);

            var list = new List<Person> { mom, son, daughter };

            yield return new object[] { list };
        }
    }



}