using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class HotelsDbContext : DbContext
    {
        public HotelsDbContext(DbContextOptions<HotelsDbContext> options) : base(options)
        {
        }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<RoomBooking> RoomBookings { get; set; }
        public DbSet<GuestBooking> GuestBookings { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Rooms)
                .WithOne(r => r.Hotel)
                .HasForeignKey(r => r.Hotel_Id);

            modelBuilder.Entity<Room>()
                .HasMany(r => r.RoomBookings)
                .WithOne(r => r.Room)
                .HasForeignKey(r => r.Room_Id);

            modelBuilder.Entity<RoomBooking>()
                  .HasMany(rb => rb.GuestBookings)
                  .WithOne(gb => gb.RoomBooking)
                  .HasForeignKey(gb => gb.RoomBooking_Id);

            modelBuilder.Entity<Customer>()
                .HasMany<Booking>()
                .WithOne(c => c.Customer)
                .HasForeignKey(b => b.Customer_Id);

            modelBuilder.Entity<Customer>()
            .HasMany<GuestBooking>()
            .WithOne(c => c.Guest)
            .HasForeignKey(g => g.GuestId)
            .OnDelete(DeleteBehavior.ClientNoAction);

            modelBuilder.Entity<Booking>()
                .HasMany(b => b.RoomBookings)
                .WithOne(b => b.Booking)
                .HasForeignKey(b => b.Booking_Id);




        }

    }
}
