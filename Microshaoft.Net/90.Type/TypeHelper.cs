
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
                        && type.GetGenericTypeDefinition() == typeof(Nullable<>)
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
    }
}
