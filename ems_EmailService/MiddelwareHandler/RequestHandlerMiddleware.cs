using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Modal;
using ModalLayer.Modal;
using Newtonsoft.Json;

namespace EmailRequest.MiddelwareHandler
{
    public static class RequestHandler
    {
        public static IApplicationBuilder UseRequestHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestHandlerMiddleware>();
        }
    }

    public class RequestHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        
        public RequestHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IDb db)
        {
            try
            {
                DbConfigModal dbConfig = null;
                Parallel.ForEach(context.Request.Headers, header =>
                {
                    if (header.Value.FirstOrDefault() != null)
                    {
                        if (header.Key == "database")
                        {
                            dbConfig = JsonConvert.DeserializeObject<DbConfigModal>(header.Value);
                        }
                    }
                });

                if (string.IsNullOrEmpty(dbConfig.Server))
                {
                    throw new Exception("Unable to get database server detail. Please contact to admin.");
                }

                var cs = @$"server={dbConfig.Server};port={dbConfig.Port};database={dbConfig.Database};User Id={dbConfig.UserId};password={dbConfig.Password};Connection Timeout={dbConfig.ConnectionTimeout};Connection Lifetime={dbConfig.ConnectionLifetime};Min Pool Size={dbConfig.MinPoolSize};Max Pool Size={dbConfig.MaxPoolSize};Pooling={dbConfig.Pooling};";
                db.SetupConnectionString(cs);

                await _next(context);
            }
            catch (HiringBellException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
