using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using EmailRequest.EMailService.Interface;
using EmailRequest.EMailService.Service;
using EmailRequest.Service;
using EmailRequest.Service.TemplateService;
using EmalRequest.Service;
using Microsoft.Extensions.FileProviders;
using ModalLayer;
using ModalLayer.Modal;

namespace EmailRequest
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private IWebHostEnvironment env;
        public Startup(WebApplicationBuilder builder)
        {
            env = builder.Environment;

            var config = builder.Configuration
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile($"appsettings.json", false, false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", false, false)
                .AddEnvironmentVariables();

            Configuration = config.Build();
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.Configure<KafkaServiceConfig>(x => Configuration.GetSection(nameof(KafkaServiceConfig)).Bind(x));

            // add services
            services.AddScoped<IEMailManager, EMailManager>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IDb, Db>(x =>
            {
                var db = new Db();
                var cs = Configuration.GetConnectionString("OnlinedatabuilderDb");
                db.SetupConnectionString(cs);
                return db;
            });

            services.AddScoped<BillingService>();
            services.AddScoped<AttendanceRequested>();
            services.AddScoped<AutoLeaveMigrationTemplate>();
            services.AddScoped<AttendanceAction>();
            services.AddScoped<ForgotPasswordRequested>();
            services.AddScoped<LeaveApprovalTemplate>();
            services.AddScoped<LeaveRequested>();
            services.AddScoped<NewRegistrationTemplate>();
            services.AddScoped<OfferLetterTemplate>();
            services.AddScoped<PayrollService>();
            services.AddScoped<TimesheetAction>();
            services.AddScoped<TimesheetRequested>();

            services.AddSingleton<FileLocationDetail>(service =>
            {
                var fileLocationDetail = Configuration.GetSection("BillingFolders").Get<FileLocationDetail>();
                string rootPath = fileLocationDetail.RootPath;
                if (fileLocationDetail.RootPath == "prod")
                {
                    rootPath = env.ContentRootPath;
                }

                var locationDetail = new FileLocationDetail
                {
                    RootPath = rootPath,
                    BillsPath = fileLocationDetail.BillsPath,
                    Location = fileLocationDetail.Location,
                    HtmlTemplatePath = Path.Combine(rootPath, fileLocationDetail.Location, fileLocationDetail.HtmlTemplatePath),
                    StaffingBillPdfTemplate = fileLocationDetail.StaffingBillPdfTemplate,
                    StaffingBillTemplate = fileLocationDetail.StaffingBillTemplate,
                    PaysliplTemplate = fileLocationDetail.PaysliplTemplate,
                    DocumentFolder = fileLocationDetail.Location,
                    UserFolder = Path.Combine(rootPath, fileLocationDetail.Location, fileLocationDetail.User),
                    BillFolder = Path.Combine(rootPath, fileLocationDetail.Location, fileLocationDetail.BillsPath),
                    LogoPath = Path.Combine(rootPath, fileLocationDetail.Location, fileLocationDetail.LogoPath)
                };

                return locationDetail;
            });
        }
        public void Configure(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                   Path.Combine(Directory.GetCurrentDirectory())),
                RequestPath = "/Files"
            });
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
