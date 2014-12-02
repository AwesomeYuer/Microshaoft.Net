namespace Microshaoft
{
    using System;
    public static class TypeHelper
    {
        public static bool IsNullableType(Type type)
        {
            return
                (
                    (
                        type.IsGenericType
                        &&
                        type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    )
                );
        }

        public static Type GetNullableTypeUnderlyingType(Type type)
        {
            Type r = null;
            if (IsNullableType(type))
            {
                r = Nullable.GetUnderlyingType(type);
            }
            return r;
        }

        public static bool IsNumericType(Type type)
        {
            var typeCode = Type.GetTypeCode(type);
            return
                (
                    (
                        type.IsPrimitive
                        &&
                        type.IsValueType
                        && !type.IsEnum
                        && typeCode != TypeCode.Char
                        && typeCode != TypeCode.Boolean
                    )
                    ||
                    typeCode == TypeCode.Decimal
                );
        }

        public static bool IsNumericOrNullableNumericType(Type type)
        {
            return
                (
                    TypeHelper.IsNumericType(type)
                    ||
                    (
                        TypeHelper.IsNullableType(type)
                        &&
                        TypeHelper.IsNumericType
                                (
                //type.GetGenericArguments()[0]
                                    Nullable.GetUnderlyingType(type)
                                )
                    )
                );
        }
    }
    public static class TypesExtensionsMethodsManager
    {
        public static bool IsNullableType(this Type type)
        {
            return TypeHelper.IsNullableType(type);
        }

        public static Type GetNullableTypeUnderlyingType(this Type type)
        {
            return TypeHelper.GetNullableTypeUnderlyingType(type);
        }

        public static bool IsNumericType(this Type type)
        {
            return TypeHelper.IsNumericType(type);
        }

        public static bool IsNumericOrNullableNumericType(this Type type)
        {
            return TypeHelper.IsNumericOrNullableNumericType(type);
        }

    }
}
