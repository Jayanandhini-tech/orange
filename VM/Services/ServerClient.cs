using CMS.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using VM.Services.Interfaces;

namespace VM.Services;
public class ServerClient : IServerClient
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly IEventService eventService;
    private readonly ILogger<ServerClient> logger;
    private string jwtToken = "";
    private DateTime expiredOn = DateTime.Now;

    public ServerClient(HttpClient httpClient, IConfiguration configuration, IEventService eventService, ILogger<ServerClient> logger)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.eventService = eventService;
        this.logger = logger;
        this.httpClient.BaseAddress = new Uri(configuration["BaseURL"]!.TrimEnd('/'));
    }


    public async Task<bool> IsServerConnectable()
    {
        try
        {
            var response = await httpClient.GetAsync("/api");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string respJson = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Status Code : {response.StatusCode}, Response :{respJson}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        HttpRequestMessage httpRequest = new HttpRequestMessage() { Method = HttpMethod.Get, RequestUri = new Uri(url, uriKind: UriKind.Relative) };
        return await SendAsync(httpRequest);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string url, T? body = null) where T : class
    {
        HttpRequestMessage httpRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(url, uriKind: UriKind.Relative)
        };

        if (body is not null)
            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");


        return await SendAsync(httpRequest);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent form)
    {


        HttpRequestMessage httpRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(url, uriKind: UriKind.Relative),
            Content = form
        };

        return await SendAsync(httpRequest);
    }

    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        return await httpClient.GetByteArrayAsync(url);
    }

    public async Task<string> GetToken()
    {
        if (jwtToken != "" && expiredOn > DateTime.Now)
            return jwtToken;

        LoginDto login = new LoginDto()
        {
            Key = configuration["MachineKey"]!,
            Mac = GetMacAddress()
        };
        var content = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json");
        var httpResp = await httpClient.PostAsync("/api/accounts/login/machine", content);
        string response = await httpResp.Content.ReadAsStringAsync();

        if (httpResp.StatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogInformation("Token Generation failed");
            logger.LogInformation(response);
        }

        if (httpResp.IsSuccessStatusCode)
        {
            var token = JsonConvert.DeserializeObject<Token>(response);
            if (token != null)
            {
                jwtToken = token.JwtToken;
                expiredOn = token.Expired;
            }
        }

        return jwtToken;
    }


    public async Task DownloadImage(string url, string localPath)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(localPath, fileBytes);
            }
        }
        catch (DirectoryNotFoundException)
        {
            string dir = new FileInfo(localPath).DirectoryName ?? "";
            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private List<string> GetMacAddress()
    {
        List<string> macIds = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet).Select(x => x.GetPhysicalAddress().ToString().Replace("-", "")).ToList();

        if (macIds.Count == 0)
            macIds = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).Select(x => x.GetPhysicalAddress().ToString().Replace("-", "")).ToList();

        return macIds;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        try
        {
            string token = await GetToken();
            if (string.IsNullOrEmpty(token))
                throw new Exception("Authendication failed");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var httpResponse = await httpClient.SendAsync(request);

            //if (!httpResponse.IsSuccessStatusCode)
            //{
            //    string requestbody = request.Content is not null ? await request.Content.ReadAsStringAsync() : "";
            //    string respJson = await httpResponse.Content.ReadAsStringAsync();
            //    string url = request.RequestUri is not null ? request.RequestUri.AbsolutePath : "";
            //    //throw new Exception($"http request failed. request : {url}, body : {requestbody} , status code : {httpResponse.StatusCode},  Response : {respJson}");
            //}
            eventService.RaiseNetworkStatusChanged(true);
            return httpResponse;
        }
        catch (HttpRequestException httpException)
        {
            if (httpException.HttpRequestError == HttpRequestError.ConnectionError)
            {
                if (eventService.IsNetworkAvailable == false)
                    eventService.RaiseNetworkMessageEvent("Unable to connect with Internet");

                eventService.RaiseNetworkStatusChanged(false);
            }
            throw;
        }
        catch
        {
            throw;
        }
    }

}