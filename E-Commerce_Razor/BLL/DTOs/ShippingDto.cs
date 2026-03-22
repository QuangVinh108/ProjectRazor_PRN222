namespace BLL.DTOs
{
    public class ShippingDto
    {
        public int ShippingId { get; set; }
        public int OrderId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public int? ShipperId { get; set; }
        public string? ShipperName { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool IsDisputed { get; set; }
        public DateTime? DisputeReportedAt { get; set; }
    }
}
