using Ordering.Customer.Contracts;
using Npgsql;
using Ordering.Customer.Services;

namespace Ordering.Customer.Services;

public interface IOrdersService
{
    Task<Result<PaginatedList<OrderResponse>>> GetMyOrdersAsync(string userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> GetAsync(string userId, int orderId, bool isStaff, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OrderTimelineItem>>> GetTimelineAsync(string userId, int orderId, bool isStaff, CancellationToken cancellationToken = default);
    Task<Result<CheckoutResponse>> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(string userId, int orderId, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class OrdersService(AppDbContext context, IPaymobService paymobService, IOrderTrackingNotifier trackingNotifier) : IOrdersService
{
    private const long DeliveryFeeCents = 5000;
    private const long CodFeeCents = 1000;

    private readonly AppDbContext _context = context;
    private readonly IPaymobService _paymobService = paymobService;
    private readonly IOrderTrackingNotifier _trackingNotifier = trackingNotifier;

    public async Task<Result<PaginatedList<OrderResponse>>> GetMyOrdersAsync(string userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Address)
            .Include(o => o.OrderProducts).ThenInclude(op => op.Product)
            .Where(o => o.UserId == userId && o.Status != OrderStatus.Paying && o.DeletedAt == null)
            .OrderByDescending(o => o.CreatedAt);

        var page = await PaginatedList<Order>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        var mapped = page.Items.Select(MapOrder).ToList();

        return Result.Succeed(new PaginatedList<OrderResponse>(mapped, page.PageNumber, page.TotalCount, page.TotalPages));
    }

    public async Task<Result<OrderResponse>> GetAsync(string userId, int orderId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Address)
            .Include(o => o.PromoCode)
            .Include(o => o.OrderProducts).ThenInclude(op => op.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || (!isStaff && order.UserId != userId))
            return Result.Failure<OrderResponse>(OrderingErrors.Order.NotFound);

        return Result.Succeed(MapOrder(order));
    }

    public async Task<Result<IReadOnlyList<OrderTimelineItem>>> GetTimelineAsync(string userId, int orderId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.StatusEvents)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || (!isStaff && order.UserId != userId))
            return Result.Failure<IReadOnlyList<OrderTimelineItem>>(OrderingErrors.Order.NotFound);

        var timeline = order.StatusEvents
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Select(e => new OrderTimelineItem(e.Status, e.CreatedAt, e.Note))
            .ToList();

        return Result.Succeed<IReadOnlyList<OrderTimelineItem>>(timeline);
    }

