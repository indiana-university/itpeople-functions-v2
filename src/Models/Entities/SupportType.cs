using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SupportType : Entity 
    { 
        /// The Name of the Support Type
        [Required]
        public string Name { get; set; }
    }

}