using System.Net;
using Xieyi.DistributedLock;
using Xieyi.DistributedLock.Connection;
using Xieyi.DistributedLock.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var distributedLockFactory = DistributedLockFactory.Create(new DistributedLockEndPoint()
{
    EndPoint = new DnsEndPoint("localhost", 6379),
    Password = "password",
    RedisKeyFormat = "{0}",
}, new DistributedLockRetryConfiguration(retryCount: 3, retryDelay: TimeSpan.FromMilliseconds(100)));

builder.Services.AddSingleton<IDistributedLockFactory>(distributedLockFactory);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();