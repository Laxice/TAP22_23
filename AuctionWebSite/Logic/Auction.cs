using Microsoft.IdentityModel.Tokens;
using TAP22_23.AuctionSite.Interface;

namespace Xia {
    public class Auction : IAuction {
        public Auction(int id, IUser seller, string description, DateTime endsOn, int siteId, string connectionString, Site site)
        {
            Id = id;
            Seller = seller;
            Description = description;
            EndsOn = endsOn;
            SiteId = siteId;
            ConnectionString = connectionString;
            Site = site;
        }
        public int Id { get; }
        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }
        public int SiteId { get; }
        public string ConnectionString { get; }
        public Site Site { get; }
        public bool Bid(ISession session, double offer) {
            CheckBidParams(session, offer);

            using var c = new DatabaseContext(ConnectionString);
            var queryAuction = (from auction in c.Auctions
                                where auction.AuctionId == Id && auction.SiteId == SiteId
                                select auction).SingleOrDefault();

            if (queryAuction == null)
                throw new AuctionSiteInvalidOperationException("Auction not valid");

            var queryUsers = (from user in c.Users
                              where user.Username == session.User.Username && queryAuction.SiteId == SiteId
                              select user).SingleOrDefault();
            var querySession = (from sessionUser in c.Sessions where session.User.Username == sessionUser.User.Username select sessionUser).SingleOrDefault();
            var queryBids = from bids in c.Bids where bids.AuctionId == Id select bids;

            if (querySession == null)
                throw new AuctionSiteArgumentException("Session not valid");
            if (queryUsers == null)
                throw new AuctionSiteArgumentException("User doesn't exist");

            (session as Session)!.ResetExpiration();

            var newBid = new DBBid
            {
                Offer = offer,
                BidTime = Site.Now(),
                AuctionId = Id,
                Auction = queryAuction,
                User = queryUsers,
                UserId = queryUsers.UserId
            };

            var currentWinner = (this.CurrentWinner() != null && queryUsers.Username.Equals(this.CurrentWinner()!.Username));

            if (currentWinner && offer < queryAuction.MaxOffer + Site.MinimumBidIncrement)
                return false;
            if (!currentWinner && offer < this.CurrentPrice())
                return false;
            if (!currentWinner && offer < this.CurrentPrice() + Site.MinimumBidIncrement &&
                queryBids.Any())
                return false;

            if (!queryBids.Any())
            {
                queryAuction.MaxOffer = offer;
                queryAuction.CurrentWinner = queryUsers;
            }

            else if (currentWinner)
                queryAuction.MaxOffer = offer;

            else if (queryBids.Any() && !currentWinner &&
                offer > queryAuction.MaxOffer)
            {
                queryAuction.CurrentPrice = Math.Min(offer, queryAuction.MaxOffer + Site.MinimumBidIncrement);
                queryAuction.MaxOffer = offer;
                queryAuction.CurrentWinner = queryUsers;
            }
            else if (queryBids.Any() && !currentWinner &&
                offer <= queryAuction.MaxOffer)
                queryAuction.CurrentPrice = Math.Min(queryAuction.MaxOffer, offer + Site.MinimumBidIncrement);

            c.Bids.Add(newBid);
            c.SaveChanges();
            return true;
        }

        public double CurrentPrice() {
            using var c = new DatabaseContext(ConnectionString);
            var queryAuctions = (from auction in c.Auctions where Id == auction.AuctionId select auction).SingleOrDefault();
            if (queryAuctions == null)
                throw new AuctionSiteInvalidOperationException("Auction doesn't exist");
            return queryAuctions.CurrentPrice;
        }

        public IUser? CurrentWinner() {
            using var c = new DatabaseContext(ConnectionString);
            var currentWinner = (from auction in c.Auctions where auction.AuctionId == Id select auction.CurrentWinner).SingleOrDefault();
            if (currentWinner == null)
                return null;
            return new User(currentWinner.Username, currentWinner.Password, currentWinner.SiteId, Site);
        }

        public void Delete() {
            using var c = new DatabaseContext(ConnectionString);
            var queryAuction = (from auction in c.Auctions where auction.AuctionId == Id select auction).SingleOrDefault();

            if (queryAuction == null)
                throw new AuctionSiteInvalidOperationException("Auction is not valid");

            c.Auctions.Remove(queryAuction);
            c.SaveChanges();
        }

        public void CheckBidParams(ISession session, double offer)
        {
            if (session == null)
                throw new AuctionSiteArgumentNullException("Session is null");
            if (offer < 0)
                throw new AuctionSiteArgumentOutOfRangeException("Offer is negative");
            if (this.EndsOn < Site.Now())
                throw new AuctionSiteInvalidOperationException("Auction has expired");
            if (session.ValidUntil < Site.Now())
                throw new AuctionSiteArgumentException("Session has expired");
            if (session.User.Equals(Seller))
                throw new AuctionSiteArgumentException("You are the owner of the auction");
        }

        public override bool Equals(object? o)
        {
            if (o == null || o.GetType() != GetType())
                return false;
            var obj = o as Auction;
            return obj!.Id == Id && obj.SiteId == SiteId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id,Site.GetHashCode());
        }
    }
}
