namespace CoolFluentHelpers
{
    internal static class QueryOperationConverter
    {

        internal static QueryOperation Convert(QueryString operation)
        {
            return operation switch
            {
                QueryString.StartsWith => QueryOperation.StartsWith,
                QueryString.EndsWith => QueryOperation.EndsWith,
                QueryString.Contains => QueryOperation.Contains,
                QueryString.Equals => QueryOperation.Equals,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryNumber operation)
        {
            return operation switch
            {
                QueryNumber.Equals => QueryOperation.Equals,
                QueryNumber.LessThan => QueryOperation.LessThan,
                QueryNumber.LessThanOrEqual => QueryOperation.LessThanOrEqual,
                QueryNumber.GreaterThan => QueryOperation.GreaterThan,
                QueryNumber.GreaterThanOrEqual => QueryOperation.GreaterThanOrEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryDate operation)
        {
            return operation switch
            {
                QueryDate.Equals => QueryOperation.Equals,
                QueryDate.LessThan => QueryOperation.LessThan,
                QueryDate.LessThanOrEqual => QueryOperation.LessThanOrEqual,
                QueryDate.GreaterThan => QueryOperation.GreaterThan,
                QueryDate.GreaterThanOrEqual => QueryOperation.GreaterThanOrEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryBool operation)
        {
            return operation switch
            {
                QueryBool.Equals => QueryOperation.Equals,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }
    }
}