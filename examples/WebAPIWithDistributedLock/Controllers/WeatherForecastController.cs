using Microsoft.AspNetCore.Mvc;
using Xieyi.DistributedLock;
using Xieyi.DistributedLock.Interfaces;

namespace WebAPIWithDistributedLock.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IDistributedLockFactory _distributedLockFactory;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IDistributedLockFactory distributedLockFactory)
    {
        _logger = logger;
        _distributedLockFactory = distributedLockFactory;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        using (DistributedLockProvider.Lock(_distributedLockFactory, "ApiTest"))
        {
            Thread.Sleep(10000);
            
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
        }
    }
}