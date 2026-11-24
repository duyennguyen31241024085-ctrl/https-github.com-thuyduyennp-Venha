using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MSSV_HoVaTen.Data;
using MSSV_HoVaTen.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var local = string.IsNullOrWhiteSpace(connectionString);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (local)
        options.UseSqlite("Data Source=framebox.db");
    else
        options.UseSqlServer(connectionString);
});
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews(options => options.Filters.Add(
    new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddScoped<MSSV_HoVaTen.Extension.ShoppingCart>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => options.Cookie.IsEssential = true);
builder.Services.AddAuthorization(options => options.AddPolicy("CatalogEditor", policy =>
{
    var editor = builder.Configuration["Catalog:EditorUserId"];
    policy.RequireAssertion(context =>
        (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(editor)) ||
        (context.User.Identity?.IsAuthenticated == true &&
         !string.IsNullOrWhiteSpace(editor) &&
         string.Equals(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, editor, StringComparison.Ordinal)));
}));
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Movies}/{action=Index}/{id?}");
app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (local)
        await db.Database.EnsureCreatedAsync();
    if (local || app.Configuration.GetValue<bool>("Catalog:SeedDemo"))
        await DemoCatalog.SeedAsync(db);
}
app.Run();
