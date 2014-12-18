using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TransFix.Extends
{
    public static class TemperatureSaveLoadEx
    {
        static Action applyLoadedDataToRegions;
        static TemperatureSaveLoadEx()
        {
            var empty = new ParameterExpression[0];
            var type = typeof(Verse.Map).Assembly.GetType("Verse.TemperatureSaveLoad");
            applyLoadedDataToRegions = Expression.Lambda<Action>(
                Expression.Call(type, "ApplyLoadedDataToRegions", null, empty),
                empty)
                .Compile();
            
            //TemperatureSaveLoad.ApplyLoadedDataToRegions();
        }

        public static void ApplyLoadedDataToRegions()
        {
            applyLoadedDataToRegions();
        }
    }
}
