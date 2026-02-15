using System.ComponentModel;

namespace DataAccess
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int Customer_Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public double TotalPrice { get; set; } = 0;
        [DefaultValue("false")]
        public bool Cancelled { get; set; } = false;
        public Customer Customer { get; set; } = null!;
        public ICollection<RoomBooking> RoomBookings { get; set; } = null!;

    }
}
