using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextTechEvent.Data;

public class ConferenceUserStatus
{
    public string UserId { get; set; }
    public StateEnum State { get; set; }
    public string ConferenceName { get; set; }
    public DateOnly EventStart { get; set; }
    public string ConferenceId { get; set; }    
}