    public async Task<Result<CheckoutResponse>> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.Cart!).ThenInclude(c => c.CartProducts).ThenInclude(cp => cp.Product)
            .Include(u => u.Cart!).ThenInclude(c => c.PromoCode)
            .FirstAsync(u => u.Id == userId, cancellationToken);

        if (user.IsDisabled)
            return Result.Failure<CheckoutResponse>(UserErrors.Disabled);

        if (!user.EmailConfirmed || !user.PhoneNumberConfirmed)
            return Result.Failure<CheckoutResponse>(UserErrors.ContactNotConfirmed);

        if (await _context.Orders.AnyAsync(
                o => o.UserId == userId &&
                     o.Status != OrderStatus.Delivered &&
                     o.Status != OrderStatus.Cancelled &&
                     o.DeletedAt == null,
                cancellationToken))
            return Result.Failure<CheckoutResponse>(OrderingErrors.Order.OngoingExists);

        var cart = user.Cart!;
        var cartProducts = cart.CartProducts.Where(cp => cp.Product!.DeletedAt == null).ToList();

        if (cartProducts.Count == 0 || cartProducts.Any(cp => cp.Quantity < 1))
            return Result.Failure<CheckoutResponse>(OrderingErrors.Order.EmptyCart);

        if (cartProducts.Any(cp => cp.Product!.Quantity < cp.Quantity))
            return Result.Failure<CheckoutResponse>(CatalogErrors.Product.OutOfStock);

        if (cart.PromoCode is { Active: false })
            return Result.Failure<CheckoutResponse>(CatalogErrors.PromoCode.Inactive);

        Address? address;
        if (request.DeliveryNeeded)
        {
            address = user.Addresses.FirstOrDefault(a => a.Id == request.AddressId && a.DeletedAt == null);
            if (address is null)
                return Result.Failure<CheckoutResponse>(OrderingErrors.Order.AddressNotFound);
        }
        else
        {
            address = await _context.Addresses.FirstOrDefaultAsync(
                a => a.Id == request.AddressId && a.UserId == null && a.DeletedAt == null,
                cancellationToken);

            if (address is null)
                return Result.Failure<CheckoutResponse>(CatalogErrors.StoreAddress.NotFound);
        }

        var offerDiscounts = await _context.LoadBestOfferDiscountByProductAsync(
            cartProducts.Select(cp => cp.Product!.Id).ToList(), DateTime.UtcNow, cancellationToken);

        var order = CreateOrder(user, request, address, cart, offerDiscounts);
        if (request.PaymentMethod != PaymentMethod.COD)
            order.RecordStatus("Awaiting online payment");
        _context.Orders.Add(order);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Lost a race against a concurrent checkout — the partial unique
            // index ux_orders_one_active_per_user is the source of truth.
            return Result.Failure<CheckoutResponse>(OrderingErrors.Order.OngoingExists);
        }

        foreach (var cartProduct in cartProducts)
            cartProduct.Product!.Quantity -= cartProduct.Quantity;

        switch (request.PaymentMethod)
        {
            case PaymentMethod.COD:
                order.Status = OrderStatus.Processing;
                order.RecordStatus("COD order placed");
                ClearCart(cart);
                await _context.SaveChangesAsync(cancellationToken);
                await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "COD order placed", DateTime.UtcNow);
                return Result.Succeed(new CheckoutResponse(MapOrder(order), null));

            case PaymentMethod.CreditCard:
            case PaymentMethod.MobileWallet:
                var paymentResult = await _paymobService.PayAsync(order, request.Identifier!, cancellationToken);
                if (paymentResult.IsFailure)
                    return Result.Failure<CheckoutResponse>(paymentResult.Error);

                order.RecordStatus("Payment session opened");
                await _context.SaveChangesAsync(cancellationToken);
                await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "Awaiting online payment", DateTime.UtcNow);
                return Result.Succeed(new CheckoutResponse(null, paymentResult.Value));

            default:
                return Result.Failure<CheckoutResponse>(Error.BadRequest("Order.InvalidPaymentMethod", "Unsupported payment method"));
        }
    }

    public async Task<Result> CancelAsync(string userId, int orderId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.OrderProducts).ThenInclude(op => op.Product)
            .AsQueryable();

        var order = await query.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || (!actor.IsStaff() && order.UserId != userId) || order.DeletedAt is not null)
            return Result.Failure(OrderingErrors.Order.NotFound);

        if (order.Status != OrderStatus.Processing)
            return Result.Failure(OrderingErrors.Order.NotCancellable);

        foreach (var orderProduct in order.OrderProducts)
            if (orderProduct.Product is not null)
                orderProduct.Product.Quantity += orderProduct.Quantity;

        order.Status = OrderStatus.Cancelled;
        order.RecordStatus("Order cancelled");
        order.DeletedAt = DateTime.UtcNow;

        _context.DeletesHistory.Add(new DeleteHistory
        {
            DeleterId = actor.GetUserId(),
            EntityType = nameof(Order),
            EntityId = order.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _trackingNotifier.NotifyStatusAsync(order.Id, order.Status, "Order cancelled", DateTime.UtcNow);
        return Result.Succeed();
    }

    private Order CreateOrder(
        ApplicationUser user,
        CheckoutRequest request,
        Address address,
        Cart cart,
        IReadOnlyDictionary<int, int> offerDiscounts)
    {
        var feeCents = request.DeliveryNeeded ? DeliveryFeeCents : 0;
        feeCents += request.PaymentMethod == PaymentMethod.COD ? CodFeeCents : 0;

        var subtotalCents = 0L;
        var orderProducts = new List<OrderProduct>(cart.CartProducts.Count);

        foreach (var cartProduct in cart.CartProducts)
        {
            var product = cartProduct.Product!;
            var (salePercent, finalPriceCents) = OfferPricing.EffectivePricing(product, offerDiscounts);
            subtotalCents += finalPriceCents * cartProduct.Quantity;

            orderProducts.Add(new OrderProduct
            {
                ProductId = product.Id,
                ProductPriceCents = product.PriceCents,
                SalePercent = salePercent,
                Quantity = cartProduct.Quantity,
                WarrantyDays = product.WarrantyDays
            });
        }

        var discountCents = 0L;
        if (cart.PromoCode is { Active: true } promo)
        {
            discountCents = promo.MaxSaleCents is null
                ? subtotalCents * promo.Percent / 100
                : Math.Min(subtotalCents * promo.Percent / 100, promo.MaxSaleCents.Value);
        }

        return new Order
        {
            UserId = user.Id,
            PromoCodeId = cart.PromoCode?.Active == true ? cart.PromoCodeId : null,
            TotalCents = feeCents + subtotalCents - discountCents,
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.Paying,
            DeliveryNeeded = request.DeliveryNeeded,
            AddressId = address.Id,
            OrderProducts = orderProducts
        };
    }

    private void ClearCart(Cart cart)
    {
        cart.PromoCodeId = null;
        _context.CartProducts.RemoveRange(cart.CartProducts);
    }

    private static OrderResponse MapOrder(Order o)
        => new(
            o.Id,
            o.TotalCents,
            o.Currency,
            o.PaymentMethod,
            o.DeliveryNeeded,
            o.Status,
            o.CreatedAt,
            o.DeliveredAt,
            ToAddressResponse(o.Address!),
            [.. o.OrderProducts.Select(op => new OrderProductResponse(
                op.ProductId,
                op.Product?.Name ?? string.Empty,
                op.Product?.Sku ?? string.Empty,
                op.ProductPriceCents,
                op.SalePercent,
                op.ProductPriceCents * (100 - op.SalePercent) / 100,
                op.Quantity,
                op.WarrantyDays))]);

    private static AddressResponse ToAddressResponse(Address a)
        => new(a.Id, a.Apartment, a.Floor, a.Building, a.Street, a.City, a.State, a.Country, a.PostalCode);
}
