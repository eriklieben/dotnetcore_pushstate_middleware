using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AspNetCorePushState
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        
        public IConfigurationRoot Configuration { get; private set; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            
            app.UseIISPlatformHandler();
            app.UseDefaultFiles();
            app.UseStaticFilesPushState();
        }

        // Entry point for the application.
        public static void Main(string[] args) => Microsoft.AspNet.Hosting.WebApplication.Run<Startup>(args);
    }
    
       public class PushStateStaticFileMiddleware  {
        
        private RequestDelegate next;
        private StaticFileMiddleware middleware;
        
        private ILogger logger;
        
        public PushStateStaticFileMiddleware(RequestDelegate next, 
        IHostingEnvironment hostingEnv, StaticFileOptions options, ILoggerFactory loggerFactory) 
        {
            this.logger = loggerFactory.CreateLogger("PushState");
            
            this.next = next;
            this.middleware = new StaticFileMiddleware(next, hostingEnv, options, loggerFactory);
            
        }
        
        public Task Invoke(HttpContext context) {
            
            this.middleware.Invoke(context).ContinueWith(task => {
                if (context.Response.StatusCode == 404) {

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Request.Path = "/index.html";
                    
                    this.logger.LogWarning("Virtual route, redirecting to index.html");
                    
                    this.middleware.Invoke(context);
                }
            }).Wait();

            return this.next(context);
        }
    }
    
    public static class StaticFileExtensions
    {
        /// <summary>
        /// Enables static file serving for the current request path
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFilesPushState(this IApplicationBuilder builder)
        {
            return builder.UseStaticFilesPushState(new StaticFileOptions());
        }

        /// <summary>
        /// Enables static file serving for the given request path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFilesPushState(this IApplicationBuilder builder, string requestPath)
        {
            return builder.UseStaticFilesPushState(new StaticFileOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables static file serving with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFilesPushState(this IApplicationBuilder builder, StaticFileOptions options)
        {
            return builder.UseMiddleware<PushStateStaticFileMiddleware>(options);
        }
    }
}
