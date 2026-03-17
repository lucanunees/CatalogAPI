using System.Text;
using CatalogAPI.API.Endpoints;
using CatalogAPI.Consumers;
using CatalogAPI.Infrastructure.Persistence;
using Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<CatalogDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

# region JWT (valida tokens do UsersAPI)
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false; // dev
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogAPI", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input your: Bearer {seu_token_jwt}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
# endregion

#region MassTransit + Outbox
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromMilliseconds(10000);
    });
    
    x.AddConsumer<PaymentProcessedConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"];
        var user = builder.Configuration["RabbitMq:Username"];
        var pass = builder.Configuration["RabbitMq:Password"];
        var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";

        cfg.Host(host, vhost, h =>
        {
            h.Username(user);
            h.Password(pass);
        });
        
        cfg.Message<OrderPlacedEventV1>(m => m.SetEntityName("fcg.catalog"));
        cfg.Publish<OrderPlacedEventV1>(p => p.ExchangeType = "topic");

        cfg.ReceiveEndpoint("catalog.payment-processed", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.payments", s =>
            {
                s.ExchangeType = "topic";
                s.RoutingKey = "v1.payment-processed";
            });
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
# endregion

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>("catalogdb");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGamesEndpoints();
app.MapOrdersEndpoints();
app.MapLibraryEndpoints();

app.Run();