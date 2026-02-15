using Microsoft.EntityFrameworkCore;

namespace HotelsAPIs
{
    public static class TestDataSeeding
    {
        public static void CleanData(string connectionString)
        {
            using (var context = new DataAccess.HotelsDbContext(
                new DbContextOptionsBuilder<DataAccess.HotelsDbContext>()
                .UseSqlServer(connectionString)
                .Options))
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Rooms Drop Constraint FK_Rooms_Hotels_Hotel_Id");
                context.Database.ExecuteSqlRaw("ALTER TABLE RoomBookings Drop Constraint FK_RoomBookings_Rooms_Room_Id");
                context.Database.ExecuteSqlRaw("ALTER TABLE RoomBookings Drop Constraint FK_RoomBookings_Bookings_Booking_Id");
                context.Database.ExecuteSqlRaw("ALTER TABLE Bookings Drop Constraint FK_Bookings_Customers_Customer_Id");
                context.Database.ExecuteSqlRaw("ALTER TABLE GuestBookings Drop Constraint FK_GuestBookings_RoomBookings_RoomBooking_Id");
                context.Database.ExecuteSqlRaw("ALTER TABLE GuestBookings Drop Constraint FK_GuestBookings_Customers_GuestId");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE Payments");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE RoomBookings");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE GuestBookings");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE Bookings");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE Customers");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE Rooms");
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE Hotels");
                context.Database.ExecuteSqlRaw("ALTER TABLE Rooms Add Constraint FK_Rooms_Hotels_Hotel_Id Foreign Key (Hotel_Id) references Hotels(HotelId) ON DELETE CASCADE");
                context.Database.ExecuteSqlRaw("ALTER TABLE RoomBookings Add Constraint FK_RoomBookings_Rooms_Room_Id Foreign Key (Room_Id) references Rooms(RoomId) ON DELETE CASCADE");
                context.Database.ExecuteSqlRaw("ALTER TABLE RoomBookings Add Constraint FK_RoomBookings_Bookings_Booking_Id Foreign Key (Booking_Id) references Bookings(BookingId) ON DELETE CASCADE");
                context.Database.ExecuteSqlRaw("ALTER TABLE Bookings Add Constraint FK_Bookings_Customers_Customer_Id Foreign Key ( Customer_Id ) references Customers(CustomerId)  ON DELETE CASCADE");
                context.Database.ExecuteSqlRaw("ALTER TABLE GuestBookings Add Constraint FK_GuestBookings_RoomBookings_RoomBooking_Id Foreign Key (RoomBooking_Id) references RoomBookings(RoomBookingId) ON DELETE CASCADE");
                context.Database.ExecuteSqlRaw("ALTER TABLE GuestBookings Add Constraint FK_GuestBookings_Customers_GuestId Foreign Key (GuestId) references Customers(CustomerId) ON DELETE NO ACTION");
            }
        }

        public static async Task RefreshData(string connectionString)
        {
            CleanData(connectionString);
            using (var context = new DataAccess.HotelsDbContext(
                new DbContextOptionsBuilder<DataAccess.HotelsDbContext>()
                .UseSqlServer(connectionString)
                .Options))
            {
                await context.Hotels.AddRangeAsync(
                      new DataAccess.Hotel { Name = "Grand Plaza", Address = "123 Main St, Cityville", Phone = "+44 1234 56789123" },
                      new DataAccess.Hotel { Name = "Mardon Villa", Address = "28 High St, Redtown", Phone = "+44 1417 9258465" },
                      new DataAccess.Hotel { Name = "Hilton Heights", Address = "425 Main Rd, Bluefield", Phone = "+44 1187 62549785" }
                      );
                await context.SaveChangesAsync();

                await context.Customers.AddRangeAsync(
                         new DataAccess.Customer { FirstName = "John", LastName = "Doe", Address = "456 Elm St, Townsville", Email = "jdoe@highdon.com", Phone = "+44 1294 567890" },
                         new DataAccess.Customer { FirstName = "Hayley", LastName = "Tilsley", Address = "9 random Way, Middlebridge", Email = "htilsley@outlook.co.uk", Phone = "+44 1934 3451915" },
                         new DataAccess.Customer { FirstName = "Rachel", LastName = "Piemaker", Address = "45 Least Road, Kettleborough", Email = "rpiemaker@gmail.com", Phone = "+44 1454 9427584" },
                         new DataAccess.Customer { FirstName = "Paul", LastName = "Pope", Address = "91 Rude Avenue, Greatley", Email = "ppope@futuremail.co.uk", Phone = "+44 1917 2365548" },
                         new DataAccess.Customer { FirstName = "Jane", LastName = "Carter", Address = "75 Bell View, Hartlingshine", Email = "jcarter@gmail.com", Phone = "+44 1652 354584" }
                         );
                await context.SaveChangesAsync();

                await context.Rooms.AddRangeAsync(
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 1, RoomNumber = 1, PricePerNight = 75.00, Capacity = 1 },
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 2, RoomNumber = 2, PricePerNight = 155.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 2, RoomNumber = 3, PricePerNight = 150.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 3, RoomNumber = 4, PricePerNight = 175.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 2, RoomNumber = 5, PricePerNight = 150.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 1, RoomTypeId = 3, RoomNumber = 6, PricePerNight = 175.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 1, RoomNumber = 1, PricePerNight = 75.00, Capacity = 1 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 1, RoomNumber = 2, PricePerNight = 75.00, Capacity = 1 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 2, RoomNumber = 3, PricePerNight = 250.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 1, RoomNumber = 4, PricePerNight = 75.00, Capacity = 1 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 2, RoomNumber = 5, PricePerNight = 250.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 2, RoomTypeId = 2, RoomNumber = 6, PricePerNight = 250.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 3, RoomNumber = 1, PricePerNight = 250.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 1, RoomNumber = 2, PricePerNight = 175.00, Capacity = 1 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 3, RoomNumber = 3, PricePerNight = 275.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 3, RoomNumber = 4, PricePerNight = 275.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 3, RoomNumber = 5, PricePerNight = 275.00, Capacity = 2 },
                    new DataAccess.Room { Hotel_Id = 3, RoomTypeId = 3, RoomNumber = 6, PricePerNight = 275.00, Capacity = 2 }
                    );
                await context.SaveChangesAsync();

                await context.Bookings.AddRangeAsync(
                    new DataAccess.Booking { Customer_Id = 1, BookingReference = "PrhEjxxuk1Bnp", TotalPrice = 475, Cancelled = false },
                    new DataAccess.Booking { Customer_Id = 2, BookingReference = "Z26UtejKnmWtA", TotalPrice = 280, Cancelled = false },
                    new DataAccess.Booking { Customer_Id = 3, BookingReference = "XR1NHc5U9Fl74", TotalPrice = 1450, Cancelled = false }
                    );
                await context.SaveChangesAsync();

                await context.RoomBookings.AddRangeAsync(
                    new DataAccess.RoomBooking { Booking_Id = 1, Room_Id = 2, StartDate = new System.DateTime(2026, 7, 1), EndDate = new System.DateTime(2026, 7, 5) },
                    new DataAccess.RoomBooking { Booking_Id = 2, Room_Id = 3, StartDate = new System.DateTime(2026, 8, 10), EndDate = new System.DateTime(2026, 8, 15) },
                    new DataAccess.RoomBooking { Booking_Id = 3, Room_Id = 4, StartDate = new System.DateTime(2026, 9, 20), EndDate = new System.DateTime(2026, 9, 25) },
                    new DataAccess.RoomBooking { Booking_Id = 1, Room_Id = 3, StartDate = new System.DateTime(2026, 7, 1), EndDate = new System.DateTime(2026, 7, 5) },
                        new DataAccess.RoomBooking { Booking_Id = 2, Room_Id = 4, StartDate = new System.DateTime(2026, 8, 10), EndDate = new System.DateTime(2026, 8, 15) },
                        new DataAccess.RoomBooking { Booking_Id = 3, Room_Id = 5, StartDate = new System.DateTime(2026, 9, 20), EndDate = new System.DateTime(2026, 9, 25) }
                    );
                await context.SaveChangesAsync();

                await context.GuestBookings.AddRangeAsync(
                    new DataAccess.GuestBooking { RoomBooking_Id = 1, GuestId = 1 },
                    new DataAccess.GuestBooking { RoomBooking_Id = 2, GuestId = 2 },
                    new DataAccess.GuestBooking { RoomBooking_Id = 3, GuestId = 3 },
                    new DataAccess.GuestBooking { RoomBooking_Id = 4, GuestId = 4 },
                    new DataAccess.GuestBooking { RoomBooking_Id = 5, GuestId = 5 },
                    new DataAccess.GuestBooking { RoomBooking_Id = 6, GuestId = 1 }
                    );


                await context.SaveChangesAsync();
            }


        }
    }
}
