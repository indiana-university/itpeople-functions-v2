using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Entity
    {
        /// The unique ID of this class.
        [Required] public int Id { get; set; }
    }
}