using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NextTechEvent.Data;
using NextTechEvent.Function.Data.JoindIn;
using NextTechEvent.Function.Data.Sessionize;
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
using System.Xml.Serialization;

namespace NextTechEvent.Function;

public class ConferenceFunctions
{
    IDocumentStore _store;
    HttpClient _client;
    public ConferenceFunctions(IDocumentStore store, HttpClient client)
    {
        _store = store;
        _client = client;
    }

    [FunctionName("UpdateSessionizeConferences")]
    //public async Task UpdateSessionizeConferences([TimerTrigger("* * * * * *")] TimerInfo myTimer, ILogger log)
    public async Task UpdateSessionizeConferences([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger log)
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
        var serializer = new XmlSerializer(typeof(urlset));

        using (var stream = GenerateStreamFromString(xml))
        {
            var conferences = (urlset)serializer.Deserialize(stream);
            var openConferences = conferences.url.Where(c => c.priority == 0.8m);

            //Add stuff
            foreach (var c in openConferences)
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
                    conference.CfpEndDate = cfpenddate.AddHours(timezoneoffsethours);
                    conference.CfpStartDate = cfpstartdate.AddHours(timezoneoffsethours);
                    conference.CfpUrl = c.loc;
                    conference.ImageUrl = imageUrl;
                    conference.CreateDate = DateTime.Now;
                    conference.Description = description;
                    conference.EventEnd = enddate;
                    conference.EventStart = startdate;
                    conference.EventUrl = url;
                    conference.Name = name;
                    conference.Source = "Sessionize";
                    conference.UpdateDate = DateTime.Now;
                    conference.Venue = fulladdress.Trim(',');
                    conference.Identifier = c.loc;

                    session.Store(conference);

                }
                else
                {

                }
            }
            session.SaveChanges();
        }
    }

    [FunctionName("UpdateJoindInConferences")]
    public async Task UpdateJoindInConferences([TimerTrigger("* * * * * *")] TimerInfo myTimer, ILogger log)
    //public async Task UpdateJoindInConferences([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger log)
    {
        //GetAllPreviousURLs
        using IDocumentSession session = _store.OpenSession();

        var conflist = session.Query<Conference>().Where(c => c.Source == "JoinedIn").ToList();

        string cfpstarttime;
        DateTime? cfpstartdate = null;
        string cfpendtime;
        DateTime? cfpenddate = null;
        DateOnly startdate;
        DateOnly enddate;

        var currentpage = 1;

        var bytes = await _client.GetByteArrayAsync("https://api.joind.in/v2.1/events?format=json&resultsperpage=5000&verbose=yes");
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var root = Newtonsoft.Json.JsonConvert.DeserializeObject<Rootobject>(json);

        log.LogInformation("Got " + root.events.Count() + " conferences");
        //Add stuff
        foreach (var c in root.events.Where(e => !conflist.Any(i => i.Identifier == e.cfp_url)))
        {
            try
            {

                if (!conflist.Any(i => i.Identifier == c.cfp_url))
                {

                    var name = c.name;
                    //Url
                    var url = c.cfp_url;

                    startdate = DateOnly.FromDateTime(c.start_date);
                    enddate = DateOnly.FromDateTime(c.end_date);
                    cfpstartdate = c.cfp_start_date;
                    cfpenddate = c.cfp_end_date;

                    //    ////Description
                    var description = c.description;

                    var conference = new Conference();
                    conference.CfpEndDate = cfpenddate;
                    conference.CfpStartDate = cfpstartdate;
                    conference.CfpUrl = c.cfp_url;
                    conference.CreateDate = DateTime.Now;
                    conference.Description = description;
                    conference.EventEnd = enddate;
                    conference.EventStart = startdate;
                    conference.EventUrl = url;
                    conference.Name = name;
                    conference.Source = "JoinedIn";
                    conference.UpdateDate = DateTime.Now;
                    conference.Venue = c.location;
                    conference.City = c.tz_place;
                    conference.Longitude = Convert.ToDouble(c.longitude);
                    conference.Latitude = Convert.ToDouble(c.latitude);
                    conference.Identifier = c.cfp_url;
                    session.Store(conference);

                    log.LogInformation("Added " + c.name);
                }
                else
                {
                    log.LogInformation("Already exists " + c.name);
                }
            }
            catch (Exception ex)
            {
                log.LogError(c + " " + ex.Message);
            }
        }
        session.SaveChanges();
    }

    //[FunctionName("UpdateConfsTechInConferences")]
    ////public async Task UpdateConfsTechInConferences([TimerTrigger("* * * * * *")]TimerInfo myTimer, ILogger log)
    //public async Task UpdateConfsTechInConferences([TimerTrigger("0 0 */6 * * *")]TimerInfo myTimer, ILogger log)
    //{
    //    //Get al conferences since this may contain confs from other sources
    //    var conflist = context.Conferences;

    //    string cfpstarttime;
    //    DateTime? cfpstartdate = null;
    //    string cfpendtime;
    //    DateTime? cfpenddate = null;
    //    DateTime startdate;
    //    DateTime enddate;

    //    var wc = new WebClient();
    //    var currentpage = 1;

    //    string[] categories = { "android", "clojure", "css", "data", "devops", "dotnet", "elixir", "general", "golang", "graphql", "ios", "java", "javascript", "networking", "php", "python", "ruby", "rust", "scala", "security", "tech-comm", "ux" };

    //    foreach (var cat in categories)
    //    {
    //        //Foreach folder and json file
    //        var json = wc.DownloadString($"https://raw.githubusercontent.com/tech-conferences/conference-data/master/conferences/2020/{cat}.json");
    //        var root = Newtonsoft.Json.JsonConvert.DeserializeObject<Data.ConfsTech.Event[]>(json);

    //        log.LogInformation("Got " + root.Count() + " conferences");
    //        //Add stuff
    //        foreach (var c in root)
    //        {
    //            try
    //            {

    //                //Makesure name is the same as well
    //                if (!conflist.Any(i => i.Identifier == (c.cfpUrl ?? c.name) && i.Name == c.name))
    //                {

    //                    var name = c.name;
    //                    //Url
    //                    var url = c.url;

    //                    startdate = c.startDate;
    //                    enddate = c.endDate;
    //                    //cfpstartdate = c.cfps;
    //                    cfpenddate = c.cfpEndDate;

    //                    //    ////Description
    //                    //var description = c.;

    //                    var conference = new Conference();
    //                    conference.CFPEndDate = cfpenddate;
    //                    conference.CFPStartDate = cfpstartdate;
    //                    conference.CFPUrl = c.cfpUrl;
    //                    conference.CreateDate = DateTime.Now;
    //                    //conference.Description = description;
    //                    conference.EventEnd = enddate;
    //                    conference.EventStart = startdate;
    //                    conference.EventUrl = url;
    //                    conference.Name = name;
    //                    conference.Source = "ConfsTech";
    //                    conference.UpdateDate = DateTime.Now;
    //                    //conference.Venue = c.;
    //                    conference.City = c.city;
    //                    //conference.Longitude = c.longitude?.ToString();
    //                    //conference.Latitude = c.latitude?.ToString();
    //                    conference.Identifier = c.cfpUrl ?? c.name;
    //                    context.Conferences.Add(conference);
    //                    await context.SaveChangesAsync();

    //                    log.LogInformation("Added " + c.name);
    //                }
    //                else
    //                {
    //                    log.LogInformation("Already exists " + c.name);
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                log.LogError(c + " " + ex.Message);
    //            }
    //        }
    //    }
    //}
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
