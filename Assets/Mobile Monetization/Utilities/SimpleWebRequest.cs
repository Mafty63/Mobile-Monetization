using UnityEngine.Networking;

namespace MobileCore.Utilities
{
    public static class SimpleWebRequest
    {
        private static UnityWebRequest www;

        public static void LoadFile(string url)
        {
            www = UnityWebRequest.Get(url);
            www.SendWebRequest();
        }

        public static bool IsDone()
        {
            return www.isDone;
        }

        public static string GetResult()
        {
            if (string.IsNullOrEmpty(www.error))
            {
                return www.downloadHandler.text;
            }
            return null;
        }
    }
}