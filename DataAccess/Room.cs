namespace DataAccess
{
    public class Room
    {
        public int RoomId { get; set; }
        public int Hotel_Id { get; set; }
        public int RoomTypeId { get; set; }
        public int RoomNumber { get; set; }
        public double PricePerNight { get; set; }
        public int Capacity { get; set; } = 0;
        public List<RoomBooking> RoomBookings { get; set; }
        public Hotel Hotel { get; set; }
    }
}
