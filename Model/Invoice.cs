using System.Security.Cryptography;
using System.Text;
using System.Web;

public class Invoice
{
   public string MerchantID = "2000132";
   public dynamic RqHeader;
   public dynamic Data;

   public Invoice(string customerName, List<dynamic> items)
   {
      var timestamp = GetTimestamp();
      // Console.WriteLine($"timestamp: {timestamp}");
      this.RqHeader = new { Timestamp = timestamp };
      this.Data = new
      {
         MerchantID = this.MerchantID,
         RelateNumber = $"louise{timestamp}",
         CustomerName = customerName, // print == 1 時必填
         CustomerAddr = "address", // print == 1 時必填
         CustomerPhone = "0954074430", // print == 1 時必填
         CustomerEmail = "louise87276@gmail.com", // print == 1 時必填
         Print = "1",
         Donation = "0",
         TaxType = "1",
         InvType = "07",
         vat = "1",
         SalesAmount = GetSaleAmount(items), // 總價
         Items = items //不確定會不會壞掉
      };
      // 不要這樣單獨一行一行更改 後續步驟不會執行 也不會有Error 也沒辦法這樣查詢 
      // this.Data.RelateNumber = $"louise{timestamp}";
      // this.Data.CustomerName = customerName;
      // this.Data.Items.Add(items);
      // this.Data.SalesAmount = GetSaleAmount(items);

      //TODO 弄一個全域變數 不然之後要改很麻煩 還有一個地方在Issue
      var Issue_HASHKEY = "ejCk326UnaZWKisg";
      var Issue_HASHIV = "q9jcZX8Ib9LM8wYk";

      // URLEncode
      var encodedData = HttpUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(this.Data));

      // AES 加密並儲存在 Data
      this.Data = EncryptData(Issue_HASHKEY, Issue_HASHIV, encodedData);
   }

   public string GetTimestamp()
   {
      return DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
   }

   public int GetSaleAmount(List<dynamic> items)
   {
      int salesAmount = 0;
      for (int i = 0; i < items.Count; i++)
      {
         salesAmount += items[i].ItemAmount;
      }
      // Console.WriteLine($"GetSaleAmount: {salesAmount}");
      return salesAmount;
   }

   private string EncryptData(string key, string iv, string data)
   {
      byte[] keyBytes = Encoding.UTF8.GetBytes(key);
      byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
      byte[] dataBytes = Encoding.UTF8.GetBytes(data);

      using (Aes aes = Aes.Create())
      {
         aes.Key = keyBytes;
         aes.IV = ivBytes;
         aes.Mode = CipherMode.CBC;
         aes.Padding = PaddingMode.PKCS7;

         using (ICryptoTransform encryptor = aes.CreateEncryptor())
         {
            byte[] encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
         }
      }
   }
}