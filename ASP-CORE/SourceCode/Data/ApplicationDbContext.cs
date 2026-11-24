using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Models;

namespace MSSV_HoVaTen.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<HoaDon> HoaDons => Set<HoaDon>();
    public DbSet<ChiTietHoaDon> ChiTietHoaDons => Set<ChiTietHoaDon>();
    public DbSet<ShippingDetail> ShippingDetails => Set<ShippingDetail>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Movie>().HasOne(movie => movie.GenreObj).WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ChiTietHoaDon>().HasOne(line => line.MovieObj).WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<HoaDon>().HasOne(order => order.ShippingDetail).WithOne()
            .HasForeignKey<ShippingDetail>(detail => detail.HoaDonID);
        builder.Entity<Genre>().HasIndex(genre => genre.Name).IsUnique();
    }
}
