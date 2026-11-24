using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSSV_HoVaTen.Models;

public class Movie
{
    public int ID { get; set; }

    [Required, StringLength(200, MinimumLength = 3), Display(Name = "Tiêu đề")]
    public string Title { get; set; }

    [StringLength(2000), Display(Name = "Tóm tắt")]
    public string Summary { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày phát hành")]
    public DateTime ReleaseDate { get; set; } = DateTime.Today.AddDays(1);

    [Range(1, int.MaxValue), Display(Name = "Thể loại")]
    public int GenreID { get; set; }
    [ForeignKey(nameof(GenreID))]
    public Genre GenreObj { get; set; }

    [Range(typeof(decimal), "5000", "999999999"), Column(TypeName = "decimal(18,2)"), Display(Name = "Giá (₫)")]
    public decimal Price { get; set; }

    [Range(1, 5), Display(Name = "Xếp hạng")]
    public double Rated { get; set; }

    public string PicturePath { get; set; }

    [NotMapped, Display(Name = "Ảnh bìa")]
    public IFormFile PictureUpload { get; set; }
}

public class Genre
{
    public int ID { get; set; }

    [Required, StringLength(200), Display(Name = "Tên thể loại")]
    public string Name { get; set; }
}
