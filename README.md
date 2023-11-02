# PeacefulCoding

You can use Expression Maker to allow you to create dynamic Expressions.

This is useful when you need to pass arbitrary comparisons to a query while keeping a stronlgy typed approach.

There is still more to come, but for now you can use it to create simple expressions with direct references to properties.

QueryOperation are meant to be controlled by the developer as there isn't a easy way to narrow them by property type just yet.

IEnumerables are not yet supported, but will be soon.

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

## Installation

You can install the package from NuGet:

```
NuGet\Install-Package CoolFluentHelpers
```
Or via the .NET Core CLI:

```
dotnet add package CoolFluentHelpers
```
