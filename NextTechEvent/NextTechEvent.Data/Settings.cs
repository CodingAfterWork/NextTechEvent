using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextTechEvent.Data
{
    public class Settings
    {
        public required string UserId { get; set; }
        public string? SessionizeCalendarUrl { get; set; }
        public string? Id { get; set; }
    }
}

