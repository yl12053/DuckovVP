using System.Linq;

namespace DuckovVP.Console.Parser.Bili;

public static class BiliUtils
{
    public const string BASE58_MAPPING = "FcwAPNKTMug3GV5Lj7EJnHpWsx4tb8haYeviqBz6rkCy12mUSDQX9RdoZf";
    public const int BV_LEN = 12;
    public const ulong XOR_CODE = 23442827791579;
    public const ulong MASK_CODE = 2251799813685247;
    public const ulong MAX_AID = 1UL << 51;
    
    public static string Aid2Bid(ulong av)
    {
        char[] bytes = new[] {'B', 'V', '1', '0', '0', '0', '0', '0', '0', '0', '0', '0'};
        int bv_idx = BV_LEN - 1;
        ulong tmp = (MAX_AID | av) ^ XOR_CODE;
        while (tmp != 0)
        {
            bytes[bv_idx] = BASE58_MAPPING[(int) (tmp % 58ul)];
            tmp /= 58ul;
            bv_idx -= 1;
        }
        (bytes[3], bytes[9]) = (bytes[9], bytes[3]);
        (bytes[4], bytes[7]) = (bytes[7], bytes[4]);
        return new(bytes);
    }

    public static ulong Bid2Aid(string bv)
    {
        var bytes = bv.ToList();
        (bytes[3], bytes[9]) = (bytes[9], bytes[3]);
        (bytes[4], bytes[7]) = (bytes[7], bytes[4]);
        bytes.RemoveRange(0, 3);
        ulong tmp = 0;
        foreach (var i in bytes)
        {
            uint idx = (uint) BASE58_MAPPING.IndexOf(i);
            tmp = tmp * 58 + idx;
        }

        return (tmp & MASK_CODE) ^ XOR_CODE;
    } 
}