using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace GrpcService.Services
{
    public class BillingService : Billing.BillingBase
    {
        private readonly Models.ApiContext _context;
        private readonly ILogger<BillingService> _logger;
        public BillingService(Models.ApiContext context, ILogger<BillingService> logger)
        {
            _context = context;
            _context.Database.EnsureCreated(); // Comment this line if use SQLite.

            _logger = logger;
        }

        public override async Task ListUsers(None none, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (UserProfile userProfile in _context.Users
                    .Select(x => new UserProfile { Name = x.Name, Amount = x.Coins.Count }))
                {
                    await responseStream.WriteAsync(userProfile);
                }
            }
            catch (Exception)
            {
                _logger.LogError($"Something went wrong during database exchange.");
            }
        }

        private class ExtUser
        {
            public Models.User User { get; set; } = null!;

            public int OverallCoins { get; set; }

            public double Reminder { get; set; }
        }

        private async Task DistributeCoins(List<Models.User> users, int coins)
        {
            var coinsToDistribute = coins - users.Count; // We must give every user a coin first, so they are sahred by default.

            int overallRating = users.Sum(x => x.Rating);

            double hareQuota = overallRating / coinsToDistribute;

            var extUsers = users.Select(x => new ExtUser
            {
                User = x,
                OverallCoins = (int)(x.Rating / hareQuota) + 1, // +1 is a coin that we shared by default.
                Reminder = (x.Rating / hareQuota) % 1,
            })
            .OrderByDescending(x => x.Reminder)
            .ToList();

            // Rest of coins must be spread between users with highest reminders.
            for (int i = 0; i < coins - extUsers.Sum(x => x.OverallCoins); i++)
            {
                extUsers[i].OverallCoins++;
            }

            // Database logic that creates coin and initial transaction and assingns the coin to the destinatio user.
            foreach (var user in extUsers)
            {
                for (int i = 0; i < user.OverallCoins; i++)
                {
                    var coin = new Models.Coin
                    {
                        Created = DateTime.UtcNow,
                    };

                    coin.Transactions = new List<Models.CoinTransaction>
                    {
                        new Models.CoinTransaction { Created = coin.Created, Coin = coin, Destination = user.User },
                    };

                    user.User.Coins.Add(coin);
                }
            }

            await _context.SaveChangesAsync();
        }

        public override async Task<Response> CoinsEmission(EmissionAmount amount, ServerCallContext context)
        {
            string msg;

            try
            {
                List<Models.User> users = _context.Users.ToList();

                if (amount == null || amount.Amount < users.Count)
                {
                    msg = $"Coin amount must be more then users ({users.Count}).";
                    _logger.LogInformation(msg);

                    return new Response
                    {
                        Status = Response.Types.Status.Failed,
                        Comment = msg,
                    };
                }

                await DistributeCoins(users, (int)amount.Amount);

                msg = $"{amount.Amount} {(amount.Amount == 1 ? "coin" : "coins")} distributed successfully.";
                _logger.LogInformation(msg);

                return new Response
                {
                    Status = Response.Types.Status.Ok,
                    Comment = msg,
                };
            }
            catch (Exception)
            {
                msg = "Something went wrong during database exchange.";
                _logger.LogError(msg);

                return new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = msg,
                };
            }
        }

        public override async Task<Response> MoveCoins(MoveCoinsTransaction trans, ServerCallContext context)
        {
            string msg;

            try
            {
                Models.User? srcUser = _context.Users
                .Include(user => user.Coins)
                .FirstOrDefault(x => x.Name == trans.SrcUser);

                if (srcUser == null)
                {
                    msg = $"User '{trans.SrcUser}' couldn't be found.";
                    _logger.LogInformation(msg);

                    return new Response
                    {
                        Status = Response.Types.Status.Failed,
                        Comment = msg,
                    };
                }

                Models.User? dstUser = _context.Users
                    .Include(user => user.Coins)
                    .FirstOrDefault(x => x.Name == trans.DstUser);

                if (dstUser == null)
                {
                    msg = $"User '{trans.DstUser}' couldn't be found.";
                    _logger.LogInformation(msg);

                    return new Response
                    {
                        Status = Response.Types.Status.Failed,
                        Comment = msg,
                    };
                }

                if (srcUser.Coins.Count < trans.Amount)
                {
                    msg = $"User '{trans.SrcUser}' doesn't have enough coins to send.";
                    _logger.LogInformation(msg);

                    return new Response
                    {
                        Status = Response.Types.Status.Failed,
                        Comment = msg,
                    };
                }

                // Simply take first N (amount) coin records assigned to the source user.
                List<Models.Coin> coins = srcUser.Coins.Take((int)trans.Amount).ToList();

                // Re-assign each of taken coins to destination user and add transaction record.
                foreach (Models.Coin coin in coins)
                {
                    coin.User = dstUser;

                    coin.Transactions.Add(new Models.CoinTransaction
                    {
                        Created = DateTime.UtcNow,
                        Source = srcUser,
                        Destination = dstUser,
                        Coin = coin,
                    });
                }

                await _context.SaveChangesAsync();

                msg = $"{trans.SrcUser} sent {trans.Amount} {(trans.Amount == 1 ? "coin" : "coins")} to {trans.DstUser}.";
                _logger.LogInformation(msg);

                return new Response
                {                    
                    Status = Response.Types.Status.Ok,
                    Comment = msg,
                };
            }
            catch (Exception)
            {
                msg = "Something went wrong during database exchange.";
                _logger.LogError(msg);

                return new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = msg,
                };
            }
            
        }

        public override async Task<Coin> LongestHistoryCoin(None none, ServerCallContext context)
        {
            string msg;

            try
            {
                int id = await _context.Transactions
                .GroupBy(e => e.CoinId)
                .Select(g => new { CoinId = g.Key, TransCount = g.Count() })
                .OrderByDescending(g => g.TransCount)
                .Select(g => g.CoinId)
                .FirstOrDefaultAsync();

                if (id == 0)
                {
                    msg = $"No coins have been emitted.";
                    _logger.LogInformation(msg);
                }

                string history = string.Join(';', _context.Transactions
                    .Include(e => e.Destination)
                    .Where(e => e.CoinId == id)
                    .Select(e => e.Destination.Name));

                msg = $"Coin with longest history ({history}) has id={id}.";
                _logger.LogInformation(msg);

                return new Coin { Id = id, History = history };
            }
            catch (Exception)
            {
                msg = "Something went wrong during database exchange.";
                _logger.LogError(msg);
            }

            return new Coin();
        }
    }
}
