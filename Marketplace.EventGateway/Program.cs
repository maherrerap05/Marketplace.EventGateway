// Marketplace.EventGateway/Program.cs

using Marketplace.EventGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

// =========================
// CORS
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =========================
// HTTP CLIENTS
// =========================
builder.Services.AddHttpClients(builder.Configuration);

// =========================
// GRAPHQL — HOTCHOCOLATE
// =========================
builder.Services.AddGraphQL();

// =========================
// BUS DE EVENTOS — RABBITMQ
// =========================
builder.Services.AddRabbitMq(builder.Configuration);

var app = builder.Build();

// =========================
// MIDDLEWARE
// =========================
app.UseCors("CorsPolicy");

// =========================
// GRAPHQL ENDPOINT
// Banana Cake Pop playground disponible en /graphql
// =========================
app.MapGraphQL();

app.Run();