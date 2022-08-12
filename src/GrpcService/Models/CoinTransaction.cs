namespace GrpcService.Models
{
    public class CoinTransaction
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public int CoinId { get; set; }
        public int? SourceId { get; set; }
        public int DestinationId { get; set; }

        public Coin Coin { get; set; } = new();
        public User? Source { get; set; }
        public User Destination { get; set; } = null!;
    }
}
