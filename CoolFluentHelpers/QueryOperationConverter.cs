namespace CoolFluentHelpers
{
    internal static class QueryOperationConverter
    {

        internal static QueryOperation Convert(QueryStringOperation operation)
        {
            return operation switch
            {
                QueryStringOperation.StartsWith => QueryOperation.StartsWith,
                QueryStringOperation.EndsWith => QueryOperation.EndsWith,
                QueryStringOperation.Contains => QueryOperation.Contains,
                QueryStringOperation.Equals => QueryOperation.Equals,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryNumberOperation operation)
        {
            return operation switch
            {
                QueryNumberOperation.Equals => QueryOperation.Equals,
                QueryNumberOperation.LessThan => QueryOperation.LessThan,
                QueryNumberOperation.LessThanOrEqual => QueryOperation.LessThanOrEqual,
                QueryNumberOperation.GreaterThan => QueryOperation.GreaterThan,
                QueryNumberOperation.GreaterThanOrEqual => QueryOperation.GreaterThanOrEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryDateOperation operation)
        {
            return operation switch
            {
                QueryDateOperation.Equals => QueryOperation.Equals,
                QueryDateOperation.LessThan => QueryOperation.LessThan,
                QueryDateOperation.LessThanOrEqual => QueryOperation.LessThanOrEqual,
                QueryDateOperation.GreaterThan => QueryOperation.GreaterThan,
                QueryDateOperation.GreaterThanOrEqual => QueryOperation.GreaterThanOrEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        internal static QueryOperation Convert(QueryBoolOperation operation)
        {
            return operation switch
            {
                QueryBoolOperation.Equals => QueryOperation.Equals,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }
    }
}