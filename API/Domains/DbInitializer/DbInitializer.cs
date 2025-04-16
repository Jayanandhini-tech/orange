using CMS.API.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Domains.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly AppDBContext db;
    private readonly ILogger<DbInitializer> logger;

    public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, AppDBContext db, ILogger<DbInitializer> logger)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.db = db;
        this.logger = logger;
    }

    public async Task Initialize()
    {


        //migrations if they are not applied
        try
        {
            if (db.Database.GetPendingMigrations().Count() > 0)
            {
                db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        if (!db.Vendors.Any(x => x.Name!.ToLower() == "bvc"))
            db.Vendors.Add(new Vendor() { Id = 1, Name = "BVC", ShortName = "BVC", CreatedOn = DateTime.Now, IsActive = true });


        if (!db.Vendors.Any(x => x.Name!.ToLower() == "demo"))
            db.Vendors.Add(new Vendor() { Id = 2, Name = "DEMO", ShortName = "BVT", CreatedOn = DateTime.Now, IsActive = true });




        // create roles if they are not created
        if (!await roleManager.RoleExistsAsync(SD.Role_Admin))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));

        if (!await roleManager.RoleExistsAsync(SD.Role_User))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_User));

        if (!await roleManager.RoleExistsAsync(SD.Role_Machine))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Machine));

        if (!await roleManager.RoleExistsAsync(SD.Role_Employee))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));

        if (!await roleManager.RoleExistsAsync(SD.Role_Student))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Student));


        if (!db.ApplicationUsers.Any(x => x.UserName == "siva@bvc24.com"))
        {
            //if roles are not created, then we will create admin user as well
            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "siva@bvc24.com",
                Email = "siva@bvc24.com",
                VendorId = 1
            }, "S!va.BVC@1");


            ApplicationUser? user = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == "siva@bvc24.com");
            if (user != null)
                await userManager.AddToRoleAsync(user!, SD.Role_Admin);

        }


        await db.SaveChangesAsync();
        return;
    }
}
