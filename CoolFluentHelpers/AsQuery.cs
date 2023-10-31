using System.Numerics;

namespace CoolFluentHelpers
{
    public class AsQuery<T> : AsQuery
    {
        private AsQuery(QueryOperation queryOperation) : base(queryOperation)
        {
        }

        public static AsQuery<string> String<TValue>(QueryString operation) where TValue : class
        {
            return new AsQuery<string>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<TValue> Numeric<TValue>(QueryNumber operation) where TValue : INumber<TValue>
        {
            return new AsQuery<TValue>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<TValue?> NullableValue<TValue>(QueryNumber operation) where TValue : struct
        {
            return new AsQuery<TValue?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<int?> NullableInt(QueryNumber operation)
        {
            return new AsQuery<int?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<decimal?> NullableDecimal(QueryNumber operation)
        {
            return new AsQuery<decimal?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<double?> NullableDouble(QueryNumber operation)
        {
            return new AsQuery<double?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<float?> NullableFloat(QueryNumber operation)
        {
            return new AsQuery<float?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<long?> NullableLong(QueryNumber operation)
        {
            return new AsQuery<long?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<short?> NullableShort(QueryNumber operation)
        {
            return new AsQuery<short?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<DateTime> Date<TValue>(QueryDate operation) where TValue : struct
        {
            return new AsQuery<DateTime>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<DateTime?> Date(QueryDate operation)
        {
            return new AsQuery<DateTime?>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<bool> Bool<TValue>(QueryBool operation) where TValue : struct
        {
            return new AsQuery<bool>(QueryOperationConverter.Convert(operation));
        }

        public static AsQuery<bool?> Bool(QueryBool operation)
        {
            return new AsQuery<bool?>(QueryOperationConverter.Convert(operation));
        }

    }
}
