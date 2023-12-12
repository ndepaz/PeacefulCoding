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
        public int? FavoriteNumber { get; set; }
        public List<Pet> Pets { get; set; } = new();
        public static int CalculateAge(DateTime birthdate)
        {
            int age = DateTime.Today.Year - birthdate.Year;

            if (birthdate > DateTime.Today.AddYears(-age))
            {
                age--; // Adjust the age if the birthdate hasn't occurred yet this year
            }

            return age;
        }

        private List<Person> _family = new();
        public IReadOnlyList<Person> Familiy => _family.AsReadOnly();

        public void AddFamily(params Person[] people)
        {
            _family.AddRange(people);
        }

        public PhoneModel PrimaryPhoneModel { get; set; }

    }

    public class PhoneModel
    {
        public string Name { get; }
        private PhoneModel(string name)
        {
            Name = name;
        }

        public static PhoneModel Iphone = new("iphone");
        public static PhoneModel Android = new("android");
        public static PhoneModel Windows = new("windows");

        public static implicit operator string(PhoneModel model)
        {
            return model.Name;
        }
    }

    public class Pet
    {
        public Pet()
        {

        }
        public string Name { get; }

        public Pet(string name)
        {
            Name = name;
        }


    }

}