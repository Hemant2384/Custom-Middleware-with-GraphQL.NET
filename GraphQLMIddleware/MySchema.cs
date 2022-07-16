using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using System;

namespace GraphQLMIddleware
{
    public class MySchema : Schema
    {
        public MySchema(IServiceProvider services, MyQuery query) : base(services)
        {
            Query = query;
        }
    }
}
