using System.Collections.Generic;

namespace Anova_Models.ViewModels
{
    public class HomeVM
    {
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<ApplicationType> ApplicationType { get; set; }
    }
}