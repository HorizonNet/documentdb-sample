using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DocumentDb.Models;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

using Newtonsoft.Json;

namespace DocumentDb
{
    public class Program
    {
        private const string Endpoint = "https://<your-documentdb>.documents.azure.com:443/";
        private const string AuthKey = "<auth key>";

        public static void Main(string[] args)
        {
            Database database = CreateDatabase().Result;
            DocumentCollection collection = CreateCollection(database).Result;
            SqlQueries(collection.DocumentsLink).Wait();
            LinqQueries(collection.DocumentsLink).Wait();
            UseSps(collection.StoredProceduresLink, collection.DocumentsLink, "Affligem", "Diebels").Wait();
            UseUdf(collection.UserDefinedFunctionsLink, collection.SelfLink).Wait();
            DeleteDatabase(database).Wait();

            Console.WriteLine();
            Console.WriteLine("Done!");

            Console.ReadKey();
        }

        private static async Task<Database> CreateDatabase()
        {
            return null;
        }

        private static async Task<DocumentCollection> CreateCollection(Database database)
        {
            return null;
        }

        private static async Task SqlQueries(string documentsLink)
        {
        }

        private static async Task LinqQueries(string documentsLink)
        {
        }

        private static async Task UseSps(string storedProceduresLink, string documentsLink, string company1, string company2)
        {
        }

        private static async Task UseUdf(string userDefinedFunctionsLink, string collectionLink)
        {
        }

        private static async Task DeleteDatabase(Database database)
        {
        }
    }
}