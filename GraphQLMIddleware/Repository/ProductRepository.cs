using GraphQLMIddleware.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQLMIddleware.Repository
{
    public class ProductRepository : GenericRepository<Product>
    {
        private readonly ProductContext _context;
        public ProductRepository(ProductContext context) : base(context)
        {
            _context = context;
        }
    }
}
