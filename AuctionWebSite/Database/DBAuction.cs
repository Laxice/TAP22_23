using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xia {
    public class DBAuction {
        public String Description { get; set; }
        public DateTime EndsOn { get; set; }
        [Key]
        public int AuctionId { get; set; }
        public  int SellerId { get; set; }
        public DBUser Seller { get; set; } //Seller
        public double StartigPrice { get; set; }
        public ICollection<DBBid>? Bids { get; set; }
        public DBSite Site { get; set; }
        public int SiteId { get; set; }
        public double MaxOffer { get; set; }
        public double CurrentPrice { get; set; }
        public int? CurrentWinnerID { get; set; }
        public DBUser? CurrentWinner { get; set; }

    }
}
