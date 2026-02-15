namespace DataAccess
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int Booking_Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
