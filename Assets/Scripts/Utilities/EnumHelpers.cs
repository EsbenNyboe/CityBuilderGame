using System;
using System.Linq;

namespace Utilities
{
    public class EnumHelpers
    {
        public static int GetMaxEnumValue<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<int>() // Cast the enum values to integers
                .Max(); // Get the maximum value
        }
    }
}