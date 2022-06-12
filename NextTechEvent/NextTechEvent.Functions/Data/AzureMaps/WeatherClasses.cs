using System;

namespace NextTechEvent.Functions.Data.AzureMaps;

public class WeatherResults
{
    public WeatherResult[] results { get; set; }
}

public class WeatherResult
{
    public DateTime date { get; set; }
    public Temperature temperature { get; set; }
    public Degreedaysummary degreeDaySummary { get; set; }
    public Precipitation precipitation { get; set; }
}

public class Temperature
{
    public Minimum minimum { get; set; }
    public Maximum maximum { get; set; }
    public Average average { get; set; }
}

public class Minimum
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}

public class Maximum
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}

public class Average
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}

public class Degreedaysummary
{
    public Heating heating { get; set; }
    public Cooling cooling { get; set; }
}

public class Heating
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}

public class Cooling
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}

public class Precipitation
{
    public float value { get; set; }
    public string unit { get; set; }
    public int unitType { get; set; }
}
