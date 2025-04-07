using System.ComponentModel.DataAnnotations;

namespace Micon.LotterySystem.Models
{
    public class BaseModel
    {
        public BaseModel()
        {
            Id = Guid.NewGuid();
            Created = DateTimeOffset.Now;
            Updated = DateTimeOffset.Now;
        }
        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
    }
}
