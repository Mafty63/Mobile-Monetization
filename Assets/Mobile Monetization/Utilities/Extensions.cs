using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class StringExtensions
{
    // Generate MD5 hash from string
    public static string ToMD5Hash(this string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    // Try to reverse an MD5 hash using a list of candidate strings
    public static string ReverseMD5Hash(this string hash, List<string> possibleOriginals)
    {
        foreach (string candidate in possibleOriginals)
        {
            if (candidate.ToMD5Hash().Equals(hash, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }
        return null;
    }
}
