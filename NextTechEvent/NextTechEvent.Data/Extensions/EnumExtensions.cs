using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NextTechEvent.Data.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayDescription(this Enum enumValue)
    {
        var enumValueAsString = enumValue.ToString();
        var val = enumValue.GetType().GetMember(enumValueAsString).FirstOrDefault();

        return val?.GetCustomAttribute<DisplayAttribute>()?.GetDescription() ?? enumValueAsString;
    }
}