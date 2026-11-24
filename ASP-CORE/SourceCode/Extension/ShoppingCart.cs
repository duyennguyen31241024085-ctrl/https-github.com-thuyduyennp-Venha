using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Data;
using MSSV_HoVaTen.Models;

namespace MSSV_HoVaTen.Extension;

public class ShoppingCart(ApplicationDbContext db)
{
    private const string CartKey = "HoaDon";
    public const string CheckoutTokenKey = "CheckoutToken";

    public Dictionary<int, int> Read(ISession session) =>
        session.Get<Dictionary<int, int>>(CartKey) ?? [];

    public void Store(ISession session, Dictionary<int, int> items)
    {
        session.Remove(CheckoutTokenKey);
        session.Set(CartKey, items);
    }

    public void Clear(ISession session) => session.Remove(CartKey);

    public async Task<List<CartLine>> GetLinesAsync(ISession session)
    {
        var items = Read(session);
        if (items.Count == 0) return [];
        var movies = await db.Movies.AsNoTracking().Where(movie => items.Keys.Contains(movie.ID))
            .OrderBy(movie => movie.ID).ToListAsync();
        return movies.Select(movie => new CartLine(movie, items[movie.ID])).ToList();
    }
}
