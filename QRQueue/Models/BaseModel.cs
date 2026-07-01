using System.ComponentModel.DataAnnotations;

namespace QRQueue.Models
{
    public class BaseModel
    {
        public BaseModel()
        {
            Id = Guid.CreateVersion7();
            Created = DateTimeOffset.UtcNow;
            Updated = DateTimeOffset.UtcNow;
        }
        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
    }
}
