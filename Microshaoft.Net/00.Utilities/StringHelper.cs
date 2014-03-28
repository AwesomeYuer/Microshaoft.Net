namespace Microshaoft
{
    using System;
    public static class StringHelper
    {
        public static bool IsValidString(string text)
        {
            return 
                    (
                        !string.IsNullOrEmpty(text)
#if NET45
                        && !string.IsNullOrWhiteSpace(text)
#endif
                    );
        }
    }
}