namespace ECommerce.Infrastructure.Errors;

public static class OrderingErrors
{
    public static class Order
    {
        public static readonly Error NotFound = Error.NotFound("Order.NotFound", "Order not found");
        public static readonly Error EmptyCart = Error.BadRequest("Order.EmptyCart", "Your cart is empty");
        public static readonly Error OngoingExists = Error.BadRequest("Order.OngoingExists", "You already have an ongoing order");
        public static readonly Error AddressNotFound = Error.NotFound("Order.AddressNotFound", "Address not found");
        public static readonly Error NotCancellable = Error.BadRequest("Order.NotCancellable", "Only processing orders can be cancelled");
        public static readonly Error InvalidStatusTransition = Error.BadRequest("Order.InvalidStatusTransition", "Invalid order status transition");
        public static readonly Error PaymentFailed = Error.BadRequest("Order.PaymentFailed", "Payment session could not be created");
        public static readonly Error TransporterRoleRequired = Error.BadRequest("Order.TransporterRoleRequired", "Assigned user must have the Transporter role");
    }

    public static class Return
    {
        public static readonly Error NotFound = Error.NotFound("Return.NotFound", "Return request not found");
        public static readonly Error AlreadyRequested = Error.Conflict("Return.AlreadyRequested", "A return was already requested for this item");
        public static readonly Error WarrantyExpired = Error.BadRequest("Return.WarrantyExpired", "The warranty period for this item has expired");
        public static readonly Error ExceedsQuantity = Error.BadRequest("Return.ExceedsQuantity", "Return quantity exceeds the remaining returnable quantity");
        public static readonly Error OrderNotDelivered = Error.BadRequest("Return.OrderNotDelivered", "Returned items must belong to a delivered order");
        public static readonly Error NotTransporter = Error.BadRequest("Return.NotTransporter", "Assigned user must have the Transporter role");
    }
}
