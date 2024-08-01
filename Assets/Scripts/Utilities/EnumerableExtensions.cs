using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utilities
{
    public static class EnumerableExtensions
    {
        public static bool IsEmptyOrNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || enumerable.IsEmpty();
        }
        
        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }
    }
}