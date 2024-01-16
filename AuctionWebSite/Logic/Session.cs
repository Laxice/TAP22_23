using TAP22_23.AuctionSite.Interface;

namespace Xia {
    public class Session : ISession {
        public Session(string id, DateTime validUntil, IUser user, int siteId, string connectionString, Site site)
        {
            Id = id;
            ValidUntil = validUntil;
            User = user;
            SiteId = siteId;
            ConnectionString = connectionString;
            Site = site;
        }

        public string Id { get; }

        public DateTime ValidUntil { get; private set; }
        public IUser User { get; }
        public int SiteId { get; }
        public string ConnectionString { get; }
        public Site Site { get; }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice)
        {
            if (description == null)
                throw new AuctionSiteArgumentNullException("Description is null");
            if (description == "")
                throw new AuctionSiteArgumentException("Description is empty");
            if (startingPrice < 0)
                throw new AuctionSiteArgumentOutOfRangeException("Starting Price is negative");
            if (endsOn < Site.Now())
                throw new AuctionSiteUnavailableTimeMachineException("Auction has ended");

            using var c = new DatabaseContext(ConnectionString);
            var querySite = (from site in c.Sites where site.SiteId == SiteId select site).SingleOrDefault();
            if (querySite == null)
                throw new AuctionSiteArgumentException("Site doesn't exist");

            var queryUser = (from user in c.Users where user.Username == User.Username && user.SiteId == SiteId select user).SingleOrDefault();
            if (queryUser == null)
                throw new AuctionSiteArgumentException("User doesn't exist");

            var querySession = (from session in c.Sessions where session.UserId == queryUser.UserId select session).SingleOrDefault();

            if (querySession == null || querySession.ExpirationTime < Site.Now())
                throw new AuctionSiteInvalidOperationException("The session has expired");

            var auction = new DBAuction
            {
                Description = description,
                EndsOn = endsOn,
                CurrentPrice = startingPrice,
                StartigPrice = startingPrice,
                Site = querySite,
                SiteId = querySite.SiteId,
                Seller = queryUser,
                SellerId = queryUser.UserId
            };
            c.Auctions.Add(auction);
            c.SaveChanges();
            ResetExpiration();

            return new Auction(auction.AuctionId, User, description, endsOn, SiteId, ConnectionString, Site);
        }

        public void Logout() {
            using var c = new DatabaseContext(ConnectionString);
            var querySession = (from session in c.Sessions where session.SessionId.ToString() == Id select session).SingleOrDefault();

            if (querySession == null)
                throw new AuctionSiteInvalidOperationException("The session does not exist");
            c.Sessions.Remove(querySession);
            c.SaveChanges();
        }

        internal void ResetExpiration()
        {
            using var c = new DatabaseContext(ConnectionString);
            var querySession = (from session in c.Sessions where session.SessionId.ToString() == Id select session).SingleOrDefault();
            if (querySession == null)
                throw new AuctionSiteArgumentException("Session doesn't exist");

            querySession.ExpirationTime = Site.Now().AddSeconds(Site.SessionExpirationInSeconds);
            c.SaveChanges();
            ValidUntil = querySession.ExpirationTime;
        }
        public override bool Equals(object? o)
        {
            if (o == null || o.GetType() != GetType())
                return false;
            var obj = o as Session;
            return obj!.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
