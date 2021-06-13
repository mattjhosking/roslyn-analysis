using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynAnalysis.Extensions
{
    public static class LookupExtensions
    {
        public static IEnumerable<T> GetLookupValueOrEmpty<T>(this ILookup<string, T> lookup, string key)
        {
            return lookup.Contains(key) ? lookup[key] : Array.Empty<T>();
        }
    }
}
