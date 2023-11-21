# PeacefulCoding

You can use ExpressionBuilder to allow you to create dynamic Expressions.

This is useful when you need to pass arbitrary comparisons to a query while keeping a stronlgy typed approach.

There is still more to come, but for now you can use it to create simple expressions with direct references to properties.

QueryOperation are meant to be controlled by the developer as there isn't a easy way to narrow them by property type just yet.

Now, IEnumerables expressions are supported.

## Simple usage in UnitTest form
Visit the unit tests project to learn more

```csharp
//arrange
var builder = ExpressionBuilder<Person>.Create();

var agePropertyExp = builder
    .ForProperty(x => x.Age)
    .Compare(QueryOperation.GreaterThan)
    .WithAnyValue(18)
    .CombineWith(QueryClause.And)
    .Compare(QueryOperation.LessThan)
    .WithAnyValue(80);

Expression<Func<Person, bool>> expectedExpression = x => x.Age > 18 && x.Age < 80;

//act

var expressionResult = agePropertyExp.AsExpressionResult();

//assert

expressionResult.IsSuccess.Should().BeTrue();

var expression = expressionResult.Value;

expression.Should().BeEquivalentTo(expectedExpression);

```

## Simple usage in UnitTest form with collections

```csharp
var builder = ExpressionBuilder<Person>.ForCollections();

var result = builder
    .ForCollection(x => x.Pets)
    .ForProperty(x => x.Name)
    .OnlyIf(true)
    .Compare(QueryOperation.EndsWith)
    .WithAnyValue("4")
    .AsAnyExpressionResult();

result.IsSuccess.Should().BeTrue();

Expression<Func<Person, bool>> stronglyTypedVersionExpression = x => x.Pets.Any(y => y.Name.EndsWith("4") );

result.Value.Should().BeEquivalentTo(stronglyTypedVersionExpression);

var list = people.AsQueryable().Where(result.Value);

var list2 = people.AsQueryable().Where(stronglyTypedVersionExpression);

list.Should().BeEquivalentTo(list2);
```

### You can chain different combinations of clauses and operations to create complex expressions.

```csharp
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
    .AsAnyExpressionResult();

result.IsSuccess.Should().BeTrue();

Expression<Func<Person, bool>> stronglyTypedVersionExpression = x => x.Pets.Any(y=>y.Name.StartsWith("Fi") && y.Name.Contains("2") || y.Name.EndsWith("3") && y.Name.Equals("Fido4") || y.Name.EndsWith("1") );
            
result.Value.Should().BeEquivalentTo(stronglyTypedVersionExpression);
            
var list = people.AsQueryable().Where(result.Value);

var list2 = people.AsQueryable().Where(stronglyTypedVersionExpression);

list.Should().BeEquivalentTo(list2);

```

## Release Notes

### v3.13.12
- Added support for Collections allowing you to create expressions for multiple properties under the same collection.
  - In addition adds support for the following operations under collections:
    - AsAnyExpressionResult
    - AsAllExpressionResult
- Corrected the order of operations by implementing the necessary changes in the affected code.
- Conducted comprehensive testing to verify the fix and ensure proper functionality.
- With this correction, the package now executes operations according to the correct order, ensuring accurate results and expected behavior.

## Installation

You can install the package from NuGet:

```
NuGet\Install-Package CoolFluentHelpers
```
Or via the .NET Core CLI:

```
dotnet add package CoolFluentHelpers
```