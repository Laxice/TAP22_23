using System.Data.Common;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Xia {
    public class Host : IHost {

        public string ConnectionString { get; }
        public IAlarmClockFactory AlarmClockFactory { get; }

        public Host(string connectionString, IAlarmClockFactory alarmClockFactory) {
            ConnectionString = connectionString;
            AlarmClockFactory = alarmClockFactory;
        }

        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            CheckSiteParams(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement);

            using var c = new DatabaseContext(ConnectionString);
            var site = new DBSite() { SiteName = name, Timezone = timezone, SessionExpirationInSeconds = sessionExpirationTimeInSeconds, MinimumBidIncrement = minimumBidIncrement };

            c.Sites.Add(site);
            c.SaveChanges();
        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos() {
            var aux = new List<(string Name, int TimeZone)>();

            var c = new DatabaseContext(ConnectionString);
            if (!c.Database.CanConnect())
                        throw new AuctionSiteUnavailableDbException("DB Exception");
            var sites = from site in c.Sites select new { Nome = site.SiteName, TimeZone = site.Timezone };

            foreach (var site in sites)
                aux.Add((site.Nome, site.TimeZone));

            return aux;
        }

        public ISite LoadSite(string name)
        {
            if (name == null)
                throw new AuctionSiteArgumentNullException("Name is null");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException($"{name} is too short or too long");

            using var c = new DatabaseContext(ConnectionString);
            if (!c.Database.CanConnect())
                throw new AuctionSiteUnavailableDbException(
                    "DB Exception");

            var querySite = (from site in c.Sites where site.SiteName == name select site).SingleOrDefault();

            if (querySite == null)
                throw new AuctionSiteInexistentNameException($"Name {name} does not exist in the DB.");

            var id = querySite.SiteId;
            var timezone = querySite.Timezone;
            var sessionExpirationTimeInSeconds = querySite.SessionExpirationInSeconds;
            var minimumBidIncrement = querySite.MinimumBidIncrement;

            var alarmClock = AlarmClockFactory.InstantiateAlarmClock(querySite.Timezone);

            return new Site(id, name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement, ConnectionString, alarmClock);

        }

        public void CheckSiteParams(string name, int timezone, int sessionExpTimeInSeconds, double minBidIncrement)
        {
            if (name == null)
                throw new AuctionSiteArgumentNullException("Name is null");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException($"{name} is too short or too long");
            if (timezone < DomainConstraints.MinTimeZone || timezone > DomainConstraints.MaxTimeZone)
                throw new AuctionSiteArgumentOutOfRangeException($"Timezone value {timezone} is out of range");
            if (sessionExpTimeInSeconds <= 0)
                throw new AuctionSiteArgumentOutOfRangeException($"Session expiration time {sessionExpTimeInSeconds} must be positive");
            if (minBidIncrement <= 0)
                throw new AuctionSiteArgumentOutOfRangeException($"Minimum bid increment {minBidIncrement} must be positive");
        }
    }
}
