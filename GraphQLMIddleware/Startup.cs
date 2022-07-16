using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GraphQL;
using GraphQL.Server;
using GraphQLMIddleware.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GraphQL.MicrosoftDI;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQLMIddleware.Repository;
using GraphQL.DataLoader;
using GraphQL.Instrumentation;
using GraphQL.Execution;
using System;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace GraphQLMIddleware
{
    public class Startup
    {
        public Startup(IConfiguration configuration,IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            string connectionString = Configuration.GetConnectionString("SampleAppDbContext");
            services.AddDbContext<ProductContext>(options =>
                      options.UseSqlServer(connectionString));
            services.AddSingleton(typeof(IRepository<>), typeof(GenericRepository<>));
            services.Configure<ErrorInfoProviderOptions>(opt => 
            opt.ExposeExceptionStackTrace = Environment.IsDevelopment());
            
            //GraphQL Services
            services.AddGraphQL(b => b
            //.AddApolloTracing()
            //.AddExecutionStrategy<SerialExecutionStrategy>(OperationType.Query)
            .AddMiddleware<InstrumentFieldsMiddleware>(false)
            //.ConfigureExecutionOptions(options =>
            //{
            //    var logger = options.RequestServices.GetRequiredService<ILogger<Startup>>();
            //    options.UnhandledExceptionDelegate = ctx =>
            //    {
            //        logger.LogError("{Error} occurred", ctx.OriginalException.Message);
            //        return Task.CompletedTask;
            //    };
            //    options.EnableMetrics = true;
            //})
            .ConfigureSchema((schema, serviceProvider) =>
            {
                // install middleware only when the custom EnableMetrics option is set
                var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
                if (settings.Value.EnableMetrics)
                {
                    var middlewares = serviceProvider.GetRequiredService<IEnumerable<IFieldMiddleware>>();
                    foreach (var middleware in middlewares)
                        schema.FieldMiddleware.Use(middleware);
                }
            })
            .ConfigureExecution(async (options, next) =>
            {
                options.EnableMetrics = true;
                DateTime start = DateTime.UtcNow;
                var ret = await next(options).ConfigureAwait(false);
                if (options.EnableMetrics)
                {
                    ret.EnrichWithApolloTracing(start);
                }
                return ret;
            })
            .AddSchema<MySchema>(GraphQL.DI.ServiceLifetime.Scoped)
            .AddSystemTextJson()
            .AddErrorInfoProvider((opts, serviceProvider) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
                opts.ExposeExceptionStackTrace = settings.Value.ExposeExceptions;
            })
            .AddWebSockets()
            .AddGraphTypes(typeof(MySchema).Assembly)// registers graph types with transient lifetimes
            .AddDataLoader()
            .AddHttpMiddleware<MySchema,CustomGraphQLMiddleware<MySchema>>()
            .AddWebSocketsHttpMiddleware<MySchema>());
            services.AddCors();

            services.AddScoped<MyGraphQLMiddleware>();
            services.Configure<GraphQLSettings>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            app.UseWebSockets();
            app.UseGraphQLWebSockets<MySchema>("/graphql");
            app.UseGraphQL<MySchema,CustomGraphQLMiddleware<MySchema>>();
            app.UseGraphQLAltair();
        }
    }
}
