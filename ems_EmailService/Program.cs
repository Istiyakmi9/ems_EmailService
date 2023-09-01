
using EmailRequest;
using EmailRequest.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var startup = new Startup(builder);
startup.ConfigureServices(builder.Services);

builder.Services.AddHostedService<KafkaService>();
var app = builder.Build();

startup.Configure(app);
