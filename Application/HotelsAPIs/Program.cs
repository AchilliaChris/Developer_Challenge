using DeveloperChallenge;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sqids;
using System.Threading.RateLimiting;

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
            builder.Services.AddRateLimiter(options =>
            {
                // basic fixed window rate limiting strategy, allowing 4 requests per 12 seconds, with a queue of 2 and processing order of oldest first
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = 4;
                    opt.Window = TimeSpan.FromSeconds(12);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 2;
                });
                // sliding window rate limiting strategy, allowing 4 requests per 12 seconds, with a queue of 2 and processing order of oldest first
                options.AddSlidingWindowLimiter("sliding", opt =>
                {
                    opt.PermitLimit = 4;
                    opt.Window = TimeSpan.FromSeconds(12);
                    opt.SegmentsPerWindow = 3;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 2;
                });
                // token bucket rate limiting strategy - used for bursts such as file uploads. Allowing 4 tokens per 12 seconds, with a queue of 2 and processing order of oldest first
                options.AddTokenBucketLimiter("token", opt =>
                {
                    opt.TokenLimit = 4;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 2;
                    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(12);
                    opt.TokensPerPeriod = 4;
                });
                // set the default rate limiting strategy to be used when no specific strategy is specified for an endpoint
                options.RejectionStatusCode = 429; // Too Many Requests
                // concurrency limiter strategy, allowing up to 4 concurrent requests, with a queue of 2 and processing order of oldest first
                options.AddConcurrencyLimiter("concurrency", opt =>
                {
                    opt.PermitLimit = 4; // allow up to 4 concurrent requests
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 2; // allow up to 2 requests to queue when the limit is reached
                });

                // when a request is rejected due to rate limiting, add a Retry-After header to indicate when the client can retry
                options.OnRejected = (context, cancellationToken) =>
                {
                    context.HttpContext.Response.Headers.RetryAfter = "12"; // clients should retry after 12 seconds
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                };
            });
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

            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}
