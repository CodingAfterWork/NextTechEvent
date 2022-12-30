namespace NextTechEvent.Data;

public class Status
{
    public string? Id { get; set; }
    public required string ConferenceId { get; set; }
    public required string UserId { get; set; }
    public required StateEnum State { get; set; }
}
