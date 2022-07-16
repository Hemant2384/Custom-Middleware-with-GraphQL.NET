using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQLMIddleware
{
    public class MyGraphQLMiddleware : IMiddleware
    {
        private readonly GraphQLSettings _settings;
        private readonly IDocumentExecuter _executer;
        private readonly IGraphQLSerializer _serializer;
        private readonly ISchema _schema;

        public MyGraphQLMiddleware(
            IOptions<GraphQLSettings> options,
            IDocumentExecuter executer,
            IGraphQLSerializer serializer,
            ISchema schema)
        {
            _settings = options.Value;
            _executer = executer;
            _serializer = serializer;
            _schema = schema;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!IsGraphQLRequest(context))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            await ExecuteAsync(context).ConfigureAwait(false);
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(_settings.GraphQLPath)
                && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
        }


        //Every request delegate accepts a HttpContext.
        //If the query is posted over an HTTP request, you can easily read the request
        //body as JSON using the following code and parse it to GraphQLRequest,
        private async Task ExecuteAsync(HttpContext context)
        {
            var start = DateTime.UtcNow;

            //Parsing the request body from JSON to a GraphQLRequest
            var request = await _serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted).ConfigureAwait(false);

            //IDocumentExecuter is used to execute the query which returns a Execution result to the 
            //context response body
            var result = await _executer.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = request.Query;
                options.OperationName = request.OperationName;
                options.Variables = request.Variables;
                options.UserContext = _settings.BuildUserContext?.Invoke(context);
                options.EnableMetrics = _settings.EnableMetrics;
                options.RequestServices = context.RequestServices;
                options.CancellationToken = context.RequestAborted;
            }).ConfigureAwait(false);

            if (_settings.EnableMetrics)
            {
                //allowing metrics to be captured
                result.EnrichWithApolloTracing(start);
            }

            await WriteResponseAsync(context, result, context.RequestAborted).ConfigureAwait(false);
        }

        private async Task WriteResponseAsync(HttpContext context, ExecutionResult result, CancellationToken cancellationToken)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200; // OK

            await _serializer.WriteAsync(context.Response.Body, result, cancellationToken).ConfigureAwait(false);
        }
    }
}
