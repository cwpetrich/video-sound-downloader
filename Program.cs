using System.CommandLine;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class Program
{
    public static async Task Main(string[] args)
    {
        var youtubeClient = new YoutubeClient();
        var argument = new Argument<string>("Url", "This is the url of a video found on youtube that you want to stream.");
        var audioOption = new Option<bool>("--audio", "Download only the audio of the given url.");
        var videoOption = new Option<bool>("--video", "Download only the video of the given url.");
        var outputOption = new Option<string?>(new string[]{"-o","--output"}, () => null, "The path to the directory where the output should be sent.");
        var nameOption = new Option<string?>(new string[]{"-n","--name"}, () => null, "The name of the file.");

        var rootCommand = new RootCommand();
        rootCommand.AddArgument(argument);
        rootCommand.AddOption(audioOption);
        rootCommand.AddOption(videoOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(nameOption);

        rootCommand.SetHandler(async (argumentValue, audioValue, videoValue, outputValue, nameValue) =>
        {
            if (string.IsNullOrWhiteSpace(outputValue))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (audioValue)
                {
                    var path = Path.Join(userProfile, "Music");
                    Directory.SetCurrentDirectory(path);
                }
                else
                {
                    var path = Path.Join(userProfile, "Videos");
                    Directory.SetCurrentDirectory(path);
                }
            }
            else
            {
                Directory.SetCurrentDirectory(outputValue);
            }

            if (!audioValue && !videoValue)
            {
                await GetVideoWithAudio(youtubeClient, argumentValue, nameValue);
            }
            else
            {
                if (audioValue)
                {
                    await GetAudio(youtubeClient, argumentValue, nameValue);
                }

                if (videoValue)
                {
                    await GetVideo(youtubeClient, argumentValue, nameValue);
                }
            }

        }, argument, audioOption, videoOption, outputOption, nameOption);

        await rootCommand.InvokeAsync(args);
    }

    private static async Task GetVideoWithAudio(YoutubeClient youtubeClient, string url, string? name = null)
    {
        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);

        // Get highest quality muxed stream
        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

        await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, $"{name ?? "video"}.{streamInfo.Container}");
    }

    private static async Task GetVideo(YoutubeClient youtubeClient, string url, string? name = null)
    {
        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);

        // ...or highest quality MP4 video-only stream
        var streamInfo = streamManifest
            .GetVideoOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestVideoQuality();

        await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, $"{name ?? "video"}.{streamInfo.Container}");
    }

    private static async Task GetAudio(YoutubeClient youtubeClient, string url, string? name = null)
    {
        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);

        // ...or highest bitrate audio-only stream
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, $"{name ?? "audio"}.{streamInfo.Container}");
    }
}