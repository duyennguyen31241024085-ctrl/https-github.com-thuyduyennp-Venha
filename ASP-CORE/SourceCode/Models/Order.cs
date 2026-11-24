using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSSV_HoVaTen.Models;

[Microsoft.EntityFrameworkCore.Index(nameof(CheckoutToken), IsUnique = true)]
public class HoaDon
{
    public int ID { get; set; }
    public DateTime NgayLap { get; set; }
    public Guid CheckoutToken { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    public List<ChiTietHoaDon> ChiTietHoaDons { get; set; } = [];
    public ShippingDetail ShippingDetail { get; set; }
}

public class ChiTietHoaDon
{
    public int ID { get; set; }
    public int HoaDonID { get; set; }

    [ForeignKey(nameof(MovieObj))]
    public int MaMovie { get; set; }
    public Movie MovieObj { get; set; }
    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }
}

public class ShippingDetail
{
    public int ID { get; set; }
    public int HoaDonID { get; set; }

    [Required, StringLength(100), Display(Name = "Họ và tên")]
    public string Name { get; set; }

    [Required, EmailAddress, StringLength(254), Display(Name = "Email")]
    public string Email { get; set; }

    [Required, RegularExpression(@"\+?[0-9]{8,15}", ErrorMessage = "Nhập từ 8 đến 15 chữ số."), Display(Name = "Điện thoại")]
    public string Mobile { get; set; }

    [Required, StringLength(250), Display(Name = "Địa chỉ giao hàng")]
    public string Address { get; set; }
}

public record CartLine(Movie Movie, int Quantity)
{
    public decimal Total => Movie.Price * Quantity;
}
