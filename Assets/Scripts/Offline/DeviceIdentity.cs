using UnityEngine;

public static class DeviceIdentity
{
    private const string DeviceIdKey = "device_id";
    private const string DeviceTokenKey = "device_token";

    public static string DeviceId
    {
        get
        {
            if (!PlayerPrefs.HasKey(DeviceIdKey))
            {
                PlayerPrefs.SetString(DeviceIdKey, System.Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(DeviceIdKey);
        }
    }

    public static string DeviceToken
    {
        get => PlayerPrefs.GetString(DeviceTokenKey, "");
        set
        {
            PlayerPrefs.SetString(DeviceTokenKey, value);
            PlayerPrefs.Save();
        }
    }

    public static void ClearToken()
    {
        PlayerPrefs.DeleteKey(DeviceTokenKey);
        PlayerPrefs.Save();
    }
}
