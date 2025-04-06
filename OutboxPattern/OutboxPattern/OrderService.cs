using System.Text.Json;
using MassTransit;

namespace OutboxPattern;

public class OrderService(EfContext context, ILogger<OrderService> logger) : IOrderService
{
    private readonly Random _random = new();

    public async Task<string> PlaceOrder(Order order)
    {
        logger.LogInformation("Начало обработки заказа {OrderId}", order.Id);

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Сохраняем заказ
            context.Orders.Add(order);
            
            // Создаем outbox сообщение
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderPlaced",
                Data = "Some event",
                CreatedAt = DateTime.UtcNow
            };
            context.OutboxMessages.Add(outboxMessage);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Заказ {OrderId} успешно сохранен", order.Id);

            // Имитируем 75% вероятность отказа
            if (_random.Next(0, 100) < 75)
            {
                logger.LogWarning("Сервис вернул отказ для заказа {OrderId} (имитация)", order.Id);
                return "Service unavailable";
            }

            return "Order placed successfully";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Ошибка при создании заказа {OrderId}", order.Id);
            throw;
        }
    }
}