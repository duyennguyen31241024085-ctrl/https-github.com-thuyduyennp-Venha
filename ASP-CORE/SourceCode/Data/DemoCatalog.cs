using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Models;

namespace MSSV_HoVaTen.Data;

public static class DemoCatalog
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Genres.AnyAsync()) return;
        var sci = new Genre { Name = "Viễn tưởng" };
        var drama = new Genre { Name = "Tâm lý" };
        var adventure = new Genre { Name = "Phiêu lưu" };
        db.Movies.AddRange(
            Movie("Tín hiệu tháng Mười", "Một kỹ sư vô tuyến nhận được lời nhắn từ trạm nghiên cứu đã im lặng suốt hai mươi năm.", sci, 79000, 4.8, "signal"),
            Movie("Mùa nước lên", "Ba chị em trở về căn nhà ven sông và tìm thấy một cuốn sổ chưa viết xong.", drama, 59000, 4.6, "river"),
            Movie("Qua dãy Lam Sơn", "Hai người bạn lên đường tìm tuyến đường núi được đánh dấu trên một tấm bản đồ cũ.", adventure, 69000, 4.7, "ridge"),
            Movie("Quỹ đạo xanh", "Một phi hành đoàn thay đổi hành trình khi phát hiện sự sống quanh một vệ tinh xa.", sci, 89000, 4.9, "orbit"),
            Movie("Bưu điện cuối phố", "Một nhân viên bưu điện tìm lại người gửi của những bức thư không địa chỉ.", drama, 49000, 4.5, "post"));
        await db.SaveChangesAsync();
    }

    private static Movie Movie(string title, string summary, Genre genre, decimal price, double rated, string poster) =>
        new() { Title = title, Summary = summary, GenreObj = genre, Price = price, Rated = rated,
            ReleaseDate = new DateTime(2028, 10, 20), PicturePath = $"/images/{poster}.jpg" };
}
