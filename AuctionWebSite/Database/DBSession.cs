using System.ComponentModel.DataAnnotations;

namespace Xia {
    public class DBSession {
        [Key]
        public int SessionId { get; set; }
        public DateTime ExpirationTime { get; set; }
        public int UserId { get; set; }
        public DBUser User { get; set; }
        public int SiteId { get; set; }
        public DBSite Site { get; set; }
    }
}
