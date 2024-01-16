using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TAP22_23.AuctionSite.Interface;

namespace Xia {
    [Index(nameof(Username),nameof(SiteId),IsUnique = true)]
    public class DBUser {
        [Key]
        public int UserId { get; set; }

        [MinLength(DomainConstraints.MinUserName)]
        [MaxLength(DomainConstraints.MaxUserName)]
        public string Username { get; set; }

        [MinLength(DomainConstraints.MinUserPassword)]
        public string Password { get; set; }

        [ForeignKey(nameof(SiteId))]
        public int SiteId { get; set; }
        public DBSite Site { get; set; }

        public int? SessionId { get; set; }
        public DBSession? Session { get; set; }
        public ICollection<DBAuction> Auctions { get; set; }
        public ICollection<DBBid> Bids { get; set; }
    }
}
