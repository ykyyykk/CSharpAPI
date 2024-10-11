using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;

public class Invoice
{
   private readonly InvoiceSettings invoiceSettings;
   public dynamic RqHeader;
   public dynamic Data;

   public Invoice(IOptions<InvoiceSettings> invoiceSettings, string customerName, List<dynamic> items)
   {
      this.invoiceSettings = invoiceSettings.Value;
      var timestamp = GetTimestamp();
      // Console.WriteLine($"timestamp: {timestamp}");
      this.RqHeader = new { Timestamp = timestamp };
      this.Data = new
      {
         MerchantID = this.invoiceSettings.MerchantID,
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

      // URLEncode
      var encodedData = HttpUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(this.Data));

      // AES 加密並儲存在 Data
      this.Data = EncryptData(this.invoiceSettings.HashKey, this.invoiceSettings.HashIV, encodedData);
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