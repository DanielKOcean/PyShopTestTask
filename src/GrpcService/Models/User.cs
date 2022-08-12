namespace GrpcService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rating { get; set; }

        public virtual ICollection<Coin> Coins { get; set; } = new List<Coin>();
    }
}
