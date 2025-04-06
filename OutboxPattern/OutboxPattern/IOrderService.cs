namespace OutboxPattern;

public interface IOrderService
{
    Task<string> PlaceOrder(Order order);
}