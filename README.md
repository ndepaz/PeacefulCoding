# PeacefulCoding

You can use Expression Maker to allow you to create dynamic Expressions.

This is useful when you need to pass arbitrary comparisons to a query while keeping a stronlgy typed approach.

There is still more to come, but for now you can use it to create simple expressions with direct references to properties.

IEnumerables are not yet supported, but will be soon.

## Example

```csharp
var expression = ExpressionMaker.For<Person>().On(x => x.Name).Equals("Jane");

var expression2 = ExpressionMaker.For<Person>().On(x => x.Name).When(QueryOperation.Equals).Value("Jane");

var expression3 = ExpressionMaker.For<Person>().On(x => x.Name).When(QueryOperation.StartsWith).Value("Ja");

var expression4 = ExpressionMaker.For<Person>().On(x => x.Name).When(QueryOperation.Contains).Value("a");

```