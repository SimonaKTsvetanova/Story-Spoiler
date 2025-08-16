using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;




namespace Story_Spoiler
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string storyId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("SimonaTsvet", "Simo1234");
            var option = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(option);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });
            
            var response = loginClient.Execute(request);

            var json = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void CreateNewStory_ShouldReturn201AndStoryId()
        {

            var request = new RestRequest("/api/Story/Create", Method.Post);

            var body = new
            {
                title = "The Hidden Truth",
                author = "Jane Doe",
                description = "Once upon a time... (spoiler removed)",
                genre = "Drama"
            };
            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), 
                "Expected status code 201 (Created)");

            var json = JObject.Parse(response.Content);

            Assert.That(json["storyId"], Is.Not.Null, "Response should contain a storyId");

            storyId = json["storyId"].Value<string>();

        }

        [Test, Order(2)]

        public void EditStoryShould_Return200AndSuccessMessage()
        {
           
            var request = new RestRequest($"/api/Story/Edit/{storyId}", Method.Put);
            

            var updatedBody = new
            {
                title = "The Hidden Truth - Revised",
                author = "Jane Doe",
                description = "Updated story content with new spoilers...",
                genre = "Thriller"
            };

            request.AddJsonBody(updatedBody);

            
            var response = client.Execute(request);

            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 (OK)");

            var json = JObject.Parse(response.Content);
            Assert.That(json["msg"]?.ToString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStoriesShouldReturn200AndNonEmptyArray()
        {
            
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

         
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 (OK)");

            var json = JArray.Parse(response.Content);

            Assert.That(json.Count, Is.GreaterThan(0),
                "Expected at least one story in the response");
        }
        [Test, Order(4)]
        public void DeleteStory_ShouldReturn200AndSuccessMessage()
        {
            var request = new RestRequest($"/api/Story/Delete/{storyId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 (OK)");

            var json = JObject.Parse(response.Content);
            Assert.That(json["msg"]?.ToString(), Is.EqualTo("Deleted successfully!"));
        }
        [Test, Order(5)]
        public void CreateStoryWithoutRequiredFieldsShouldReturn400BadRequest()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);

            var incompleteBody = new
            {
                author = "John Doe",
                genre = "Mystery"
            };

            request.AddJsonBody(incompleteBody);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStorShouldReturn404NotFound()
        {
            var nonExistingId = 12345;
            var request = new RestRequest($"/api/Story/Edit/{nonExistingId}", Method.Put);
            var updatedBody = new
            {
                title = "Ghost Story",
                author = "Unknown",
                description = "This story should not exist",
                genre = "Horror"
            };
            request.AddJsonBody(updatedBody);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "Expected status code 404 (NotFound) for non-existing story");

            var json = JObject.Parse(response.Content);
            Assert.That(json["msg"]?.ToString(), Is.EqualTo("No spoilers..."));

        }
        
        [Test, Order(7)]
        public void DeleteNonExistingStoryShouldReturn400BadRequest()
        {
            var nonExistingId = 111111;
            var request = new RestRequest($"/api/Story/Delete/{nonExistingId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "Expected 400 BadRequest when deleting a non-existing story");

            var json = JObject.Parse(response.Content);
            Assert.That(json["msg"]?.ToString(), Is.EqualTo("Unable to delete this story spoiler!"));
        }



        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}