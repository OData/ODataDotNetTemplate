using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
#if (EnableDefaultODataBatch)
using Microsoft.AspNetCore.OData.Batch;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (EnableOpenAPI)
using Microsoft.OpenApi.Models;
#endif

namespace ODataWebApi.WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Learn more about configuring OData at https://learn.microsoft.com/odata/webapi-8/getting-started
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddOData(opt =>
            {
#if (EnableDefaultODataBatch)
                DefaultODataBatchHandler defaultBatchHandler = new DefaultODataBatchHandler();
                defaultBatchHandler.MessageQuotas.MaxNestingDepth = 2;
                defaultBatchHandler.MessageQuotas.MaxOperationsPerChangeset = 10;
                defaultBatchHandler.MessageQuotas.MaxReceivedMessageSize = 100;

                opt.AddRouteComponents(
                        routePrefix: "odata",
                        model: EdmModelBuilder.GetEdmModel(),
                        batchHandler: defaultBatchHandler)
#else
                opt.AddRouteComponents("odata", EdmModelBuilder.GetEdmModel())
#endif
#if (IsQueryOptionAll)
                    .EnableQueryFeatures(100);
#else
#if (IsQueryOptionSelect)
                    .Select()
#endif
#if (IsQueryOptionFilter)
                    .Filter()
#endif
#if (IsQueryOptionExpand)
                    .Expand()
#endif
#if (IsQueryOptionOrderby)
                    .OrderBy()
#endif
#if (IsQueryOptionCount)
                    .Count()
#endif
                    .SetMaxTop(100);
#endif

#if (EnableCaseInsensitive)
                opt.RouteOptions.EnableControllerNameCaseInsensitive = true;
                opt.RouteOptions.EnableActionNameCaseInsensitive = true;
                opt.RouteOptions.EnablePropertyNameCaseInsensitive = true;
#endif
                opt.RouteOptions.EnableNonParenthesisForEmptyParameterFunction = true;
#if (!EnableNoDollarQueryOptions)
                opt.EnableNoDollarQueryOptions = false;
#endif
            });

#if (EnableOpenAPI)
            // Learn more about configuring Swagger/OpenAPI at https://github.com/OData/AspNetCoreOData/tree/main/sample/ODataRoutingSample
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ODataWebApi.WebApplication1", Version = "v1" });
            });
#if (!IsNet6Or8)

            // OpenAPI
            builder.Services.AddOpenApi();
#endif
#endif

            var app = builder.Build();
#if (EnableDefaultODataBatch)
            app.UseODataBatching();
#endif

#if (ConfigureHttps)
            app.UseHttpsRedirection();
#endif

            if (app.Environment.IsDevelopment())
            {
                app.UseODataRouteDebug();
#if (EnableOpenAPI)
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ODataWebApi.WebApplication1 V1"));
#if (!IsNet6Or8)

                // OpenAPI
                app.MapOpenApi();
#endif
#endif

                app.UseRouting();

                app.MapControllers();

                app.Run();
            }
        }
    }
}
