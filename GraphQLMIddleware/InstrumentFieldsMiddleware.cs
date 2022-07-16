using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQLParser.AST;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQLMIddleware
{
    public class InstrumentFieldsMiddleware : IFieldMiddleware
    {
        //Handles execution of a field.
        public async ValueTask<object> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object>
            {
                {"typeName", context.ParentType.Name},
                {"fieldName", context.FieldDefinition.Name},
                {"path",context.Path },
                {"arguments",context.Arguments },
                //{"metrics" , context.Metrics }
            };
            var path = $"{context.ParentType.Name}.{context.FieldDefinition.Name}";
            using (context.Metrics.Subject("field",path, metadata))
            {
                return await next(context).ConfigureAwait(false);
            }
        }
    }
}