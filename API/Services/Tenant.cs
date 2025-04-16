using CMS.API.Dtos;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using System.Security.Claims;

namespace CMS.API.Services;

public class Tenant : ITenant
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private ClaimsPrincipal? user;


    public Tenant(IHttpContextAccessor httpContextAccessor)
    {
        user = httpContextAccessor.HttpContext?.User;
        this.httpContextAccessor = httpContextAccessor;
    }

    public string AppType
    {
        get
        {
            //if (user == null)
            //    return StrDir.AppType.POS;

            //var appType = user.Claims.FirstOrDefault(x => x.Type == SD.AppType)?.Value;
            //return string.IsNullOrEmpty(appType) ? string.Empty : appType.Trim().ToUpper();

            if (httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("X-APP-TYPE") ?? false)
            {
                string? appType = httpContextAccessor.HttpContext!.Request.Headers["X-APP-TYPE"];
                return string.IsNullOrEmpty(appType) ? string.Empty : appType.Trim().ToUpper();
            }
            else
            {
                if (user == null)
                    return string.Empty;

                var appType = user.Claims.FirstOrDefault(x => x.Type == SD.AppType)?.Value;
                return string.IsNullOrEmpty(appType) ? string.Empty : appType.Trim().ToUpper();
            }

        }
    }

    public int VendorId
    {
        get
        {
            if (user == null)
                return 0;
            var vId = user.Claims.FirstOrDefault(x => x.Type == SD.VendorId)?.Value;
            int.TryParse(vId, out int VendorId);
            return VendorId;
        }
    }


    public string MachineId
    {
        get
        {
            if (user == null)
                return string.Empty;

            var mId = user.Claims.FirstOrDefault(x => x.Type == SD.MachineId)?.Value;
            return mId ?? string.Empty;
        }
    }

    public string MachineName
    {
        get
        {
            if (user == null)
                return string.Empty;

            var Name = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            return Name ?? string.Empty;
        }
    }

    public int MachineNumber
    {
        get
        {
            if (user == null)
                return 0;

            var mNo = user.Claims.FirstOrDefault(x => x.Type == SD.MachineNumber)?.Value;
            int.TryParse(mNo, out int machineNumber);
            return machineNumber;
        }
    }

}
