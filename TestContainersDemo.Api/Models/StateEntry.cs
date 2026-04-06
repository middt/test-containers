namespace TestContainersDemo.Api.Models;

public class StateEntry<T>
{
    public string Key { get; set; } = string.Empty;
    public T? Value { get; set; }
}

public class OrderEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
