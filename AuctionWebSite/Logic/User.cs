using System.ComponentModel.DataAnnotations;
using TAP22_23.AuctionSite.Interface;

namespace Xia {
    public class User : IUser {
        public User(string username, string password, int siteId, Site site)
        {
            Username = username;
            Password = password;
            SiteId = siteId;
            Site = site;
            ConnectionString = site.ConnectionString;
        }

        public string Username { get; }
        public string Password { get; }
        public int SiteId { get; }
        public Site Site { get; }
        public string ConnectionString { get; }
        public void Delete() {
            using var c = new DatabaseContext(ConnectionString);
            var queryUser = (from user in c.Users
                             where user.Username == Username && user.SiteId == SiteId
                             select user).SingleOrDefault();

            if (queryUser == null)
                throw new AuctionSiteInvalidOperationException("User doesn't exist");

            var queryAuction = (from auction in c.Auctions
                                where auction.Seller.UserId == queryUser.UserId && auction.EndsOn > Site.Now()
                                select auction);

            if (queryAuction.Any())
                throw new AuctionSiteInvalidOperationException("User owns an auction, cannot be deleted");

            var queryAuctionWinner = (from auction in c.Auctions
                                      where auction.CurrentWinnerID == queryUser.UserId && auction.EndsOn > Site.Now()
                                      select auction);

            if (queryAuctionWinner.Any())
                throw new AuctionSiteInvalidOperationException("User is winning an auction, cannot be deleted");

            var queryEndedAuctionWinner = (from auction in c.Auctions
                                           where auction.CurrentWinnerID == queryUser.UserId && auction.EndsOn <= Site.Now()
                                           select auction);

            foreach (var q in queryEndedAuctionWinner)
            {
                q.CurrentWinnerID = null;
                q.CurrentWinner = null;
            }

            c.Users.Remove(queryUser);
            c.SaveChanges();
        }

        public IEnumerable<IAuction> WonAuctions()
        {
            var aux = new List<IAuction>();

            using var c = new DatabaseContext(ConnectionString);

            var queryWonAuctions = from auction in c.Auctions
                                   where auction.CurrentWinner != null && auction.CurrentWinner.Username == Username && auction.SiteId == SiteId
                                   select new
                                   {
                                       Auction = auction,
                                       Seller = auction.Seller
                                   };
            foreach (var q in queryWonAuctions)
            {
                var seller = new User(Username, Password, SiteId, Site);
                var auction = new Auction(q.Auction.AuctionId, seller, q.Auction.Description, q.Auction.EndsOn, q.Auction.SiteId, ConnectionString,
                    Site);
                aux.Add(auction);
            }

            return aux;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;
            var o = obj as User;
            return o!.SiteId == SiteId && o.Username.Equals(Username);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Username, Site.GetHashCode());
        }
    }
}
