using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using EmailRequest.EMailService.Interface;
using EmailRequest.EMailService.Service;
using EmailRequest.MIddleware;
using EmailRequest.Service;
using EmailRequest.Service.TemplateService;
using EmalRequest.Service;

namespace EmailRequest
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // add services
            services.AddScoped<IEMailManager, EMailManager>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IDb, Db>(x =>
            {
                var db = new Db();
                db.SetupConnectionString("server=192.168.0.101;port=3306;database=onlinedatabuilder;User Id=istiyak;password=live@Bottomhalf_001;Connection Timeout=30;Connection Lifetime=0;Min Pool Size=0;Max Pool Size=100;Pooling=true;");
                return db;
            });

            services.AddScoped<BillingTemplate>();
        }
        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseMiddleware<KafkaMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
