using DeveloperChallenge;
using Microsoft.EntityFrameworkCore;
using Sqids;

namespace HotelsAPIs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddSingleton(new SqidsEncoder<int>(new()
            {
                Alphabet = "2pKB0eLxIhfd5GMH3qQREN9XaVPl7bUDtzZFoAjiwv6WgYumrcJ14yCnskT8SO",
                MinLength = 8,
            }));
            builder.Services.AddDbContext<DataAccess.HotelsDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            builder.Services.AddScoped<IHotelService, HotelService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IRoomBookingService, RoomBookingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            { 
                TestDataSeeding.RefreshData(builder.Configuration.GetConnectionString("DefaultConnection")).Wait();
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "Hotels Api");
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
