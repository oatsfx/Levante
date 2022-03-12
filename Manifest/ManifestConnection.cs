using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace Levante.Manifest
{
    internal class ManifestConnection
    {
        public class ManifestRepository
        {
            public static string DbFile =>
                // TODO: change this to reflect self updating
                "Data/Manifest/world_sql_content_f5d265c7cb4dc5794bc2006c58a1f33b.content";

            public static SQLiteConnection ManifestDBConnection()
            {
                return new SQLiteConnection("Data Source=" + DbFile);
            }

            public static DestinyInventoryItemDefinition GetInventoryItem(int id)
            {
                using var cnn = ManifestDBConnection();
                cnn.Open();
                var result = cnn.Query<DestinyInventoryItemDefinition>(
                    @"SELECT json FROM DestinyInventoryItemDefinition WHERE Id = @id", new { id }).FirstOrDefault();
                return result;
            }

            public static DestinyInventoryItemDefinition GetInventoryItemByName(string name)
            {
                using var cnn = ManifestDBConnection();
                cnn.Open();
                var result = cnn.Query<DestinyInventoryItemDefinition>(
                    @"SELECT json FROM DestinyInventoryItemDefinition WHERE json LIKE %@name%", new { name }).FirstOrDefault();
                return result;
            }
        }
    }
}
