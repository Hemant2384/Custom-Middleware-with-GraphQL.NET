using GraphQL.Types;
using GraphQLMIddleware.Model;

namespace GraphQLMIddleware.Types
{
    public class ProductType : ObjectGraphType<Product>
    {
        public ProductType()
        {
            Field(t => t.Id);
            Field(t => t.ProductId);
            Field(t => t.Title);
            Field(t => t.Review).Description("When the product was first introduced in the catalog");
        }
    }
}
