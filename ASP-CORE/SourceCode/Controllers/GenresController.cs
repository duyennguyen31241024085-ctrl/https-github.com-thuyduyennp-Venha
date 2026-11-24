using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Data;
using MSSV_HoVaTen.Models;

namespace MSSV_HoVaTen.Controllers;

public class GenresController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Genres.OrderBy(item => item.Name).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var genre = await db.Genres.FindAsync(id);
        return genre is null ? NotFound() : View(genre);
    }

    [Authorize(Policy = "CatalogEditor")]
    public IActionResult Create() => View(new Genre());

    [HttpPost, Authorize(Policy = "CatalogEditor")]
    public Task<IActionResult> Create([Bind("Name")] Genre genre) => Save(genre, genre.Name, "Create");

    [Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Edit(int id)
    {
        var genre = await db.Genres.FindAsync(id);
        return genre is null ? NotFound() : View(genre);
    }

    [HttpPost, Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Edit(int id, [Bind("Name")] Genre input)
    {
        var genre = await db.Genres.FindAsync(id);
        return genre is null ? NotFound() : await Save(genre, input.Name, "Edit");
    }

    [Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await db.Genres.FindAsync(id);
        return genre is null ? NotFound() : View(genre);
    }

    [HttpPost, ActionName("Delete"), Authorize(Policy = "CatalogEditor")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var genre = await db.Genres.FindAsync(id);
        if (genre is null) return NotFound();
        db.Genres.Remove(genre);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể xóa thể loại đang có phim.");
            return View("Delete", genre);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(Genre genre, string name, string view)
    {
        genre.Name = name?.Trim();
        if (await db.Genres.AnyAsync(item => item.ID != genre.ID && item.Name == genre.Name))
            ModelState.AddModelError(nameof(Genre.Name), "Thể loại này đã tồn tại.");
        if (!ModelState.IsValid) return View(view, genre);
        if (genre.ID == 0) db.Genres.Add(genre);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể lưu thể loại.");
            return View(view, genre);
        }
        return RedirectToAction(nameof(Index));
    }
}
