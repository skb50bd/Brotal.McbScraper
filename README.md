# Brotal.McbScraper

A .NET console application for downloading and organizing music tracks from music.com.bd with automatic metadata cleaning.

## Overview

Brotal.McbScraper is a web scraper designed to download music tracks from music.com.bd and organize them into albums with clean metadata. The application automatically:

- Scrapes track listings from music.com.bd
- Downloads MP3 files to organized directories
- Cleans unwanted metadata (removes website URLs, promotional text)
- Organizes tracks by album in separate folders
- Preserves proper track and album information

## Prerequisites

- .NET 10.0 or later
- Internet connection for downloading tracks

## Installation

1. Clone or download this repository
2. Navigate to the project directory
3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
4. Build the application:
   ```bash
   dotnet build
   ```

## Usage

The application requires two command-line arguments:

```bash
dotnet run <output_directory> <music_url>
```

### Parameters

- `output_directory`: The directory where downloaded music will be saved
- `music_url`: The music.com.bd URL to scrape tracks from

### Examples

```bash
# Download tracks to a local Music folder
dotnet run "/home/user/Music" "https://music.com.bd/artist/some-artist"

# Download tracks to current directory
dotnet run "./Downloads" "https://music.com.bd/album/some-album"
```

## How It Works

### 1. URL Processing
The application accepts a music.com.bd URL and recursively processes:
- Album directories (navigates into subdirectories)
- Track files (downloads .mp3 files)

### 2. File Organization
- Creates album-specific directories under the output path
- Downloads tracks with proper filenames
- Skips already downloaded files

### 3. Metadata Cleaning
The application automatically removes unwanted metadata including:
- Website URLs (music.com.bd, www.music.com.bd)
- Promotional text ("Download from music.com.bd", "Bangla Song Download")
- File extensions (.mp3, mp3)
- Album art images
- Copyright and publisher information

### 4. Track Information
For each track, the application:
- Extracts track name from the URL and page content
- Determines album name from the directory structure
- Preserves clean track and album metadata

## Dependencies

- **HtmlAgilityPack** (1.11.49): For HTML parsing and web scraping
- **TagLibSharp** (2.3.0): For reading and writing audio file metadata

## Features

- **Recursive Scraping**: Automatically navigates through album directories
- **Duplicate Prevention**: Skips already downloaded files
- **Metadata Cleaning**: Removes unwanted promotional content
- **Organized Storage**: Creates album-based directory structure
- **Error Handling**: Continues processing even if individual tracks fail
- **Progress Feedback**: Console output shows download progress

## Output Structure

```
output_directory/
├── Album Name 1/
│   ├── Track 1.mp3
│   ├── Track 2.mp3
│   └── Track 3.mp3
├── Album Name 2/
│   ├── Song A.mp3
│   └── Song B.mp3
└── ...
```

## Error Handling

The application includes robust error handling:
- Continues processing if individual tracks fail to download
- Creates output directories if they don't exist
- Handles network timeouts (10-minute timeout per request)
- Logs errors to console for debugging

## Limitations

- Designed specifically for music.com.bd website structure
- Requires valid music.com.bd URLs
- Downloads are subject to the website's availability and rate limits
- Only processes MP3 files (skips ZIP archives)

## Troubleshooting

### Common Issues

1. **"Error getting tracks from URL"**
   - Verify the URL is accessible and points to a valid music.com.bd page
   - Check your internet connection

2. **"File not found" errors**
   - Ensure the output directory path is valid and writable
   - Check disk space availability

3. **Download failures**
   - Some tracks may fail due to network issues or server problems
   - The application will continue with other tracks

### Debug Tips

- Run with verbose output to see detailed progress
- Check console output for specific error messages
- Verify URLs work in a web browser before using them

## Legal Notice

This tool is for educational purposes only. Please respect copyright laws and the terms of service of music.com.bd. Only download content you have the right to access.

## License

This project is provided as-is for educational and personal use.
