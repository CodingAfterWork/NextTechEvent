namespace NextTechEvent.Data.Index;

public class ConferenceCountByDate
{
    public DateOnly Date { get; set; }
    public required string SearchTerm { get; set; } 
    public int Count { get; set; }
}