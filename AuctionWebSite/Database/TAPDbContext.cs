using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TAP22_23.AuctionSite.Interface;

namespace Xia;
public class DatabaseContext : TapDbContext {
    public DbSet<DBAuction> Auctions { get; set; }
    public DbSet<DBBid> Bids { get; set; }
    public DbSet<DBSession> Sessions { get; set; }
    public DbSet<DBSite> Sites { get; set; }
    public DbSet<DBUser> Users { get; set; }

    private readonly string _connectionString;

    public DatabaseContext(string connectionString) : base(new DbContextOptionsBuilder<DatabaseContext>().Options) {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(_connectionString);

        options.LogTo(Console.WriteLine).EnableSensitiveDataLogging();

        base.OnConfiguring(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<DBUser>();
        var auction = modelBuilder.Entity<DBAuction>();
        var session = modelBuilder.Entity<DBSession>();
        var bid = modelBuilder.Entity<DBBid>();

        user.HasOne(user => user.Site)
            .WithMany(site => site.Users)
            .HasForeignKey(user => user.SiteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        user.HasOne(user => user.Session)
            .WithOne(sessions => sessions.User)
            .HasForeignKey<DBUser>(user => user.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        auction.HasOne(auction => auction.Seller)
            .WithMany(user => user.Auctions)
            .HasForeignKey(auction => auction.SellerId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        auction.HasOne(auction => auction.Site)
            .WithMany(site => site.Auctions)
            .HasForeignKey(auction => auction.SiteId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();

        auction.HasMany(auction => auction.Bids)
            .WithOne(bids => bids.Auction)
            .HasForeignKey(bids => bids.AuctionId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        bid.HasOne(bid => bid.User)
            .WithMany(user => user.Bids)
            .HasForeignKey(bid => bid.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        bid.HasOne(bid => bid.Auction)
            .WithMany(auction => auction.Bids)
            .HasForeignKey(bid => bid.AuctionId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();

        session.HasOne(session => session.Site)
            .WithMany(site => site.Sessions)
            .HasForeignKey(session => session.SiteId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        try
        {
            return base.SaveChanges();
        }
        catch (SqlException e)
        {
            throw new AuctionSiteUnavailableDbException("Cannot connect with the database ", e);
        }
        catch (DbUpdateConcurrencyException e)
        {
            throw new AuctionSiteConcurrentChangeException("Concurrent change exception ", e);
        }
        catch (DbUpdateException e)
        {
            var exc = e.InnerException as SqlException;
            if (exc == null)
                throw new AuctionSiteInvalidOperationException("Inner exception value was null", e);
            switch (exc.Number)
            {
                case 2601:
                    throw new AuctionSiteNameAlreadyInUseException(null, "Name already in use ", exc);
                default:
                    throw new AuctionSiteInvalidOperationException("Default SQL error " + exc.Number + " occured ", e);
            }
        }
    }
}