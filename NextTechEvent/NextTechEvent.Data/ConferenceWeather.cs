namespace NextTechEvent.Data
{
    public class ConferenceWeather
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Average { get; set; }
        public DateTime Timestamp { get; set; }
        public string ConferenceId { get; set; }
        public string Name { get; set; }
        public DateTime? CfpStartDate { get; set; }
        public DateTime? CfpEndDate { get; set; }
        public DateOnly EventStart { get; set; }
        public DateOnly EventEnd { get; set; }
    }
}
