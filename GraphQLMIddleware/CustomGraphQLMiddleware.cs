using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace GraphQLMIddleware
{
    //ASP.NET Core middleware for processing GraphQL requests.
    //Type of GraphQL schema that is used to validate and process requests.
    public class CustomGraphQLMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema>where TSchema : ISchema
    {        
        public CustomGraphQLMiddleware(ILogger<CustomGraphQLMiddleware<TSchema>> logger, 
            IGraphQLTextSerializer requestDeserializer)
            : base(requestDeserializer)
        {
            _logger = logger;
            _serializer = requestDeserializer;
        }
        private readonly ILogger _logger;
        private readonly IGraphQLTextSerializer _serializer;

        protected override Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            if (requestExecutionResult.Result.Errors != null)
            {
                if (requestExecutionResult.IndexInBatch.HasValue)
                    _logger.LogError("GraphQL execution completed in {Elapsed} with error(s) in batch [{Index}]: {Errors}", 
                        requestExecutionResult.Elapsed,
                        requestExecutionResult.IndexInBatch,
                        requestExecutionResult.Result.Errors);
                else
                    _logger.LogError("GraphQL execution completed in {Elapsed} with error(s): {Errors}",
                        requestExecutionResult.Elapsed, requestExecutionResult.Result.Errors);
            }
            else
                _logger.LogInformation("GraphQL execution successfully completed in {Elapsed}",
                    requestExecutionResult.Elapsed);

            return base.RequestExecutedAsync(requestExecutionResult);
        }
        protected override CancellationToken GetCancellationToken(HttpContext context)
        {
            // custom CancellationToken example
            var cts = CancellationTokenSource.CreateLinkedTokenSource(base.GetCancellationToken(context), 
                new CancellationTokenSource(TimeSpan.FromSeconds(500)).Token);
            return cts.Token;
        }
        protected override Task WriteResponseAsync<TResult>(HttpResponse httpResponse, IGraphQLSerializer serializer, CancellationToken cancellationToken, TResult result)
        {
            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = 200;
             return serializer.WriteAsync(httpResponse.Body, result, cancellationToken);
        }
    }
}
