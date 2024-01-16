using Microsoft.Data.SqlClient;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Xia
{
    public class HostFactory : IHostFactory
    {
        public void CreateHost(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new AuctionSiteArgumentNullException("connectionString is null");
            }
            try {
                using (var c = new DatabaseContext(connectionString)) {
                    c.Database.EnsureDeleted(); 
                    c.Database.EnsureCreated();
                }
            }catch (SqlException ex) {
                    throw new AuctionSiteUnavailableDbException("connectionString is malformed", ex);
            }
        }


        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory)
        {
            if (String.IsNullOrEmpty(connectionString))
                throw new AuctionSiteArgumentNullException("connectionString is null or empty");
            if(alarmClockFactory == null)
                throw new AuctionSiteArgumentNullException("alarmClockFactory is null");
            try
            {
                using (var c = new DatabaseContext(connectionString))
                {
                    if (!c.Database.CanConnect())
                        throw new AuctionSiteUnavailableDbException(
                            "connectionString is malformed or the DB doesn't exist");
                    return new Host(connectionString, alarmClockFactory);
                }
            }
            catch (SqlException ex)
            {
                throw new AuctionSiteUnavailableDbException("Error with the DB ",ex);
            }
        }
    }
}