using NextTechEvent.Data.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace NextTechEvent.Data;

public class Conference
{
    public string Id { get; set; }
    [Required, MinLength(3)]
    [Display(Name = "Conference name")]
    public string Name { get; set; }
    [Url]
    public string ImageUrl { get; set; }
    [Url]
    [Display(Name="Call for paper URL")]
    public string? CfpUrl { get; set; }
    [Display(Name = "Call for paper opens")]
    public DateTime? CfpStartDate { get; set; } = DateTime.Now;
    [Display(Name = "Call for paper closes")]
    public DateTime? CfpEndDate { get; set; } = DateTime.Now;
    [Url]
    [Display(Name = "Event URL")]
    public string EventUrl { get; set; }
    [MustBeFutureDate]
    [Display(Name = "When does the event start?")]
    public DateOnly EventStart { get; set; }= DateOnly.FromDateTime(DateTime.Now);
    [Display(Name = "When does the event end?")]
    [MustBeFutureDate]
    public DateOnly EventEnd { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [Display(Name = "Where is the event?")]
    public string Venue { get; set; }
    [Display(Name = "What country is the event in?")]
    public string Country { get; set; }
    [Display(Name = "What city is the event in?")]
    public string City { get; set; }
    [Display(Name = "What is the event about?")]
    public string Description { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public string Identifier { get; set; }
    public string Source { get; set; }

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    [Display(Name = "What is the Twitter handle of the event?")]
    public string? Twitter { get; set; }
    
    [Url]
    [Display(Name="URL to code of conduct")]
    public string? CodeOfConductUrl { get; set; }

    public bool IsOnline { get; set; }

    public List<string> Tags { get; set; } = new List<string>();

    public bool AddedAddressInformation { get; set; } = false;

    [Newtonsoft.Json.JsonIgnore]
    public string ClosingIn
    {
        get
        {
            if (CfpEndDate != null)
            {
                var time = (CfpEndDate - DateTime.Now).Value;

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
}
