using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NextTechEvent.Data;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace NextTechEvent.Function;

public class ConferenceFunctions
{
    IDocumentStore _store;
    HttpClient _client;
    IConfiguration _configuration;
    public ConferenceFunctions(IDocumentStore store, HttpClient client, IConfiguration config)
    {
        _store = store;
        _client = client;
        _configuration = config;
    }
    [FunctionName("UpdateConferences")]
    public async Task UpdateConferences([TimerTrigger("* * * * * *")] TimerInfo myTimer, ILogger log)
    //public async Task UpdateConferences([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger log)
    {
        await UpdateSessionizeConferences(log);
        await UpdateJoindInConferences(log);
        await UpdateConfsTechConferences(log);
        await UpdateLocation(log);
    }

    public async Task UpdateLocation(ILogger log)
    {
        using IDocumentSession session = _store.OpenSession();
        var fromdate = DateOnly.FromDateTime(DateTime.Now);

        var conflist = session.Query<Conference>().Where(c => c.EventStart > fromdate && c.Venue != "Online" && c.Longitude == 0 && c.Latitude == 0 && c.AddedAddressInformation == false && c.Venue != "").ToList();

        foreach (var item in conflist)
        {
            try
            {
                var query = Uri.EscapeDataString(HttpUtility.HtmlDecode(item.Venue));
                var BingApiKey = _configuration["BingApiKey"];
                var url = $"http://dev.virtualearth.net/REST/v1/Locations/{query}?includeNeighborhood=false&maxResults=1&o=json&key={BingApiKey}";
                var json = await _client.GetStringAsync(url);

                var root = JsonConvert.DeserializeObject<Functions.Data.Bing.LocationResponse>(json);

                if (root.resourceSets.Length > 0 && root.resourceSets[0].resources.Length > 0)
                {
                    var resource = root.resourceSets[0].resources[0];
                    item.Longitude = resource.geocodePoints.First().coordinates[1];
                    item.Latitude = resource.geocodePoints.First().coordinates[0];
                    if (item.Country == null)
                    {
                        item.Country = resource.address.countryRegion;
                    }
                    if (item.City == null)
                    {
                        item.City = resource.address.locality;
                    }
                }
                item.AddedAddressInformation = true;
                session.Store(item);
            }
            catch (Exception ex)
            {
                //If there is an error, set AddedAddressInformation to true, because we tried
                item.AddedAddressInformation = true;
                session.Store(item);
            }
        }
        session.SaveChanges();

    }

