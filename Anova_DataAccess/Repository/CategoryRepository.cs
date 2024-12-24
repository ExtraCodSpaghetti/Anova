using Anova_DataAccess.Repository.IRepository;
using Anova_Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anova_DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _db;

        public CategoryRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }
        public void Update(Category obj)
        {
           var objFromDb = base.FirstOrDefault(u=> u.Id == obj.Id);
            if (objFromDb != null)
            {
                objFromDb.CategoryName = obj.CategoryName;
                objFromDb.DisplayOrder = obj.DisplayOrder;
            }
        }
    }
}
