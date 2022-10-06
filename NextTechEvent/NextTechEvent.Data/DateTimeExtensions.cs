using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextTechEvent.Data;

public static class DateTimeExtensions
{
    public static string ToClosingIn(this DateTime? date)
    {
        if (date != null)
        {
            var time = (date - DateTime.Now).Value;

            if (time.TotalDays < 1)
            {
                if (time.TotalHours < 1)
                {
                    if (time.TotalSeconds < 0)
                    {
                        return "Closed";
                    }
                    else
                    {
                        return Math.Round(time.TotalMinutes) + " minutes";
                    }
                }
                else
                {
                    return Math.Round(time.TotalHours) + " hours";
                }
            }
            else
            {
                return Math.Round(time.TotalDays) + " days";
            }
        }
        else
        {
            return "?";
        }
    }
}