    public async Task UpdateSessionizeConferences(ILogger log)
    {
        //GetAllPreviousURLs
        using IDocumentSession session = _store.OpenSession();

        var conflist = session.Query<Conference>().Where(c => c.Source == "Sessionize").ToList();

        string cfpstarttime;
        DateTime cfpstartdate;
        string cfpendtime;
        DateTime cfpenddate;
        DateOnly startdate;
        DateOnly enddate;

        var xml = await _client.GetStringAsync("https://sessionize.com/sitemap/events.xml");
        var serializer = new XmlSerializer(typeof(Data.Sessionize.urlset));

        using (var stream = GenerateStreamFromString(xml))
        {
            var conferences = (Data.Sessionize.urlset)serializer.Deserialize(stream);
            var openConferences = conferences.url;//.Where(c => c.priority == 0.8m);

            //Add stuff
            foreach (var c in openConferences)
            {
                try
                {
                    if (!conflist.Any(item => item.Identifier == c.loc))
                    {
                        var html = await _client.GetStringAsync(c.loc);
                        var regexp = new Regex("<h2 class=\"no-margins\">.*?([0-9]{1,2} [a-zA-z]{3} [0-9]{4}).*?<\\/h2>", RegexOptions.Singleline);
                        var matches = regexp.Matches(html);

                        var timezone = Regex.Match(html, @"This event is in \<strong\>(.*?) \(UTC([+-][0-9][0-9]:[0-9][0-9])\)\<\/strong\> timezone");
                        var timezoneoffset = timezone.Groups[2].Value;
                        var timezoneoffsethours = -1 * int.Parse(timezoneoffset.Substring(0, 3));
                        var timezoneoffsetminutes = int.Parse(timezoneoffset.Substring(4, 2));

                        if (matches.Count == 4)
                        {
                            cfpstarttime = Regex.Match(html, @"CfS opens at (\d+:\d+ (A|P)M)<\/span>").Groups[1].Value;
                            cfpstartdate = DateTime.ParseExact(matches[2].Groups[1].Value + " " + cfpstarttime.PadLeft(8, '0'), "dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            cfpendtime = Regex.Match(html, @"CfS closes at (\d+:\d+ (A|P)M)<\/span>").Groups[1].Value;
                            cfpenddate = DateTime.ParseExact(matches[3].Groups[1].Value + " " + cfpendtime.PadLeft(8, '0'), "dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            startdate = DateOnly.ParseExact(matches[0].Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture);
                            enddate = DateOnly.ParseExact(matches[1].Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture);
                        }
                        else if (matches.Count == 3) //One day event and user groups
                        {
                            try
                            {
                                cfpstarttime = Regex.Match(html, @"CfS opens at (\d+:\d+ (A|P)M)<\/span>").Groups[1].Value;
                                cfpstartdate = DateTime.ParseExact(matches[1].Groups[1].Value + " " + cfpstarttime.PadLeft(8, '0'), "d MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
                                cfpendtime = Regex.Match(html, @"CfS closes at (\d+:\d+ (A|P)M)<\/span>").Groups[1].Value;
                                cfpenddate = DateTime.ParseExact(matches[2].Groups[1].Value + " " + cfpendtime.PadLeft(8, '0'), "d MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
                                startdate = DateOnly.ParseExact(matches[0].Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture);
                                enddate = startdate;
                            }
                            catch
                            {
                                continue;
                                //If we can't parse the dates, we skip the conference
                            }
                        }
                        else
                        {
                            Debug.WriteLine(c.loc);
                            continue;
                            //We are looking for atleast 3 dates, if only two are present something is off, like a user group not added their next event.
                        }
                        //Name
                        var name = Regex.Match(html, @"<h4>(.*?)</h4>").Groups[1].Value;
                        //Url
                        var url = Regex.Match(html, "<a href=\"(.*?)\" class=\"navy-link\" target=\"_blank\"", RegexOptions.Multiline).Groups[1].Value;
                        //Location
                        var addresses = Regex.Matches(html, "<span class=\"block\">(.*?)</span>");
                        var fulladdress = string.Join(',', addresses.Select(a => a.Groups[1].Value));
                        //Description
                        var description = string.Join(' ', Regex.Matches(html, "<p>(.*?)</p>"));
                        //Image
                        var imageUrl = Regex.Match(html, "<meta property=\"og:image\" content=\"(.*?)\"", RegexOptions.Multiline).Groups[1].Value;

                        //Update Database
                        var conference = new Conference();
                        conference.Name = HttpUtility.HtmlDecode(name);
                        conference.CfpEndDate = cfpenddate.AddHours(timezoneoffsethours);
                        conference.CfpStartDate = cfpstartdate.AddHours(timezoneoffsethours);
                        conference.CfpUrl = c.loc;
                        conference.ImageUrl = imageUrl;
                        conference.CreateDate = DateTime.Now;
                        conference.Description = description;
                        conference.EventEnd = enddate;
                        conference.EventStart = startdate;
                        conference.EventUrl = url;
                        conference.IsOnline = c.loc.ToLower().Contains("online");
                        conference.Source = "Sessionize";
                        conference.UpdateDate = DateTime.Now;
                        conference.Venue = HttpUtility.HtmlDecode(fulladdress.Trim(','));
                        conference.Identifier = c.loc;
                        log.Log(LogLevel.Information, $"Adding {conference.Name}");
                        session.Store(conference);

                    }
                    else
                    {

                    }
                }
                catch { /*Do nothing*/}
            }
            session.SaveChanges();
        }
    }

    public async Task UpdateJoindInConferences(ILogger log)
    {
        //GetAllPreviousURLs
        using IDocumentSession session = _store.OpenSession();

        var conflist = session.Query<Conference>().ToList();

        string cfpstarttime;
        DateTime? cfpstartdate = null;
        string cfpendtime;
        DateTime? cfpenddate = null;
        DateOnly startdate;
        DateOnly enddate;

        var currentpage = 1;

        var bytes = await _client.GetByteArrayAsync("https://api.joind.in/v2.1/events?format=json&resultsperpage=5000&verbose=yes");
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var root = Newtonsoft.Json.JsonConvert.DeserializeObject<NextTechEvent.Function.Data.JoindIn.Rootobject>(json);

        log.LogInformation("Got " + root.events.Count() + " conferences");
        //Add stuff
        foreach (var c in root.events)
        {
            try
            {
                startdate = DateOnly.FromDateTime(c.start_date);
                var conference = conflist.FirstOrDefault(i => (i.Name.ToLower() == c.name.ToLower() && i.EventStart == startdate) || (i.CfpUrl == c.cfp_url && i.CfpEndDate == c.cfp_end_date && i.CfpEndDate != null) || (i.EventUrl == c.href && i.EventStart == startdate));
                if (conference == null)
                {
                    conference = new Conference();
                    conference.Name = HttpUtility.HtmlDecode(c.name);
                    conference.CfpEndDate = c.cfp_end_date;
                    conference.CfpStartDate = c.cfp_start_date;
                    conference.CfpUrl = c.cfp_url;
                    conference.CreateDate = DateTime.Now;
                    conference.Description = c.description;
                    conference.EventEnd = DateOnly.FromDateTime(c.end_date); ;
                    conference.EventStart = DateOnly.FromDateTime(c.start_date); ;
                    conference.EventUrl = c.href;
                    conference.Source = "JoinedIn";
                    conference.UpdateDate = DateTime.Now;
                    conference.Venue = HttpUtility.HtmlDecode(c.location);
                    conference.IsOnline = c.location.ToLower().Contains("online");
                    conference.City = c.tz_place;

                    conference.Identifier = c.cfp_url ?? c.name;
                    log.LogInformation("Added " + c.name);
                }
                else
                {
                    log.LogInformation("Already exists " + c.name);
                }
                foreach (var tag in c.tags)
                {
                    if (!conference.Tags.Contains(tag))
                    {
                        conference.Tags.Add(tag);
                    }
                }
                if (conference.Longitude == 0 && conference.Latitude == 0)
                {
                    conference.Longitude = Convert.ToDouble(c.longitude);
                    conference.Latitude = Convert.ToDouble(c.latitude);
                }
                session.Store(conference);
            }
            catch (Exception ex)
            {
                log.LogError(c + " " + ex.Message);
            }
        }
        session.SaveChanges();
    }

    public async Task UpdateConfsTechConferences(ILogger log)
    {
        //Get al conferences since this may contain confs from other sources
        using IDocumentSession session = _store.OpenSession();
        var conflist = session.Query<Conference>().ToList();

        string cfpstarttime;
        DateTime? cfpstartdate = null;
        string cfpendtime;
        DateTime? cfpenddate = null;
        DateOnly startdate;
        DateOnly enddate;

        var currentpage = 1;

        string[] categories = { "android", "clojure", "css", "data", "devops", "dotnet", "elixir", "general", "golang", "graphql", "ios", "java", "javascript", "networking", "php", "python", "ruby", "rust", "scala", "security", "tech-comm", "ux" };
        foreach (var cat in categories)
        {
            try
            {
                //Foreach folder and json file
                var json = await _client.GetStringAsync($"https://raw.githubusercontent.com/tech-conferences/conference-data/master/conferences/{DateTime.Now.Year}/{cat}.json");
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<Data.ConfsTech.Event[]>(json);

                log.LogInformation("Got " + root.Count() + " conferences");
                //Add stuff
                foreach (var c in root)
                {
                    try
                    {
                        startdate = DateOnly.FromDateTime(c.startDate);
                        var conference = conflist.FirstOrDefault(i => (i.Name.ToLower() == c.name.ToLower() && i.EventStart == startdate) || (i.CfpUrl == c.cfpUrl && i.CfpEndDate == c.cfpEndDate && i.CfpEndDate != null && i.CfpUrl != null) || (i.EventUrl == c.url && i.EventStart == startdate));
                        //Make sure name is the same as well
                        if (conference == null)
                        {
                            conference = new Conference();
                            conference.Name = HttpUtility.HtmlDecode(c.name);

                            enddate = DateOnly.FromDateTime(c.endDate);
                            cfpenddate = c.cfpEndDate;
                            conference.CfpEndDate = cfpenddate;
                            conference.CfpStartDate = cfpstartdate;
                            conference.CfpUrl = c.cfpUrl;
                            conference.CreateDate = DateTime.Now;
                            conference.EventEnd = enddate;
                            conference.EventStart = startdate;
                            conference.EventUrl = c.url;
                            conference.Source = "ConfsTech";
                            conference.UpdateDate = DateTime.Now;
                            conference.Venue = c.online ? "Online" : $"{c.city},{c.country}";
                            conference.City = c.city;
                            conference.Country = c.country;
                            conference.IsOnline = c.online;
                            conference.Identifier = c.cfpUrl ?? c.name;
                            conflist.Add(conference);
                        }

                        if (!conference.Tags.Contains(cat))
                        {
                            conference.Tags.Add(cat);
                        }
                        if (string.IsNullOrEmpty(conference.Twitter))
                        {
                            conference.Twitter = c.twitter;
                        }
                        if (string.IsNullOrEmpty(conference.CodeOfConductUrl))
                        {
                            conference.CodeOfConductUrl = c.cocUrl;
                        }

                        session.Store(conference);
                        log.LogInformation("Added " + c.name);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(c + " " + ex.Message);
                    }

                }
            }
            catch { }
        }
        session.SaveChanges();
    }
    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
