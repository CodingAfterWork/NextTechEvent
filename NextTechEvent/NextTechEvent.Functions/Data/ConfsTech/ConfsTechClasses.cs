using System;

namespace NextTechEvent.Function.Data.ConfsTech
{

    public class Rootobject
    {
        public Event[] events { get; set; }
    }

    public class Event
    {
        public string name { get; set; }
        public string url { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string twitter { get; set; }
        public string cfpUrl { get; set; }
        public DateTime? cfpEndDate { get; set; }
        public string? cocUrl { get; set; }
        public bool online { get; set; } = false;
    }

}
