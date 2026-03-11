namespace DataAccess
{
    public class Hotel
    {
        public int HotelId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public List<Room> Rooms { get; set; } = new();
    }
}
