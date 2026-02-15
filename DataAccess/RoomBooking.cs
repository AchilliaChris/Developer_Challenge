namespace DataAccess
{
   public class RoomBooking
    {
        public int RoomBookingId { get; set; }
        public int Booking_Id { get; set; }
        public int Room_Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Booking Booking { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public ICollection<GuestBooking> GuestBookings { get; set; } = null!;
    }
}
