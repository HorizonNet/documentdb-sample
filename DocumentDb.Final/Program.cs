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
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                const string DatabaseName = "BeerRegistry";

                // Check if database already exists
                Database database =
                    client.CreateDatabaseQuery().Where(db => db.Id == DatabaseName).AsEnumerable().FirstOrDefault();

                if (database == null)
                {
                    // Create the database
                    database = await client.CreateDatabaseAsync(new Database { Id = DatabaseName });
                }

                return database;
            }
        }

        private static async Task<DocumentCollection> CreateCollection(Database database)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                const string CollectionName = "Beers";

                // Check if collection already exists
                DocumentCollection collection =
                    client.CreateDocumentCollectionQuery(database.CollectionsLink)
                        .Where(c => c.Id == CollectionName)
                        .AsEnumerable()
                        .FirstOrDefault();

                if (collection == null)
                {
                    // Create collection
                    collection = await client.CreateDocumentCollectionAsync(
                                        database.CollectionsLink,
                                        new DocumentCollection { Id = CollectionName });
                }

                return collection;
            }
        }

        private static async Task SqlQueries(string documentsLink)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                dynamic affligem = JsonConvert.DeserializeObject(File.ReadAllText(@".\Data\Affligem.json"));
                dynamic diebels = JsonConvert.DeserializeObject(File.ReadAllText(@".\Data\Diebels.json"));

                // Persist the documents
                await client.CreateDocumentAsync(documentsLink, affligem);
                await client.CreateDocumentAsync(documentsLink, diebels);

                IQueryable<dynamic> query = client.CreateDocumentQuery(
                    documentsLink,
                    "SELECT * FROM Beers b WHERE b.id = 'Affligem'");
                var company = query.AsEnumerable().FirstOrDefault();

                Console.WriteLine("The Affligem brewery have the following beers:");

                if (company != null)
                {
                    foreach (var beer in company.beers)
                    {
                        Console.WriteLine(beer.name);
                    }
                }
            }
        }

        private static async Task LinqQueries(string documentsLink)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                const string Frankenheim = "Frankenheim";

                // Check if the document already exists
                Document document = client.CreateDocumentQuery(documentsLink)
                                        .Where(d => d.Id == Frankenheim)
                                        .AsEnumerable()
                                        .FirstOrDefault();

                if (document == null)
                {
                    // Create the document
                    var frankenheim = new Company
                                          {
                                              Id = Frankenheim,
                                              Location = "Germany",
                                              Beers = new[] { new Beer { Name = "Frankenheim blue", Level = 2.9 } }
                                          };

                    document = await client.CreateDocumentAsync(documentsLink, frankenheim);
                }

                IQueryable<Beer> beers = client.CreateDocumentQuery<Company>(documentsLink).SelectMany(c => c.Beers);

                foreach (Beer beer in beers.ToList())
                {
                    Console.WriteLine(beer.Name + " " + beer.Level);
                }

                document = await client.DeleteDocumentAsync(document.SelfLink);

                if (document == null)
                {
                    Console.WriteLine("Document deleted");
                }
            }
        }

        private static async Task UseSps(string storedProceduresLink, string documentsLink, string company1, string company2)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                const string StoredProcedureName = "SwapLocations";

                StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(storedProceduresLink).Where(sp => sp.Id == StoredProcedureName).AsEnumerable().FirstOrDefault();

                if (storedProcedure == null)
                {
                    // Register a stored procedure
                    storedProcedure = new StoredProcedure
                                        {
                                            Id = StoredProcedureName,
                                            Body = File.ReadAllText(@".\StoredProcedures\SwapLocations.js")
                                        };
                    storedProcedure = await client.CreateStoredProcedureAsync(storedProceduresLink, storedProcedure);
                }

                await client.ExecuteStoredProcedureAsync<dynamic>(storedProcedure.SelfLink, company1, company2);

                IQueryable<dynamic> query = client.CreateDocumentQuery(
                    documentsLink,
                    "SELECT * FROM Beers b WHERE b.id = 'Affligem'");
                var company = query.AsEnumerable().FirstOrDefault();

                Console.WriteLine("The Affligem brewery is located in:");

                if (company != null)
                {
                    Console.WriteLine(company.location);
                }

                query = client.CreateDocumentQuery(
                    documentsLink,
                    "SELECT * FROM Beers b WHERE b.id = 'Diebels'");
                company = query.AsEnumerable().FirstOrDefault();

                Console.WriteLine("The Diebels brewery is located in:");

                if (company != null)
                {
                    Console.WriteLine(company.location);
                }
            }
        }

        private static async Task UseUdf(string userDefinedFunctionsLink, string collectionLink)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                const string UdfMaxName = "Sqrt";

                UserDefinedFunction userDefinedFunction =
                    client.CreateUserDefinedFunctionQuery(userDefinedFunctionsLink)
                        .Where(udf => udf.Id == UdfMaxName)
                        .AsEnumerable()
                        .FirstOrDefault();

                if (userDefinedFunction == null)
                {
                    // Register User Defined Function "Max"
                    userDefinedFunction = new UserDefinedFunction
                                              {
                                                  Id = UdfMaxName,
                                                  Body = File.ReadAllText(@".\UDFs\Sqrt.js")
                                              };
                    await client.CreateUserDefinedFunctionAsync(userDefinedFunctionsLink, userDefinedFunction);
                }

                var query = client.CreateDocumentQuery(collectionLink, "SELECT Sqrt(b.level) FROM b IN Beers.beers");
                var result = query.AsEnumerable().FirstOrDefault();

                Console.WriteLine("Sqrt: " + result);
            }
        }

        private static async Task DeleteDatabase(Database database)
        {
            using (var client = new DocumentClient(new Uri(Endpoint), AuthKey))
            {
                await client.DeleteDatabaseAsync(database.SelfLink);
            }
        }
    }
}