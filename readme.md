# YouTube Video Downloader

This is a **YouTube Video Downloader** built using C# and the **YoutubeExplode** library. It allows users to download videos from YouTube, including both audio and video, and merge them into a single file using **FFmpeg**.

## Features

- Download YouTube videos with the best available quality.
- Supports downloading video + audio as a single file.
- Handles separate audio and video streams and merges them using FFmpeg.
- Provides real-time progress updates.
- Logs download and merge activities.
- Unicode support for handling video titles.

## How It Works

1. **Run the tool:** Simply execute the provided `YoutubeDownloader.exe` file in the `downloader/` folder.
2. **Enter the YouTube video URL** when prompted.
3. The tool will automatically:
   - Retrieve video details and choose the best available quality.
   - Download the video and audio streams.
   - Merge video and audio if required.
   - Display a real-time progress bar.
   - Save the final video in the `videos/` folder.
   - Log the download activity in the `logs/` folder.
4. Once completed, you will see a success message and can find your video in the `videos/` directory.

## Example Output
```
🔗 Enter YouTube video URL: https://www.youtube.com/watch?v=exampleID
🎬 Downloading: Example Video Title
⏳ Downloading Video + Audio...
[██████████████████████████████] 100.0% ✅
🔄 Merging video and audio...
✅ Merge complete!
🎉 Downloaded Successfully
```

## File Structure
```
project-directory/
│── downlaoder/YoutubeDownloader.exe
│── logs/ (Contains log files)
│── videos/ (Stores downloaded videos)
```

## Notes
- If a video has separate audio and video streams, the tool will download both and merge them.
- If FFmpeg is missing, merging will not be possible.
- All download logs are stored for reference.

## License
This project is for **educational purposes only**. Downloading videos from YouTube may violate YouTube’s Terms of Service. Ensure you have permission to download any content before use.

## Author
**Harshit Patel**

For any inquiries, contact: [ironman.mak50@gmail.com](mailto:ironman.mak50@gmail.com)
