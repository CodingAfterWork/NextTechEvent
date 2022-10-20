namespace NextTechEvent.Data.Index;

public class ConferenceCountByDate
{
    public DateOnly Date { get; set; }
    public string SearchTerm { get; set; }
    public int Count { get; set; }
}