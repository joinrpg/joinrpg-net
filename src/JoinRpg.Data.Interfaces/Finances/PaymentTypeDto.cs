using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    public class PaymentTypeDto
    {
        public int PaymentTypeId { get; set; }
        public string Name { get; set; }

        public PaymentTypeKind TypeKind { get; set; }
    }
}
