using Hangfire;
using Hangfire.Extensions.Configuration;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.LiteDB;
using Hangfire.MemoryStorage;
using Hangfire.Redis;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hangfire.Server.Configurations.Hangfire
{
    public static class HangfireConfiguration
    {

        public static void AddHangfireConfiguration(this IServiceCollection services,
                                                    IConfiguration configuration)
        {

            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseColouredConsoleLogProvider();
                //config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();



                // Heartbeat
                config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(1));

                #region IoC

                config.UseDefaultActivator();


                #endregion

                #region Storage

                string applicationStoragePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "storage");

                if (configuration.GetSection("Hangfire:Storage").Value?.ToLower() != "memory")
                {
                    if (!Directory.Exists(applicationStoragePath))
                        Directory.CreateDirectory(applicationStoragePath);
                }

                switch (configuration.GetSection("Hangfire:Storage").Value?.ToLower())
                {

                    case "sqlite":

                        #region SQLite                        

                        var SQliteOptions = new SQLiteStorageOptions()
                        {
                            SchemaName = "hangfire",
                            PrepareSchemaIfNecessary = true,
                            TransactionIsolationLevel = System.Data.IsolationLevel.Serializable,
                            QueuePollInterval = TimeSpan.FromSeconds(1),
                            //JobExpirationCheckInterval = TimeSpan.FromHours(1),
                            //CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        };

                        var SQliteConnection = new SqliteConnection(string.Format("Data Source={0}\\hangfire.sqlite;", applicationStoragePath));

                        config.UseSQLiteStorage(SQliteConnection.ConnectionString, SQliteOptions);

                        #endregion

                        break;
                    case "litedb":

                        #region LiteDB

                        var LiteDBOption = new LiteDbStorageOptions()
                        {

                        };

                        config.UseLiteDbStorage(string.Format("Filename={0}\\hangfire.db;", applicationStoragePath), LiteDBOption);

                        #endregion

                        break;
                    case "redis":

                        #region Redis

                        var redisConnection = ConnectionMultiplexer.Connect(configuration.GetSection("Hangfire:Redis:ConnectionString").Value);
                        var redisStorageOptions = new RedisStorageOptions()
                        {
                            Db = 0,
                            Prefix = "hangfire:",
                            SucceededListSize = 100000
                        };

                        config.UseRedisStorage(redisConnection, redisStorageOptions);

                        #endregion

                        break;

                    default:

                        #region Memory

                        config.UseMemoryStorage(new MemoryStorageOptions()
                        {
                            JobExpirationCheckInterval = TimeSpan.FromMinutes(5),
                            //CountersAggregateInterval = TimeSpan.FromMinutes(20),
                            //FetchNextJobTimeout = TimeSpan.FromMinutes(30)
                        });

                        #endregion

                        break;
                }

                #endregion

                //config.UseRecurringJob("recurringjob.json", reloadOnChange: true);
                //config.UseDefaultActivator();



            });
        }

        public static IApplicationBuilder UseHangfireConfiguration(this IApplicationBuilder app,
                                                                  IWebHostEnvironment env,
                                                                  IConfiguration configuration)
        {

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[]
                {
                    new HangfireAuthorizationFilter()
                },
                //AppPath = null,
                //IsReadOnlyFunc = (DashboardContext context) => false
                DashboardTitle = "Hangfire",
            });

            app.UseHangfireServer(
                configuration.GetHangfireBackgroundJobServerOptions(),
                additionalProcesses: new[] { new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(1)) }
            );

            return app;

        }

    }
}
