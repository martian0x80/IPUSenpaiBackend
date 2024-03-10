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
    options.UseNpgsql(builder.Configuration.GetConnectionString("CONNSTR")), ServiceLifetime.Scoped);

builder.Services.AddRateLimiter(s =>
{
    s.AddTokenBucketLimiter(policyName: "tokenbucket", options =>
    {
        options.TokenLimit = 2;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
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
                    TokenLimit = 30,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 15,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                    TokensPerPeriod = 30,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigins",
        policy  =>
        {
            policy.WithOrigins("https://ipu-senpai.vercel.app",
                "http://localhost:3000");
        });
});

var app = builder.Build();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DisplayRequestDuration());
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();