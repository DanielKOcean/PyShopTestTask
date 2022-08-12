namespace GrpcService.Models
{
    public class Coin
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public int UserId { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ICollection<CoinTransaction> Transactions { get; set; } = new List<CoinTransaction>();
    }
}
