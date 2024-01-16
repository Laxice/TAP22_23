using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xia {
    public class DBBid {
        [Key]
        public int BidId { get; set; }
        public int UserId { get; set; }
        public DBUser User { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double Offer { get; set; }
        public DateTime BidTime { get; set; }
        public DBAuction Auction { get; set; }
        public int AuctionId { get; set; }

    }
}
