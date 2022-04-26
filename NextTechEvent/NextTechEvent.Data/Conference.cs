using System.ComponentModel.DataAnnotations;

namespace NextTechEvent.Data;

public class Conference
{
    public string Id { get; set; }
    [MinLength(3)]
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string CfpUrl { get; set; }
    public DateTime? CfpStartDate { get; set; }
    public DateTime? CfpEndDate { get; set; }
    public string EventUrl { get; set; }
    public DateOnly EventStart { get; set; }
    public DateOnly EventEnd { get; set; }
    public string Venue { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string Description { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public string Identifier { get; set; }
    public string Source { get; set; }

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public string? Twitter { get; set; }
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
