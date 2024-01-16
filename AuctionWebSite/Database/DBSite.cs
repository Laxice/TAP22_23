using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TAP22_23.AuctionSite.Interface;

namespace Xia {

    [Index(nameof(SiteName), IsUnique = true)]
    public class DBSite {

        [Key]
        public int SiteId { get; set; }

        [MinLength(DomainConstraints.MinSiteName)]
        [MaxLength(DomainConstraints.MaxSiteName)]
        public string SiteName { get; set; }

        [Range(0,int.MaxValue)]
        public int SessionExpirationInSeconds { get; set; }
        [Range(double.Epsilon, double.MaxValue)]
        public double MinimumBidIncrement { get; set; }
        [Range(DomainConstraints.MinTimeZone,DomainConstraints.MaxTimeZone)]
        public int Timezone { get; set; }
        public ICollection<DBAuction>? Auctions { get; set; }
        public ICollection<DBUser>? Users { get; set; }
        public ICollection<DBSession>? Sessions { get; set; }
    }
}
