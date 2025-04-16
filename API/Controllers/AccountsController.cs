using CMS.API.Domains;
using CMS.API.Dtos;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{

    private readonly UserManager<IdentityUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly AppDBContext dBContext;
    private readonly ITokenProvider tokenProvider;

    public AccountsController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDBContext dBContext,
        ITokenProvider tokenProvider)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.dBContext = dBContext;
        this.tokenProvider = tokenProvider;
    }


    [HttpPost]
    [Route("register")]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> RegisterAsync(SignUp signup)
    {

        bool isExist = await userManager.Users.AnyAsync(u => u.UserName == signup.Email);
        if (isExist)
            return BadRequest("Username already exist");

        bool isVendorExist = await dBContext.Vendors.AnyAsync(x => x.Id == signup.VendorId);
        if (!isVendorExist)
            return BadRequest("Vendor not found");

        bool isRoleExist = await roleManager.RoleExistsAsync(signup.Role);
        if (!isRoleExist)
            return BadRequest("Role not found");

        var result = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = signup.Email,
            Email = signup.Email,
            VendorId = signup.VendorId,
            EmailConfirmed = true
        }, signup.Password);


        if (!result.Succeeded)
            return BadRequest(string.Join(", ", result.Errors.Select(x => x.Description).ToList()));

        IdentityUser? user = await userManager.FindByNameAsync(signup.Email);

        await userManager.AddToRoleAsync(user!, signup.Role);


        return Ok(new { Message = "User created successfully" });
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> LoginAsync(Login login)
    {

        var user = await dBContext.ApplicationUsers.Where(x => x.UserName == login.Username.Trim()).FirstOrDefaultAsync();
        if (user is null)
            return BadRequest("Incorrect username or password");

        var validUser = await userManager.CheckPasswordAsync(user, login.Password);

        if (!validUser)
            return BadRequest("Incorrect username or password!");


        List<Claim> claims = [  new(ClaimTypes.Name, user.UserName!),
                                new Claim(SD.VendorId, user.VendorId.ToString()!),
                                new Claim(SD.AppType,  StrDir.AppType.POS) ];

        var roles = await userManager.GetRolesAsync(user);

        if (roles.Count > 0)
        {
            claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList());
        }

        var token = tokenProvider.GenerateJWTTokens(claims);


        var tokens = dBContext.RefreshTokens.Where(x => x.UserName == login.Username && x.CreatedOn <= DateTime.Now.AddMonths(-3));
        dBContext.RefreshTokens.RemoveRange(tokens);

        await dBContext.RefreshTokens.AddAsync(new RefreshToken() { UserName = user.UserName, Token = token.RefreshToken, CreatedOn = DateTime.Now, IsActive = true });
        await dBContext.SaveChangesAsync();
        return Ok(token);
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("login/machine")]
    public async Task<IActionResult> MachineLoginAsync(MachineLoginDto login)
    {
        var machine = await dBContext.Machines.Where(m => m.IsActive == true && m.Id == login.Key && login.Mac.Contains(m.Mac)).FirstOrDefaultAsync();

        if (machine is null)
            return BadRequest("Machine not registered");

        List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, machine.Name),
                new Claim(ClaimTypes.Role,SD.Role_Machine),
                new Claim(SD.MachineId, machine.Id.ToString()),
                new Claim(SD.MachineNumber, machine.MachineNumber.ToString()),
                new Claim(SD.VendorId, machine.VendorId.ToString()!),
                new Claim(SD.AppType, machine.AppType)
            };

        var token = tokenProvider.GenerateJWTTokens(claims, 24 * 60);

        return Ok(token);
    }


    [AllowAnonymous]
    [HttpPost]
    [Route("refresh")]
    public async Task<IActionResult> RefreshTokenAsync(Token expiredtoken)
    {

        JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken token = jwtSecurityTokenHandler.ReadJwtToken(expiredtoken.JwtToken);

        var username = token.Payload.TryGetValue("unique_name", out object? value) ? (string)value : null;

        if (username is null)
            return BadRequest("Invalid token");

        var savedTokens = await dBContext.RefreshTokens.Where(x => x.UserName == username && x.Token == expiredtoken.RefreshToken).FirstOrDefaultAsync();
        if (savedTokens is null)
            return BadRequest("Invalid refreshtoken");

        var user = await dBContext.ApplicationUsers.Where(x => x.UserName == username).FirstOrDefaultAsync();
        if (user is null)
            return BadRequest("Invalid user");

        List<Claim> claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, user.UserName!),
                                    new Claim("VendorId", user.VendorId.ToString()!)
                                };

        var roles = await userManager.GetRolesAsync(user);

        if (roles.Count > 0)
        {
            claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList());
        }

        var newtoken = tokenProvider.GenerateJWTTokens(claims);


        dBContext.RefreshTokens.Remove(savedTokens);

        await dBContext.RefreshTokens.AddAsync(new RefreshToken() { UserName = user.UserName, Token = newtoken.RefreshToken, CreatedOn = DateTime.Now, IsActive = true });
        await dBContext.SaveChangesAsync();

        return Ok(newtoken);
    }

    [HttpPost]
    [Route("logout")]
    [Authorize]
    public async Task<IActionResult> LogOutAsync()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
            return Ok();

        var tokens = dBContext.RefreshTokens.Where(x => x.UserName == username);
        dBContext.RefreshTokens.RemoveRange(tokens);
        await dBContext.SaveChangesAsync();
        return Ok();
    }
}
