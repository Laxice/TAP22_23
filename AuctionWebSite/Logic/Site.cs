using Microsoft.EntityFrameworkCore;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Xia {
    public class Site : ISite {
        public int SiteId { get;}
        public string Name { get; }
        public int Timezone { get; }
        public int SessionExpirationInSeconds { get; }
        public double MinimumBidIncrement { get; }
        public string ConnectionString { get; }

        private readonly IAlarmClock _alarmClock;
        private IAlarm _alarm;
        public Site(int siteId, string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement, string connectionString, IAlarmClock alarmClock)
        {
            SiteId = siteId;
            Name = name;
            Timezone = timezone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
            ConnectionString = connectionString;
            _alarmClock = alarmClock;
            _alarm = _alarmClock.InstantiateAlarm(300_000);
            _alarm.RingingEvent += RemoveExpiredSession;
        }

        public void CreateUser(string username, string password) {

            CheckUserParams(username, password);

            using var c = new DatabaseContext(ConnectionString);
            var user = new DBUser()
            {
                Username = username,
                Password = Auxiliary.HashPassword(password), //TODO:Hashed
                SiteId = SiteId
            };
            c.Users.Add(user);
            c.SaveChanges();
        }

        public void Delete() {
            using var c = new DatabaseContext(ConnectionString);
            var query = (from site in c.Sites where site.SiteId == SiteId select site).SingleOrDefault();
            if (query == null)
                throw new AuctionSiteInvalidOperationException("The site does not exist");
            c.Sites.Remove(query);
            c.SaveChanges();
        }

        public ISession? Login(string username, string password) {
            CheckUserParams(username, password);

            using var c = new DatabaseContext(ConnectionString);
            var querySite = from site in c.Sites where site.SiteId == SiteId select site;

            if (!querySite.Any())
                throw new AuctionSiteInvalidOperationException("Site doesn't exist anymore");

            var queryUser = ((from user in c.Users where user.Username == username && user.SiteId == SiteId select user).Include(u => u.Session)).SingleOrDefault();
            if (queryUser == null || !Auxiliary.VerifyHashPassword(queryUser.Password, password))
                return null;

            var userObj = new User(queryUser.Username, queryUser.Password, queryUser.SiteId, this);

            if (queryUser.Session != null)
            {
                if (queryUser.Session.ExpirationTime < Now())
                {
                    c.Sessions.Remove(queryUser.Session);
                    c.SaveChanges();
                }
                else
                {
                    queryUser.Session.ExpirationTime = Now().AddSeconds(SessionExpirationInSeconds);
                    c.SaveChanges();
                    return new Session(queryUser.Session.SessionId.ToString(),
                        queryUser.Session.ExpirationTime, userObj, queryUser.Session.SiteId, ConnectionString, this);
                }
            }

            var newSession = new DBSession
            {
                User = queryUser,
                SiteId = SiteId,
                UserId = queryUser.UserId,
                ExpirationTime = Now().AddSeconds(SessionExpirationInSeconds)
            };
            c.Sessions.Add(newSession);
            c.SaveChanges();

            return new Session(newSession.SessionId.ToString(), newSession.ExpirationTime, userObj, SiteId, ConnectionString, this);

        }

        public DateTime Now()
        {
            return _alarmClock.Now;
        }

        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded)
        {
            var aux = new List<IAuction>();


            using var c = new DatabaseContext(ConnectionString);
            var querySite = from site in c.Sites where site.SiteId == SiteId select site;

                if (!querySite.Any())
                    throw new AuctionSiteInvalidOperationException("Site doesn't exist anymore");

                IEnumerable<DBAuction> queryAuctions;
                if (onlyNotEnded)
                {
                    queryAuctions = (from auction in c.Auctions
                                    where auction.SiteId == SiteId && auction.EndsOn > Now()
                                    select auction).Include(a => a.Seller);
                }
                else
                {
                    queryAuctions = (from auction in c.Auctions
                                    where auction.SiteId == SiteId
                                    select auction).Include(a => a.Seller);
                }

                foreach (var auction in queryAuctions)
                {
                    var newUser = new User(auction.Seller.Username, auction.Seller.Password, SiteId, this);
                    var newAuction = new Auction(auction.AuctionId, newUser, auction.Description, auction.EndsOn, SiteId, ConnectionString, this);

                    aux.Add(newAuction);
                }

                return aux;
        }

        public IEnumerable<ISession> ToyGetSessions() {
            var aux = new List<ISession>();

            using var c = new DatabaseContext(ConnectionString);
            var querySite = from site in c.Sites where site.SiteId == SiteId select site;

                if (!querySite.Any())
                    throw new AuctionSiteInvalidOperationException("Site doesn't exist anymore");

                var querySession = (from session in c.Sessions join user in c.Users on session.UserId equals user.UserId where session.SiteId == SiteId select new {Session = session, User = user}).ToList();

                foreach (var query in querySession)
                {
                    var newUser = new User(query.User.Username, query.User.Password, SiteId, this);
                    var newSession = new Session(query.Session.SessionId.ToString(),query.Session.ExpirationTime, newUser, SiteId, ConnectionString,this);
                    aux.Add(newSession);
                }

            return aux;
        }

        public IEnumerable<IUser> ToyGetUsers()
        {
            var aux = new List<IUser>();

            using var c = new DatabaseContext(ConnectionString);
            var querySite = (from site in c.Sites where site.SiteId == SiteId select site).SingleOrDefault();

                if (querySite == null)
                    throw new AuctionSiteInvalidOperationException("Website not found");

                var queryUsers = from user in c.Users where user.SiteId == SiteId select user;

                foreach (var q in queryUsers)
                {
                    var user = new User(q.Username, q.Username, SiteId, this);
                    aux.Add(user);
                }
                return aux;
        }

        public void CheckUserParams(string username, string password) {
            if (username == null || password == null)
                throw new AuctionSiteArgumentNullException("Username or Password is null");
            if (username.Length < DomainConstraints.MinUserName || username.Length > DomainConstraints.MaxUserName)
                throw new AuctionSiteArgumentException($"{username} is too short or too long");
            if (password.Length < DomainConstraints.MinUserPassword)
                throw new AuctionSiteArgumentException("Password is too short");
        }

        private void RemoveExpiredSession()
        {
            using var c = new DatabaseContext(ConnectionString);
            var querySession = from session in c.Sessions
                    where session.SiteId == SiteId && session.ExpirationTime < Now()
                    select session;

                foreach (var q in querySession)
                {
                    c.Sessions.Remove(q);
                }

                c.SaveChanges();
            _alarm = _alarmClock.InstantiateAlarm(300_000);


        }
        public override bool Equals(object? o)
        {
            if (o == null || o.GetType() != GetType())
                return false;
            var obj = o as Site;
            return obj!.Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
