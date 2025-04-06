using MassTransit;
using Microsoft.EntityFrameworkCore;
using OutboxPattern;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EfContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Регистрируем сервисы
builder.Services.AddScoped<IOrderService, OrderService>();

// Настройка MassTransit с Outbox
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, rabbitConfig) =>
    {
        rabbitConfig.Host(builder.Configuration["RabbitMQ:Host"], host =>
        {
            host.Username(builder.Configuration["RabbitMQ:Username"]);
            host.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        rabbitConfig.ConfigureEndpoints(context);
    });
});
builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();