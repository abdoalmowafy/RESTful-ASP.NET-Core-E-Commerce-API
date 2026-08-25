using Ordering.Customer.Contracts;

namespace Ordering.Customer.Services;

public interface IPaymobCallbackService
{
    Task<Result> HandleAsync(string? receivedHmac, PaymobCallbackPayload payload, CancellationToken cancellationToken = default);
}

public class PaymobCallbackService(
    AppDbContext context,
    IPaymobCallbackVerifier verifier,
    IOrderTrackingNotifier trackingNotifier,
    ILogger<PaymobCallbackService> logger) : IPaymobCallbackService
{
    private readonly AppDbContext _context = context;
    private readonly IPaymobCallbackVerifier _verifier = verifier;
    private readonly IOrderTrackingNotifier _trackingNotifier = trackingNotifier;
    private readonly ILogger<PaymobCallbackService> _logger = logger;

    public async Task<Result> HandleAsync(string? receivedHmac, PaymobCallbackPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload.Obj is null)
            return Result.Succeed();

        if (!_verifier.IsValid(receivedHmac, payload.Obj))
            return Result.Failure(Error.Unauthorized("Payments.InvalidSignature", "Callback signature verification failed"));

        var transaction = payload.Obj;

        if (transaction.Order is null)
            return Result.Succeed();

        var order = await _context.Orders
            .Include(o => o.User!)
                .ThenInclude(u => u.Cart!)
                    .ThenInclude(c => c.CartProducts)
            .FirstOrDefaultAsync(o => o.PaymobOrderId == transaction.Order.Id, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Paymob callback for unknown order {PaymobOrderId}", transaction.Order.Id);
            return Result.Succeed();
        }

        if (!transaction.Success || transaction.Pending || transaction.ErrorOccured)
            return Result.Succeed();

        if (order.Status != OrderStatus.Paying)
            return Result.Succeed();

        order.Status = OrderStatus.Processing;
        order.RecordStatus("Online payment confirmed");

        var cart = order.User?.Cart;
        if (cart is not null)
        {
            cart.PromoCodeId = null;
            _context.CartProducts.RemoveRange(cart.CartProducts);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "Online payment confirmed", DateTime.UtcNow);
        return Result.Succeed();
    }
}
