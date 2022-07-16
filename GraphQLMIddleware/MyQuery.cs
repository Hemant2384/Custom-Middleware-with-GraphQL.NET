using GraphQL;
using GraphQL.Types;
using GraphQLMIddleware.Model;
using GraphQLMIddleware.Repository;
using GraphQLMIddleware.Types;
namespace GraphQLMIddleware
{
    public class MyQuery : ObjectGraphType
    {
        public MyQuery(GenericRepository<Product> genericRepository)
        {
            Field<ListGraphType<ProductType>>( //type of field
                "products", //name and description shows up in schema docs and name used for querying
                resolve: context => genericRepository.GetAll() // resolver tells where to get data from
            );

            //getting prod details by id
            Field<ProductType>(
                "product",
                //creating an non null argument for "product" query with product id as the parameter
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" }),
                resolve: context =>
                {
                    var id = context.GetArgument<int>("id"); //getting the id from parameter in query using context object and get argument method
                    return genericRepository.GetById(id);
                }
            );
        }
    }
}
