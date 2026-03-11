namespace DataAccess
{
    public class GuestBooking
    {
        public int GuestBookingId { get; set; }
        public int RoomBooking_Id { get; set; }
        public int GuestId { get; set; }
        public RoomBooking RoomBooking { get; set; } = null!;
        public Customer Guest { get; set; } = null!;
    }
}
