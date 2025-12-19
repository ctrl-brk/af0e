using AF0E.Common.Entities;
using Asp.Versioning;
using Azure.Data.Tables;
using DX.Api;
using DX.Api.Models;
using Microsoft.Extensions.Logging.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(o =>
{
    o.ReportApiVersions = true;
    o.AssumeDefaultVersionWhenUnspecified = true;

    o.DefaultApiVersion = new ApiVersion(1);
    //o.ApiVersionSelector = new CurrentImplementationApiVersionSelector(o);

    o.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'V";
    o.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddApplicationInsights(
        configureTelemetryConfiguration: (c) => c.ConnectionString = builder.Configuration.GetConnectionString("AppInsights"),
        configureApplicationInsightsLoggerOptions: (_) => { }
    );

    builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("dx-api", LogLevel.Trace);
}



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.MapOpenApi();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("openapi/v1.json", "v1");
    c.DocumentTitle = "AFØE DX Api";
});
app.UseHttpsRedirection();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app.MapGet("v{version:apiVersion}/30days", (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("AzureTableStorage") ?? throw new ApplicationException("Connection string not found");

    var svcClient = new TableServiceClient(connectionString);

    var tblClient = svcClient.GetTableClient("DxInfo");

    var data = tblClient.Query<DxInfoTableEntity>(
            $"PartitionKey ge '{DateTime.UtcNow:yyyyMM}' and PartitionKey le '{DateTime.UtcNow.AddMonths(1):yyyyMM}'")
        .DistinctBy(x => x.CallSign)
        .Where(x => x.BeginDate <= DateTime.UtcNow.AddMonths(1) && x.EndDate >= DateTime.UtcNow)
        .OrderBy(x => x.BeginDate)
        .Select(x => new DxInfo(x))
        .ToList();

    return TypedResults.Ok(data);
})
.WithApiVersionSet(versionSet)
.WithName("GetActiveDx")
.WithSummary("DX list for the next 30 days")
.WithDescription("Gets list of scheduled DX stations for the next 30 days starting from now.");

app.Run();
