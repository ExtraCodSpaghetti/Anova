using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Anova_Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [DisplayName("Name of category")]
        [Required]
        public string CategoryName { get; set; }

        [DisplayName("Display Order")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage ="Display Order for category must be greater then 0")]
        public int DisplayOrder { get; set; }

    }
}
