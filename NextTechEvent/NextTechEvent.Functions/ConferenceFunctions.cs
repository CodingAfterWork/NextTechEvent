using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NextTechEvent.Data;
using NextTechEvent.Functions.Data.AzureMaps;
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
        //await UpdateSessionizeConferences(log);
        //await UpdateJoindInConferences(log);
        //await UpdateConfsTechConferences(log);
        await UpdatePaperCallConferences(log);
        //await UpdateLocation(log);
    }

    public async Task UpdateLocation(ILogger log)
    {
        using IDocumentSession session = _store.OpenSession();
        var fromdate = DateOnly.FromDateTime(DateTime.Now);

        var conflist = session.Query<Conference>().Where(c => c.EventStart > fromdate && c.Venue != "Online" /*&& c.Longitude == 0 && c.Latitude == 0 && c.AddedAddressInformation == false*/ && c.Venue != "").ToList();

        foreach (Conference item in conflist)
        {
            log.Log(LogLevel.Information, $"Geolocating {item.Name}");
            try
            {
                var query = Uri.EscapeDataString(HttpUtility.HtmlDecode(item.Venue));
                var apiKey = _configuration["AzureMapsKey"];
                var url = $"https://atlas.microsoft.com/search/address/json?&subscription-key={apiKey}&api-version=1.0&language=en-US&query={query}";
                var json = await _client.GetStringAsync(url);

                var root = JsonConvert.DeserializeObject<GeoLocationResults>(json);

                if (root.results.Length > 0)
                {
                    var result = root.results.FirstOrDefault();
                    if (result != null)
                    {
                        item.Latitude = Convert.ToDouble(result.position.lat);
                        item.Longitude = Convert.ToDouble(result.position.lon);

                        if (item.Country == null)
                        {
                            item.Country = result.address.country;
                        }
                        if (item.City == null)
                        {
                            item.City = result.address.municipality;
                        }
                    }
                }

                //Try and get the weather

                if (item.Longitude != 0 && item.Latitude != 0)
                {
                    log.Log(LogLevel.Information, $"Finding weather {item.Name}");
                    var lonlat = $"{item.Latitude.ToString("G", CultureInfo.CreateSpecificCulture("en-US"))},{item.Longitude.ToString("G", CultureInfo.CreateSpecificCulture("en-US"))}";
                    var weatherurl = $"https://atlas.microsoft.com/weather/historical/normals/daily/json?&subscription-key={apiKey}&api-version=1.0&language=en-US&query={lonlat}&startDate={item.EventStart.ToString("yyyy-MM-dd")}&endDate={item.EventEnd.ToString("yyyy-MM-dd")}";
                    var weatherjson = await _client.GetStringAsync(weatherurl);

                    var weatherroot = JsonConvert.DeserializeObject<WeatherResults>(weatherjson);

                    if (weatherroot.results.Any())
                    {
                        var tsf = session.TimeSeriesFor<WeatherData>(item.Id);
                        foreach (var temp in weatherroot.results)
                        {
                            tsf.Append(temp.date.Date, new WeatherData
                            {
                                Minimum = Convert.ToDouble(temp.temperature.minimum.value),
                                Maximum = Convert.ToDouble(temp.temperature.maximum.value),
                                Average = Convert.ToDouble(temp.temperature.average.value)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                item.UpdateDate = DateTime.Now;
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
        //Get all conferences since this may contain confs from other sources
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

    public async Task UpdatePaperCallConferences(ILogger log)
    {
        //GetAllPreviousURLs
        using IDocumentSession session = _store.OpenSession();

        var conflist = session.Query<Conference>().Where(c => c.Source == "Papercall").ToList();

        string cfpstarttime;
        DateTime? cfpstartdate = null;
        string cfpendtime;
        DateTime? cfpenddate = null;
        DateTime startdate;
        DateTime enddate;

        var currentpage = 1;
        while (true)
        {

            var html = await _client.GetStringAsync("https://www.papercall.io/events?page=" + currentpage);

            var links = Regex.Matches(html, "<a href=\"https://www.papercall.io/(.*?)\"");

            var conferences = links.Select(l => "https://www.papercall.io/" + l.Groups[1].Value).Distinct();
            if (conferences.Count() == 0)
            {
                break;
            }
            else
            {
                currentpage++;
            }

            log.LogInformation("Got " + conferences.Count() + " conferences (page: " + currentpage + ")");
            //Add stuff
            foreach (var c in conferences)
            {
                try
                {
                    if (!conflist.Any(item => item.Identifier == c))
                    {
                        var eventhtml = await _client.GetStringAsync(c);
                        //Name
                        var name = Regex.Match(eventhtml, "<h1 class=\"subheader__title\">(.*?)</h1>").Groups[1].Value;
                        //Url
                        var url = Regex.Match(eventhtml, "<a target=\"_blank\" href=\"(.*?)\">(.*?)</a>", RegexOptions.Multiline).Groups[1].Value;

                        var monthregexp = "(January|February|March|April|May|June|July|August|September|October|November|December)";
                        var adressanddates = Regex.Match(eventhtml, "<h1 class=\"subheader__subtitle\">(.*?) (January|February|March|April|May|June|July|August|September|October|November|December) (\\d{2}), (\\d{4}), (January|February|March|April|May|June|July|August|September|October|November|December) (\\d{2}), (\\d{4})", RegexOptions.Singleline);

                        try
                        {

                            ////Location
                            if (adressanddates.Length == 0)
                            {   //Try with one day
                                adressanddates = Regex.Match(eventhtml, "<h1 class=\"subheader__subtitle\">(.*?) (January|February|March|April|May|June|July|August|September|October|November|December) (\\d{2}), (\\d{4})", RegexOptions.Singleline);
                                enddate = Convert.ToDateTime($"{adressanddates.Groups[2].Value} {adressanddates.Groups[3].Value} {adressanddates.Groups[4].Value}");

                            }
                            else
                            {
                                enddate = Convert.ToDateTime($"{adressanddates.Groups[5].Value} {adressanddates.Groups[6].Value} {adressanddates.Groups[7].Value}");
                            }
                        }
                        catch
                        {
                            //No date was found, probably a user group
                            log.LogInformation(c + " Probably a user group");
                            continue;
                        }
                        var fulladdress = adressanddates.Groups[1].Value;
                        startdate = Convert.ToDateTime($"{adressanddates.Groups[2].Value} {adressanddates.Groups[3].Value} {adressanddates.Groups[4].Value}");

                        var cfpend = Regex.Match(eventhtml, "<td width=\"100%\">" + monthregexp + " (\\d{2}), (\\d{4}) (\\d{2}:\\d{2}) UTC</td>");
                        cfpenddate = Convert.ToDateTime($"{cfpend.Groups[1].Value} {cfpend.Groups[2].Value} {cfpend.Groups[3].Value} {cfpend.Groups[4].Value}z");
                        ////Description
                        var description = string.Join(' ', Regex.Matches(eventhtml, "<div class=\"box__content\">(.*?)</div>", RegexOptions.Singleline));

                        //Tags
                        var tagsmatches = Regex.Matches(eventhtml, """<a href="/events\?keywords=tags%3A(.*?)">(.*?)</a>""", RegexOptions.Singleline);
                        var tags = tagsmatches.Select(t => t.Groups[2].Value).ToList();

                        //Update Database

                        var conference = new Conference();
                        conference.CfpEndDate = cfpenddate;
                        conference.CfpStartDate = cfpstartdate;
                        conference.CfpUrl = c;
                        conference.CreateDate = DateTime.Now;
                        conference.Description = description;
                        conference.EventEnd = DateOnly.FromDateTime(enddate);
                        conference.EventStart = DateOnly.FromDateTime(startdate);
                        conference.EventUrl = url;
                        conference.Name = name;
                        conference.Source = "Papercall";
                        conference.UpdateDate = DateTime.Now;
                        conference.Venue = Regex.Replace(fulladdress, @"\s+", string.Empty);
                        conference.Identifier = c;
                        conference.Tags = tags;
                        session.Store(conference);
                        log.LogInformation("Added " + c);
                    }
                    else
                    {
                        log.LogInformation("Already exists " + c);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(c + " " + ex.Message);
                }

            }
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
