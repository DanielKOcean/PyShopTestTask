using GrpcService.Models;
using GrpcService.Services;
using Microsoft.EntityFrameworkCore;

namespace GrpcService1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            // Add services to the container.
            builder.Services.AddGrpc();

            builder.Services.AddDbContext<ApiContext>(opt => opt
                .UseInMemoryDatabase("ApiDatabase")); // Comment this line when use SQLite
                //.UseSqlite("Data Source=d:\\mydb.db;")); // Uncomment this line when use SQLite, don't forget to apply migrations.

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<BillingService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}