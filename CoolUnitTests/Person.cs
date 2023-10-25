using System;
using System.Collections.Generic;

namespace CoolUnitTests
{
    public class Person
    {
        public Person(string name, DateTime born)
        {
            Name = name;
            Born = born;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; set; }
        public DateTime Born { get; set; }
        public int Age => CalculateAge(Born);
        public static int CalculateAge(DateTime birthdate)
        {
            int age = DateTime.Today.Year - birthdate.Year;

            if (birthdate > DateTime.Today.AddYears(-age))
            {
                age--; // Adjust the age if the birthdate hasn't occurred yet this year
            }

            return age;
        }

        public List<Person> _relatives => new();
        public IReadOnlyList<Person> Relatives { get; set; }

        public void AddRelative(params Person[] people)
        {
            _relatives.AddRange(people);
        }
    }



}