using HtmlAgilityPack;
using TagLib;
using File = System.IO.File;

var outputBasePath = args[0];
var outputBaseDir = new DirectoryInfo(outputBasePath);
if (!outputBaseDir.Exists)
{
    outputBaseDir.Create();
}

var baseUrl = args[1];

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(10)
};

await foreach (var track in GetTracks(baseUrl))
{
    if (track is null)
    {
        Console.WriteLine($"Error getting tracks from {baseUrl}");
        continue;
    }

    var dir = outputBaseDir;
    if (outputBaseDir.Name.Equals(track.Album.Name, StringComparison.OrdinalIgnoreCase) is false)
    {
        dir = new DirectoryInfo(
            Path.Combine(outputBaseDir.FullName, track.Album.Name)
        );

        if (!Directory.Exists(dir.FullName))
        {
            Directory.CreateDirectory(dir.FullName);
        }
    }

    var extension = track.Url.Split('.').LastOrDefault("mp3")?.ToLower();
    var trackFile = Path.Combine(dir.FullName, $"{track.Name}.{extension}");
    await DownloadTrack(track, trackFile);
    await CleanTrackMetadata(track, trackFile);
}

async IAsyncEnumerable<TrackInfo> GetTracks(string searchUrl)
{
    Console.WriteLine($"Getting tracks from {searchUrl}");
    var response = await httpClient.GetAsync(searchUrl);
    var content = await response.Content.ReadAsStringAsync();
    var htmlDocument = new HtmlDocument();
    htmlDocument.LoadHtml(content);

    var links = htmlDocument.DocumentNode.SelectNodes("//div[@class='list-group']//a[@class='list-group-item']");

    foreach (var link in links)
    {
        var url = link.GetAttributeValue("href", "");

        if (link.InnerText.Contains("Back to Parent Directory"))
        {
            continue;
        }

        if (url.Contains(".zip"))
        {
            continue;
        }

        if (url.Contains(".mp3.html")) // is a track
        {
            // detect album name from the title
            var albumName =
                url.Split('/')[^2]
                    .Replace("%20", " ")
                    .Replace(" (music.com.bd)", "")
                    .Replace(".mp3.html", "")
                    .Split('-')
                    .Last()
                    .Trim();

            var albumUrl = "https:" + url.Split('/').Take(url.Split('/').Length - 1).Aggregate((a, b) => a + "/" + b);
            var trackUrl = "https:" + url.Replace(" ", "%20").Replace(".html", "");
            var trackSize =
                link
                    .SelectNodes("span[@class='badge quote-list-badge']")
                    ?.FirstOrDefault()?.InnerText
                    .Trim()
                ?? "";

            var trackName =
                link.InnerText
                    .Replace("&nbsp;", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace(" (music.com.bd)", "")
                    .Replace(".mp3", "")
                    .Replace(trackSize, "")
                    .Trim()
                    .Split('-')
                    .Last()
                    .Trim();

            var track = new TrackInfo(new AlbumInfo(albumName, albumUrl), trackName, trackUrl);
            yield return track;
        }
        else if (!link.InnerText.Contains("Back to Parent Directory")) // is a album
        {
            // recursively get tracks from the album
            await foreach (var track in GetTracks(url))
            {
                yield return track;
            }
        }
    }
}

async Task<bool> DownloadTrack(TrackInfo track, string filePath)
{
    try
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Downloading track {track.Name} from {track.Url}");
            var trackResponse = await httpClient.GetAsync(track.Url);
            var trackContent = await trackResponse.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, trackContent);
        }
        else
        {
            Console.WriteLine($"Track {track.Name} already exists in {filePath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error downloading track {track.Name}: {ex.Message}");
        return false;
    }

    return true;
}

Task CleanTrackMetadata(TrackInfo track, string filePath)
{
    try
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return Task.CompletedTask;
        }

        using var file = TagLib.File.Create(filePath);

        // Remove album art
        file.Tag.Pictures = new IPicture[0];

        // Clean up bad metadata
        var badStrings = new[]
        {
            "music.com.bd",
            "www.music.com.bd",
            "https://music.com.bd",
            "http://music.com.bd",
            "Download from music.com.bd",
            "Bangla Song Download",
            "Bangla Music Download",
            "Bangla MP3 Download",
            ".mp3",
            "mp3",
            "www."
        };

        // Clean title
        if (!string.IsNullOrEmpty(file.Tag.Title))
        {
            foreach (var badString in badStrings)
            {
                file.Tag.Title = file.Tag.Title.Replace(badString, "", StringComparison.OrdinalIgnoreCase);
            }
            file.Tag.Title = file.Tag.Title.Trim();
        }

        // Clean album
        if (!string.IsNullOrEmpty(file.Tag.Album))
        {
            foreach (var badString in badStrings)
            {
                file.Tag.Album = file.Tag.Album.Replace(badString, "", StringComparison.OrdinalIgnoreCase);
            }
            file.Tag.Album = file.Tag.Album.Trim();
        }

        // Clean artist
        if (file.Tag.Performers.Length > 0)
        {
            foreach (var performer in file.Tag.Performers)
            {
                foreach (var badString in badStrings)
                {
                    if (performer.Contains(badString))
                    {
                        file.Tag.Performers = file.Tag.Performers.Where(p => p != performer).ToArray();
                    }
                }
            }
        }

        // Clean comment - be more aggressive with null safety
        try
        {
            if (!string.IsNullOrEmpty(file.Tag.Comment))
            {
                foreach (var badString in badStrings)
                {
                    if (!string.IsNullOrEmpty(file.Tag.Comment))
                    {
                        file.Tag.Comment = file.Tag.Comment.Replace(badString, "", StringComparison.OrdinalIgnoreCase);
                    }
                }
                file.Tag.Comment = file.Tag.Comment?.Trim() ?? "";

                // If comment only contains website info, clear it completely
                if (!string.IsNullOrEmpty(file.Tag.Comment) &&
                    (file.Tag.Comment.Contains("music.com.bd") ||
                     file.Tag.Comment.Contains("www.music.com.bd")))
                {
                    file.Tag.Comment = "";
                }
            }
        }
        catch (Exception commentEx)
        {
            Console.WriteLine($"Error cleaning comment for {track.Name}: {commentEx.Message}");
            // Just clear the comment if there's any error
            file.Tag.Comment = "";
        }

        // Remove website URLs from various fields
        file.Tag.Copyright = "";
        file.Tag.Publisher = "";

        // Set proper album and artist info
        file.Tag.Album = track.Album.Name;
        file.Tag.Title = track.Name;

        // Save the cleaned metadata
        file.Save();

        Console.WriteLine($"Cleaned metadata for: {track.Name}");
        return Task.CompletedTask;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error cleaning metadata for {track.Name}: {ex.Message}");
        return Task.CompletedTask;
    }
}

public record AlbumInfo(string Name, string Url);
public record TrackInfo(AlbumInfo Album, string Name, string Url);