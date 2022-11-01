*Idk why Github is rendering this without any breaklines. To see the correct formatting, just look at the raw file.

# Stream Timelapse Generator
 Generates timelapse videos from recorded art footage

To use:
>Make sure you have FFMpeg and FFprobe installed. See /Resources/FFMpeg Instructions.txt if you don't know how to do that.
>Drag your stream recordings + your desired music file (if you want one) into the exe

It will kick out a timelapse mp4 syncronized to whatever you have set in settings.js. Settings.js will be created with default values on the first execution if it doesn't exist in the folder.

There are a couple sample files in /Resources for if you want to quick test any of the settings. If TestingEnabled is enabled in settings.js, it will automatically use those /Resources files to create a sample video if you just run the exe instead of dragging anything into it.

Settings explanation:

 //In case I ever need to update this
   public int MergerVersion = 1;
 //If true, remove the big merge file when finished, otherwise leave it
   public Boolean DeleteMasterMergedFile = true;
 //The length of your compressed timelapse in hh:mm:ss, 2:20 is Twitter's limit
   public string CompressedFileVideoLength = "00:02:20";
 //Whether or not to put the song title on the video
   public Boolean WriteSongTitleOverlay = true;
 //Whether or not to ask for user input
   public Boolean PromptForSongTitleOverride = true;

 //Song title options
   public int TextOverlaySize = 24;
   public double TextOverlayBoxOpacity = 0.5;
   public int TextOverlayBoxSize = 5;
   public int TextOverlayDisplaySeconds = 3;
   public int TextOverlayFadeSeconds = 1;
   public int TextOverlayPlacementX = 25;
   public int TextOverlayPlacementY = 25;
   public string FontFileLocationOverride = "";

 //Leave an mp4 result
   public Boolean ExportResultAsMP4 = true;
 //Leave a webm result
   public Boolean ExportResultAsWEBM = false;

 //Prompt for base name input
   public Boolean PromptForBaseFilename = true;
 //Default basename for if prompting is disabled
   public string DefaulBaseFilename = "timelapse";

 //Don't close the window when finished
   public Boolean PromptOnCompletion = true;

 //Whether or not to run with testing example mode enabled
   public bool TestingEnabled = true;
