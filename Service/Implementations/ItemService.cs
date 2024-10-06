
// using MySqlConnector;

// public class ItemService : IItemService
// {
//    private readonly MySqlConnection connection;

//    public ItemService(MySqlConnection connection)
//    {
//       this.connection = connection;
//    }

//    public async Task<(bool success, string message)> PurchaseItemAsync(string itemID, int amount)
//    {
//       if (connection.State.HasFlag(System.Data.ConnectionState.Open))
//       {
//          await connection.OpenAsync();
//       }

//       Item item = null;

//       using (var command = new MySqlCommand("Select * FROM Item WHERE id = ?", connection))
//       {
//          command.Parameters.AddWithValue("?", itemID);
//          using var dataReader = await command.ExecuteReaderAsync();

//          if (await dataReader.ReadAsync())
//          {
//             item = new Item()
//             {
//                id = Convert.ToInt64(dataReader["id"]),
//                name = (string)dataReader["name"],
//                detail = (string)dataReader["detail"],
//                price = Convert.ToInt32(dataReader["price"]),
//                stock = Convert.ToInt32(dataReader["stock"]),
//                category = (string)dataReader["category"],
//                status = (string)dataReader["status"],
//                saleAmount = Convert.ToInt32(dataReader["saleAmount"]),
//                thumbnail = (string)dataReader["thumbnail"],
//             };
//          }
//       }

//       if (item == null)
//       {
//          return (false, "找不到物品");
//       }

//       if (item.stock < amount)
//       {
//          return (false, "庫存不足");
//       }

//       item.stock -= amount;
//       item.saleAmount += amount;
//       using (var updateCommand = new MySqlCommand(
//          @"UPDATE Item
//            SET stock = ?, saleAmount = ?
//            WHERE id = ?",
//            connection
//       ))
//       {
//          updateCommand.Parameters.AddWithValue("?", item.stock);
//          updateCommand.Parameters.AddWithValue("?", item.saleAmount);
//          updateCommand.Parameters.AddWithValue("?", item.id);
//          await updateCommand.ExecuteNonQueryAsync();
//       }
//       return (true, "減少庫存物品 並 增加銷量");
//    }
// }