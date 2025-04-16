using System.Text;

namespace CMS.API.Extensions;

public static class StringHelper
{
    public static string ConvertBase64ToString(this string encodedString)
    {
        byte[] data = Convert.FromBase64String(encodedString);
        string decodedString = Encoding.UTF8.GetString(data);
        return decodedString;
    }
}
