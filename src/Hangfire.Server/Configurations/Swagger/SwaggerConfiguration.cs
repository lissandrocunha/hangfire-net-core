using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Server.Configurations.Swagger
{
    public static class SwaggerConfiguration
    {
        public static void AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Hangfire",
                    Description = "Aplicações de controle de execução de Jobs",
                    //TermsOfService = new Uri("none"),
                    Contact = new OpenApiContact()
                    {
                        Name = "Lissandro Perossi Cunha",
                        Email = "lissandro_cunha@yahoo.com.br",
                        //Url = new Uri("")
                    },
                    License = new OpenApiLicense()
                    {
                        Name = "",
                        //Url = new Uri("")
                    }
                });

                options.EnableAnnotations();

                options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
                options.OperationFilter<FileUploadOperation>(); //Register File Upload Operation Filter

                //options.AddSecurityDefinition(
                //    );
            });

            services.ConfigureSwaggerGen(options =>
            {
                options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });
        }

        public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app,
                                                                  IWebHostEnvironment env,
                                                                  IConfiguration configuration)
        {

            if (env.IsProduction())
            {
                // Se não tiver um token válido no browser não funciona.
                // Descomente para ativar a segurança.
                //app.UseSwaggerAuthorized();
            }

            // Habilitar o middleware para servir o Swagger gerado como um terminal JSON.
            app.UseSwagger();
            // Habilitar o middleware para servir swagger-ui (HTML, JS, CSS, etc.)
            // especificando o endpoint JSON do Swagger.
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hangfire v1.0");
                options.RoutePrefix = "swagger";
            });

            return app;
        }

    }

    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId?.ToLower() == "apivaluesuploadpost")
            {
                operation.Parameters.Clear();
                //operation.Parameters.Add(new NonBodyParameter
                //{
                //    Name = "Hangfire.Job",
                //    In = "formData",
                //    Description = "arquvio zip contendo o job a ser instalado",
                //    Required = true,
                //    Type = ".zip"
                //});
                //operation.Consumes.Add("multipart/form-data");
            }
        }

    }
}
