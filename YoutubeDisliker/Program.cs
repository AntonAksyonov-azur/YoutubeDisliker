using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Util.Store;

namespace YoutubeDisliker
{
    internal class Program
    {
        private const string VideoId = "s3oR9OPANvw";
        // private const string VideoId = "gvQa3IEjAEQ";

        static void Main(string[] args)
        {
            try
            {
                Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task Run()
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YoutubeDisliker")
                );
            }

            var api = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YoutubeDisliker"
            });

            var listRequest = api.Videos.List(new Repeatable<string>(new[] { "snippet", "statistics" }));
            listRequest.Id = VideoId;

            var result = await listRequest.ExecuteAsync();
            if (result.Items != null)
            {
                var item = result.Items.First(); ;
                Console.WriteLine($"Title= {item.Snippet.Title}, Likes= {item.Statistics.LikeCount}, Dislikes= {item.Statistics.DislikeCount})");
                Console.WriteLine();

                var updateRequest = api.Videos.Update(item, new Repeatable<string>(new[] {"snippet", "statistics"}));
                var newDescription = UpdateDescription(item.Snippet.Description, item.Statistics.DislikeCount);
                item.Snippet.Description = newDescription;
                // item.Snippet.Description = "123";
                
                await updateRequest.ExecuteAsync();
            }
        }

        private static string UpdateDescription(string snippetDescription, ulong? dislikesCount)
        {
            var stringBuilder = new StringBuilder();
            var infoString = $"Dislikes count: {dislikesCount}";

            stringBuilder.AppendLine(infoString);
            stringBuilder.AppendLine(infoString);
            
            var strings = snippetDescription.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var firstString = strings[0];

            var startIndex = firstString.StartsWith("Dislikes count") ? 1 : 0;
            for (var i = startIndex; i < strings.Length; i++)
            {
                stringBuilder.AppendLine(strings[i]);
            }

            return stringBuilder.ToString();
        }
    }
}
