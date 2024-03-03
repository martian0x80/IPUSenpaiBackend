using System.Net;
using IPUSenpaiBackend.DBContext;
using IPUSenpaiBackend.IPUSenpai;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); ;
builder.Services.AddScoped<IIPUSenpaiAPI, IPUSenpaiAPI>();
builder.Services.AddLogging();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "IPUSenpaiBackend", Version = "v1" });
    });
    
}

builder.Services.AddDbContext<IPUSenpaiDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CONNSTR")));

builder.Services.AddRateLimiter(s =>
{
    s.AddTokenBucketLimiter(policyName: "tokenbucket", options =>
    {
        options.TokenLimit = 2;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        options.TokensPerPeriod = 2;
        options.AutoReplenishment = true;
    });
    s.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context =>
    {
        IPAddress? remoteIpAddress = context.Connection.RemoteIpAddress;

        if (!IPAddress.IsLoopback(remoteIpAddress!))
        {
            return RateLimitPartition.GetTokenBucketLimiter
            (remoteIpAddress!, _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                    TokensPerPeriod = 5,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
    });
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
} else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();