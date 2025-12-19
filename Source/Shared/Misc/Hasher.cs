using System;
using System.Security.Cryptography;
using System.Text;

namespace Shared;

public static class Hasher
{
    public static string GetHashFromString(object input, bool noSpecialChars = true)
    {
        using SHA256 shaAlgorythm = SHA256.Create();
        byte[] code = shaAlgorythm.ComputeHash(Encoding.ASCII.GetBytes(input.ToString()));

        if (noSpecialChars) return BitConverter.ToString(code).Replace("-", "");
        else return BitConverter.ToString(code);
    }
}