using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Data;
using MSSV_HoVaTen.Extension;
using MSSV_HoVaTen.Models;

namespace MSSV_HoVaTen.Controllers;

public class MoviesController(ApplicationDbContext db, IWebHostEnvironment env,
    IConfiguration configuration, ILogger<MoviesController> logger, ShoppingCart shoppingCart) : Controller
{
    private const string CheckoutKey = "CheckoutOrder";
    private const int MaxImageBytes = 5 * 1024 * 1024;

    public async Task<IActionResult> Index(int? genre, string q)
    {
        var movies = db.Movies.Include(movie => movie.GenreObj).AsNoTracking().AsQueryable();
        if (genre.HasValue) movies = movies.Where(movie => movie.GenreID == genre);
        if (!string.IsNullOrWhiteSpace(q)) movies = movies.Where(movie => movie.Title.Contains(q.Trim()));
        ViewBag.Genres = new SelectList(await db.Genres.OrderBy(item => item.Name).ToListAsync(), "ID", "Name", genre);
        ViewBag.Query = q;
        return View(await movies.OrderBy(movie => movie.ID).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var movie = await db.Movies.Include(item => item.GenreObj).FirstOrDefaultAsync(item => item.ID == id);
        return movie is null ? NotFound() : View(movie);
    }

    [Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Create() => await MovieForm("Create", new Movie { Rated = 4 });

    [HttpPost, Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Create([Bind("Title,Summary,ReleaseDate,GenreID,Price,Rated,PictureUpload")] Movie movie)
    {
        await ValidateMovie(movie);
        if (!ModelState.IsValid) return await MovieForm("Create", movie);
        movie.PicturePath = await SavePicture(movie.PictureUpload);
        db.Movies.Add(movie);
        await SaveMovie(movie.PicturePath);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Edit(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        return movie is null ? NotFound() : await MovieForm("Edit", movie);
    }

    [HttpPost, Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Edit(int id, [Bind("Title,Summary,ReleaseDate,GenreID,Price,Rated,PictureUpload")] Movie input)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        input.ID = id;
        input.PicturePath = movie.PicturePath;
        await ValidateMovie(input, movie.ReleaseDate);
        if (!ModelState.IsValid) return await MovieForm("Edit", input);

        movie.Title = input.Title;
        movie.Summary = input.Summary;
        movie.ReleaseDate = input.ReleaseDate;
        movie.GenreID = input.GenreID;
        movie.Price = input.Price;
        movie.Rated = input.Rated;
        var oldPath = movie.PicturePath;
        var newPath = await SavePicture(input.PictureUpload);
        if (newPath is not null) movie.PicturePath = newPath;
        await SaveMovie(newPath);
        if (newPath is not null) RemovePicture(oldPath);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        return movie is null ? NotFound() : View(movie);
    }

    [HttpPost, ActionName("Delete"), Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        db.Movies.Remove(movie);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể xóa phim đang có trong đơn hàng.");
            return View("Delete", movie);
        }
        RemovePicture(movie.PicturePath);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Cart() => View(await shoppingCart.GetLinesAsync(HttpContext.Session));

    [HttpPost]
    public async Task<IActionResult> AddToCart(int id)
    {
        if (!await db.Movies.AnyAsync(movie => movie.ID == id)) return NotFound();
        var cart = shoppingCart.Read(HttpContext.Session);
        cart[id] = Math.Min(99, cart.GetValueOrDefault(id) + 1);
        shoppingCart.Store(HttpContext.Session, cart);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    public IActionResult UpdateCart(int id, int quantity)
    {
        var cart = shoppingCart.Read(HttpContext.Session);
        if (ModelState.IsValid && cart.ContainsKey(id) && quantity is >= 1 and <= 99)
            cart[id] = quantity;
        shoppingCart.Store(HttpContext.Session, cart);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    public IActionResult RemoveFromCart(int id)
    {
        var cart = shoppingCart.Read(HttpContext.Session);
        cart.Remove(id);
        shoppingCart.Store(HttpContext.Session, cart);
        return RedirectToAction(nameof(Cart));
    }

    public async Task<IActionResult> Checkout()
    {
        var lines = await shoppingCart.GetLinesAsync(HttpContext.Session);
        if (lines.Count == 0) return RedirectToAction(nameof(Cart));
        ViewBag.Total = lines.Sum(line => line.Total);
        var token = HttpContext.Session.GetString(ShoppingCart.CheckoutTokenKey) ?? Guid.NewGuid().ToString("D");
        HttpContext.Session.SetString(ShoppingCart.CheckoutTokenKey, token);
        ViewBag.CheckoutToken = token;
        return View(new ShippingDetail());
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([Bind("Name,Email,Mobile,Address")] ShippingDetail detail, Guid checkoutToken)
    {
        var token = HttpContext.Session.GetString(ShoppingCart.CheckoutTokenKey);
        if (checkoutToken == Guid.Empty || token != checkoutToken.ToString("D"))
            return RedirectToAction(nameof(Checkout));

        var existingOrderId = await FindCheckoutOrder(checkoutToken);
        if (existingOrderId.HasValue) return CompleteCheckout(existingOrderId.Value);

        ViewBag.CheckoutToken = token;
        var lines = await shoppingCart.GetLinesAsync(HttpContext.Session);
        if (lines.Count == 0) ModelState.AddModelError("", "Giỏ hàng đang trống.");
        ViewBag.Total = lines.Sum(line => line.Total);
        if (!ModelState.IsValid) return View(detail);
        var order = new HoaDon
        {
            NgayLap = DateTime.UtcNow,
            CheckoutToken = checkoutToken,
            TongTien = lines.Sum(line => line.Total),
            ShippingDetail = detail,
            ChiTietHoaDons = lines.Select(line => new ChiTietHoaDon
            {
                MaMovie = line.Movie.ID, SoLuong = line.Quantity, DonGia = line.Movie.Price
            }).ToList()
        };
        db.HoaDons.Add(order);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error)
        {
            existingOrderId = await FindCheckoutOrder(checkoutToken);
            if (existingOrderId.HasValue) return CompleteCheckout(existingOrderId.Value);

            logger.LogWarning(error, "Không lưu được đơn hàng.");
            ModelState.AddModelError("", "Không thể lưu đơn hàng. Vui lòng thử lại.");
            return View(detail);
        }

        await EmailServiceNew.SendAsync(configuration, logger, detail.Email, order.ID, order.TongTien);
        return CompleteCheckout(order.ID);
    }

    public IActionResult CheckoutCompleted()
    {
        var order = HttpContext.Session.GetInt32(CheckoutKey);
        return order.HasValue ? View(order.Value) : RedirectToAction(nameof(Index));
    }

    private IActionResult CompleteCheckout(int orderId)
    {
        shoppingCart.Clear(HttpContext.Session);
        HttpContext.Session.SetInt32(CheckoutKey, orderId);
        return RedirectToAction(nameof(CheckoutCompleted));
    }

    private Task<int?> FindCheckoutOrder(Guid token) =>
        db.HoaDons.Where(order => order.CheckoutToken == token).Select(order => (int?)order.ID).SingleOrDefaultAsync();

    private async Task<IActionResult> MovieForm(string view, Movie movie)
    {
        ViewBag.Genres = new SelectList(await db.Genres.OrderBy(item => item.Name).ToListAsync(), "ID", "Name");
        return View(view, movie);
    }

    private async Task ValidateMovie(Movie movie, DateTime? existingDate = null)
    {
        if (movie.ReleaseDate.Date <= DateTime.Today && movie.ReleaseDate.Date != existingDate?.Date)
            ModelState.AddModelError(nameof(Movie.ReleaseDate), "Ngày phát hành phải sau hôm nay.");
        if (!await db.Genres.AnyAsync(genre => genre.ID == movie.GenreID))
            ModelState.AddModelError(nameof(Movie.GenreID), "Hãy chọn thể loại hợp lệ.");
        if (movie.PictureUpload is null) return;
        if (movie.PictureUpload.Length is < 8 or > MaxImageBytes)
        {
            ModelState.AddModelError(nameof(Movie.PictureUpload), "Ảnh phải có dung lượng tối đa 5 MB.");
            return;
        }
        using var stream = movie.PictureUpload.OpenReadStream();
        var header = new byte[8];
        await stream.ReadExactlyAsync(header);
        var extension = Path.GetExtension(movie.PictureUpload.FileName).ToLowerInvariant();
        var valid = extension switch
        {
            ".jpg" or ".jpeg" => header.AsSpan(0, 3).SequenceEqual(new byte[] { 255, 216, 255 }),
            ".png" => header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => false
        };
        if (!valid) ModelState.AddModelError(nameof(Movie.PictureUpload), "Chỉ chấp nhận ảnh JPEG hoặc PNG hợp lệ.");
    }

    private async Task<string> SavePicture(IFormFile image)
    {
        if (image is null) return null;
        var directory = Path.Combine(env.WebRootPath, "images", "uploads");
        Directory.CreateDirectory(directory);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(image.FileName).ToLowerInvariant();
        var url = $"/images/uploads/{name}";
        try
        {
            await using var file = System.IO.File.Create(Path.Combine(directory, name));
            await image.CopyToAsync(file);
            return url;
        }
        catch
        {
            RemovePicture(url);
            throw;
        }
    }

    private async Task SaveMovie(string newPath)
    {
        try { await db.SaveChangesAsync(); }
        catch { RemovePicture(newPath); throw; }
    }

    private void RemovePicture(string path)
    {
        if (path?.StartsWith("/images/uploads/", StringComparison.Ordinal) != true) return;
        try
        {
            System.IO.File.Delete(Path.Combine(env.WebRootPath, "images", "uploads", Path.GetFileName(path)));
        }
        catch (IOException error) { logger.LogWarning(error, "Không xóa được ảnh cũ."); }
        catch (UnauthorizedAccessException error) { logger.LogWarning(error, "Không xóa được ảnh cũ."); }
    }
}
