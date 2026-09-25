using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public static class DiscoveryService
{
    public static void DiscoverDashboard(LanConfig config, Action<bool, string> onResult)
    {
        // Instance() may create a GameObject, so it must run on the main
        // thread (the caller's). The ThreadPool callback below only calls
        // Enqueue on this already-created instance.
        UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance();
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            string url = TryDiscover(config);
            dispatcher.Enqueue(() =>
            {
                onResult?.Invoke(!string.IsNullOrEmpty(url), url);
            });
        });
    }

    private static string TryDiscover(LanConfig config)
    {
        AndroidJavaObject multicastLock = AcquireMulticastLock();
        try
        {
            using (UdpClient udp = new UdpClient())
            {
                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = config.discoveryTimeoutMs;
                byte[] magic = Encoding.ASCII.GetBytes(config.discoveryMagic);
                udp.Send(magic, magic.Length, new IPEndPoint(IPAddress.Broadcast, config.discoveryPort));

                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] reply = udp.Receive(ref remote);
                string text = Encoding.ASCII.GetString(reply);
                return ParseResponse(config, text);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DiscoveryService] {ex.Message}");
            return null;
        }
        finally
        {
            ReleaseMulticastLock(multicastLock);
        }
    }

    private static string ParseResponse(LanConfig config, string text)
    {
        string[] parts = text.Split('|');
        if (parts.Length >= 4
            && parts[0] == config.expectedResponsePrefix
            && parts[1] == "v1"
            && IsValidHost(parts[2])
            && int.TryParse(parts[3], out int port)
            && port >= 1 && port <= 65535)
        {
            if (!IsPrivateLanHost(parts[2]))
            {
                Debug.LogWarning($"[DiscoveryService] Host rechazado, no es privado/local: {parts[2]}");
                return null;
            }
            return $"http://{parts[2]}:{parts[3]}";
        }
        Debug.LogWarning($"[DiscoveryService] Respuesta inesperada: {text}");
        return null;
    }

    private static bool IsValidHost(string host)
    {
        return !string.IsNullOrEmpty(host)
            && Uri.CheckHostName(host) != UriHostNameType.Unknown;
    }

    // A discovery reply may come from any device on the LAN, so the host it
    // advertises is only trusted when it is private/local: an IPv4 literal in
    // a private range, or a DNS name that resolves into one of those ranges.
    // Anything else (public IPs, IPv6 literals, unresolvable names) is
    // rejected so the client never leaves the LAN.
    private static bool IsPrivateLanHost(string host)
    {
        if (IPAddress.TryParse(host, out IPAddress address))
        {
            return address.AddressFamily == AddressFamily.InterNetwork
                && IsPrivateIPv4(address);
        }

        try
        {
            foreach (IPAddress resolved in Dns.GetHostAddresses(host))
            {
                if (resolved.AddressFamily == AddressFamily.InterNetwork && IsPrivateIPv4(resolved))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DiscoveryService] No se pudo resolver el host '{host}': {ex.Message}");
        }
        return false;
    }

    private static bool IsPrivateIPv4(IPAddress address)
    {
        byte[] b = address.GetAddressBytes();
        return b.Length == 4
            && (b[0] == 10                                // 10.0.0.0/8
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)  // 172.16.0.0/12
            || (b[0] == 192 && b[1] == 168)               // 192.168.0.0/16
            || (b[0] == 169 && b[1] == 254));             // 169.254.0.0/16 (link-local)
    }

    private static AndroidJavaObject AcquireMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                AndroidJavaObject multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "neuroflex");
                multicastLock.Call("acquire");
                return multicastLock;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DiscoveryService] MulticastLock error: {ex.Message}");
            return null;
        }
#else
        return null;
#endif
    }

    private static void ReleaseMulticastLock(AndroidJavaObject multicastLock)
    {
        if (multicastLock == null)
        {
            return;
        }
        try
        {
            multicastLock.Call("release");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DiscoveryService] Release lock error: {ex.Message}");
        }
        finally
        {
            multicastLock.Dispose();
        }
    }
}
