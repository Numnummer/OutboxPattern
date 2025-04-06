using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace OutboxPattern;

public class OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger,
    IPublishEndpoint publishEndpoint) : BackgroundService
{
    
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EfContext>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        logger.LogInformation("Обработка сообщения {MessageId} типа {MessageType}", 
                            message.Id, message.Type);

                        await publishEndpoint.Publish(message, stoppingToken);

                        message.ProcessedAt = DateTime.UtcNow;
                        message.RetryCount++;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        logger.LogInformation("Сообщение {MessageId} успешно обработано", message.Id);
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.Message;
                        message.RetryCount++;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        logger.LogError(ex, "Ошибка при обработке сообщения {MessageId}", message.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в Outbox Processor");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}