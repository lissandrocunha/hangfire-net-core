using Hangfire.Server.Configurations.Hangfire;
using Hangfire.Server.Configurations.Swagger;
using Hangfire.Server.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    public class Startup
    {

        #region Variables

        private IConfiguration _configuration;

        #endregion

        #region Constructors

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region Methods

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            // Configuração do Hangfire
            services.AddHangfireConfiguration(_configuration);

            // Configuração do Swagger
            services.AddSwaggerConfig();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireConfiguration(env, _configuration);

            app.UseSwaggerConfiguration(env, _configuration);

            // Redireciona para utilizar Https
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            JobManager.StartRecurringJobs();

            //Log.Warning("Hangfire iniciado as " + DateTime.Now + ".");

        }

        #endregion

    }
}
