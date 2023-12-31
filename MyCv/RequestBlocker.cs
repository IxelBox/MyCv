using System.Collections.Concurrent;
using System.Net;

namespace MyCv;

public class RequestBlocker
{
    private readonly ConcurrentDictionary<string, int> _accessCounter = new();
    
    public bool BlockRequest(HttpRequest request)
    {
        var ipAddress = GetUserIpAddress(request);

        var localCounter = _accessCounter.AddOrUpdate(ipAddress, key => 1, (key, old) => Interlocked.Increment(ref old));
        return localCounter > 10;
    }
    
    public void RemoveBlockCount(HttpRequest request)
    {
        var ipAddress = GetUserIpAddress(request);

        _accessCounter.TryRemove(ipAddress, out int _);
    }
    
    public  string GetUserIpAddress(HttpRequest request)
    {
        bool lan = false;
            string? userIPAddress = request.Headers["HTTP_X_FORWARDED_FOR"];

            if (String.IsNullOrEmpty(userIPAddress))
                userIPAddress = request.Headers["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(userIPAddress))
                userIPAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(userIPAddress) || userIPAddress.Trim() == "::1")
            {
                lan = true;
                userIPAddress = string.Empty;
            }

            if (lan)
            {
                if (string.IsNullOrEmpty(userIPAddress))
                {
                    //This is for Local(LAN) Connected ID Address
                    string stringHostName = Dns.GetHostName();
                    //Get Ip Host Entry
                    IPHostEntry ipHostEntries = Dns.GetHostEntry(stringHostName);
                   
                    System.Net.IPAddress[] arrIpAddress = ipHostEntries.AddressList;

                    try
                    {
                        foreach (IPAddress ipAddress in arrIpAddress)
                        {
                            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                userIPAddress= ipAddress.ToString();
                            }
                        }

                        if(string.IsNullOrEmpty(userIPAddress))
                        userIPAddress = arrIpAddress[^1].ToString();
                    }
                    catch
                    {
                        try
                        {
                            userIPAddress = arrIpAddress[0].ToString();
                        }
                        catch
                        {
                            try
                            {
                                arrIpAddress = Dns.GetHostAddresses(stringHostName);
                                userIPAddress = arrIpAddress[0].ToString();
                            }
                            catch
                            {
                                //local address
                                userIPAddress = "127.0.0.1";
                            }
                        }
                    }
                }
            }

            return userIPAddress;
        }
}