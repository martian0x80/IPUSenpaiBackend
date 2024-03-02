using IPUSenpaiBackend.DBContext;
using IPUSenpaiBackend.IPUSenpai;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

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