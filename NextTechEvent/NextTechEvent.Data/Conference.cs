﻿using NextTechEvent.Data.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace NextTechEvent.Data;

public class Conference
{
    public string? Id { get; set; }
    [Required, MinLength(3)]
    [Display(Name = "Conference name")]
    public string Name { get; set; } = "";
    [Url]
    public string? ImageUrl { get; set; }
    [Url]
    [Display(Name = "Call for paper URL")]
    public string? CfpUrl { get; set; }
    [Display(Name = "Call for paper opens")]
    public DateTime? CfpStartDate { get; set; } = DateTime.Now;
    [Display(Name = "Call for paper closes")]
    public DateTime? CfpEndDate { get; set; } = DateTime.Now;
    [Url]
    [Display(Name = "Event URL")]
    public string EventUrl { get; set; } = "";
    [MustBeFutureDate]
    [Display(Name = "When does the event start?")]
    public DateOnly EventStart { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [Display(Name = "When does the event end?")]
    [MustBeFutureDate]
    public DateOnly EventEnd { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [Display(Name = "Where is the event?")]
    public string Venue { get; set; } = "";
    [Display(Name = "What country is the event in?")]
    public string Country { get; set; } = "";

    public string Continent { get; set; } = "";
    public string Region { get; set; } = "";

    [Display(Name = "What city is the event in?")]
    public string City { get; set; } = "";
    [Display(Name = "What is the event about?")]
    public string Description { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public string Identifier { get; set; } = "";
    public string Source { get; set; } = "";

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    [Display(Name = "What is the Twitter handle of the event?")]
    public string? Twitter { get; set; }

    [Display(Name = "What is the LinkedIn handle of the event?")]
    public string? LinkedIn { get; set; }

    [Display(Name = "What is the Facebook handle of the event?")]
    public string? Facebook { get; set; }

    [Display(Name = "What is the Instagram handle of the event?")]
    public string? Instagram { get; set; }

    public bool? ConferenceFeeCovered { get; set; }
    public bool? AccomodationCovered { get; set; }
    public bool? TravelCovered { get; set; }

    [Url]
    [Display(Name = "URL to code of conduct")]
    public string? CodeOfConductUrl { get; set; }

    public bool IsOnline { get; set; }

    public List<string> Tags { get; set; } = new List<string>();

    public bool AddedAddressInformation { get; set; } = false;
    public bool AddedRealTimeWeather { get; set; } = false;

    int _numberOfDays = 0;
    public int NumberOfDays 
    { 
        get 
        {
            if (_numberOfDays != 0)
            {
                return _numberOfDays;
            }
            else
            {
                return 1 + (EventEnd.DayOfYear - EventStart.DayOfYear);
            }
            
        }
        set
        {
            _numberOfDays = value;
        }
    }

    public string GetLocation()
    {
        List<string> location = new();
        if (!string.IsNullOrEmpty(City))
        {
            location.Add(City);
        }
        if (!string.IsNullOrEmpty(Country))
        {
            location.Add(Country);
        }
        var result = string.Join(",", location);
        if (string.IsNullOrEmpty(result))
        {
            if (IsOnline)
            {
                result = "Online";
            }
            else
            {
                if (!string.IsNullOrEmpty(Venue))
                {
                    result = Venue;
                }
                else
                {
                    result = "N/A";
                }
            }
        }
        return result;
    }
}
