using System.Text.Json.Serialization;
using AF0E.DB;
using Logbook.Api.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddDbContext<HrdDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("HrdLog")));
builder.Services.AddScoped<HrdDbContext>(_ => new HrdDbContext(builder.Configuration.GetConnectionString("HrdLog")!));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.RegisterV1Endpoints();

app.MapFallbackToFile("/index.html");
app.Run();
