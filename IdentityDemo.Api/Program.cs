using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Smart")
    .AddPolicyScheme("Smart", "Entra or Auth0", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (header.StartsWith("Bearer "))
            {
                var token = header.Substring("Bearer ".Length);
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var issuer = handler.ReadJwtToken(token).Issuer;
                    if (issuer.Contains("auth0.com"))
                        return "Auth0";
                }
            }
            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthentication()
    .AddJwtBearer("Auth0", options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration.GetSection("Cosmos");
    return new CosmosClient(cfg["Endpoint"], cfg["Key"], new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        LimitToEndpoint = true,
        HttpClientFactory = () => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var db = await client.CreateDatabaseIfNotExistsAsync("IdentityDemo");
    await db.Database.CreateContainerIfNotExistsAsync("Items", "/id");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast").RequireAuthorization();

app.MapPost("/items", async (CosmosClient client, IConfiguration config, Item item) =>
{
    var container = client.GetContainer(config["Cosmos:Database"], config["Cosmos:Container"]);
    await container.CreateItemAsync(item, new PartitionKey(item.Id));
    return Results.Created($"/items/{item.Id}", item);
}).RequireAuthorization();

app.MapGet("/items/{id}", async (string id, CosmosClient client, IConfiguration config) =>
{
    var container = client.GetContainer(config["Cosmos:Database"], config["Cosmos:Container"]);
    try
    {
        var response = await container.ReadItemAsync<Item>(id, new PartitionKey(id));
        return Results.Ok(response.Resource);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

public record Item([property: JsonProperty("id")] string Id, string Name);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
