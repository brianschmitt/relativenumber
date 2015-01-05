using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RelativeNumber
{
    public static class ResourceDictionaryExtensions
    {
        public static T GetValue<T>(this ResourceDictionary dictionary, object key, T defaultValue)
        {
            if (dictionary.Contains(key))
            {
                return (T)dictionary[key];
            }
            return defaultValue;
        }
    }
}