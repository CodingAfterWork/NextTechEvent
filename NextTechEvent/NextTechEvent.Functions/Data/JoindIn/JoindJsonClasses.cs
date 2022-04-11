using System;
using System.Collections.Generic;
using System.Text;

namespace NextTechEvent.Function.Data.JoindIn
{

    public class Rootobject
    {
        public Event[] events { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public int count { get; set; }
        public int total { get; set; }
        public string this_page { get; set; }
        public string next_page { get; set; }
    }


   

    public class Event
    {
        public string name { get; set; }
        public string url_friendly_name { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string description { get; set; }
        public object stub { get; set; }
        public string href { get; set; }
        public string icon { get; set; }
        public float? latitude { get; set; }
        public float? longitude { get; set; }
        public string tz_continent { get; set; }
        public string tz_place { get; set; }
        public string location { get; set; }
        public string contact_name { get; set; }
        public object hashtag { get; set; }
        public int attendee_count { get; set; }
        public bool attending { get; set; }
        public int event_average_rating { get; set; }
        public int comments_enabled { get; set; }
        public int event_comments_count { get; set; }
        public int tracks_count { get; set; }
        public int talks_count { get; set; }
        public DateTime? cfp_start_date { get; set; }
        public DateTime? cfp_end_date { get; set; }
        public string cfp_url { get; set; }
        public object rejection_reason { get; set; }
        public int talk_comments_count { get; set; }
        //public Images images { get; set; }
        public string[] tags { get; set; }
        public string uri { get; set; }
        public string verbose_uri { get; set; }
        public string comments_uri { get; set; }
        public string talks_uri { get; set; }
        public string tracks_uri { get; set; }
        public string attending_uri { get; set; }
        public string images_uri { get; set; }
        public int? pending { get; set; }
        public string website_uri { get; set; }
        public string all_talk_comments_uri { get; set; }
        //public Host[] hosts { get; set; }
        public string hosts_uri { get; set; }
        public string attendees_uri { get; set; }
        public bool can_edit { get; set; }
    }

    public class Images
    {
        public Orig orig { get; set; }
        public Small small { get; set; }
    }

    public class Orig
    {
        public string type { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Small
    {
        public string type { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Host
    {
        public string host_name { get; set; }
        public string host_uri { get; set; }
    }

}
