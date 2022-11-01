using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Stream_Video_Merger__Core_.Settings;

namespace Stream_Video_Merger__Core_
{
    class Program
    {
        public class settings
        {
            //In case I ever need to update this
            public int MergerVersion = 1;
            //If true, remove the big merge file when finished, otherwise leave it
            public Boolean DeleteMasterMergedFile = true;
            //The length of your compressed timelapse, 2:20 is Twitter's limit
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

            //Depricated
            //public double CompressedFileVideoLengthInSeconds { get { return TimeSpan.Parse(CompressedFileVideoLength).TotalSeconds; } }

            //Don't close the window when finished
            public Boolean PromptOnCompletion = true;

            //Whether or not to run with testing example mode enabled
            public bool TestingEnabled = true;
        }


        static void Main(string[] args)
        {
            try
            {
                bool testing = true;
                string exeDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                settings settingsObj = new settings();
                string settingsFileLocation = exeDirectory + @"\settings.json";

                    //Create settings file with defaults if it doesn't exist, otherwise load the existing one
                if (!File.Exists(settingsFileLocation))
                    File.WriteAllText(settingsFileLocation, JsonConvert.SerializeObject(settingsObj, Formatting.Indented));
                else
                    settingsObj = JsonConvert.DeserializeObject<settings>(File.ReadAllText(settingsFileLocation));

                testing = settingsObj.TestingEnabled;
                String textToOverlay = "";

                if (args.Length == 0)
                {
                    if (testing)
                    {
                        Console.WriteLine("No input files; testing example run enabled; using test files");
                        args = new[] { @".\Resources\mergetestfile1.mp4", @".\Resources\mergetestfile2.mp4", @".\Resources\mergetestfile5.mp4", @".\Resources\testAudioFile.mp3" };
                    }
                    else
                    {
                        Console.WriteLine("No input files; drag all stream files and audio files into the exe to merge");
                        Console.ReadKey();
                        return;
                    }
                }

                string workingFormat = "mp4";
                string resultFormat = "mp4";
                

                    //Calculate the length of the resulting video in seconds
                double lengthInSeconds = 0;
                string[] lengths = settingsObj.CompressedFileVideoLength.Split(":");
                lengthInSeconds = double.Parse(lengths[0]) * 60 * 60 + Int32.Parse(lengths[1]) * 60 + Int32.Parse(lengths[2]);

                List<string> allVideos = new List<string>();
                List<string> allMusic = new List<string>();

                Boolean useShortest = true; //Music is expected to be longer than the 2:20 twitter limit but doesn't have to be
                
                    //TODO change this to use FFMpeg.GetAudioCodecs();
                string[] audioFileTypes = { ".MP3", ".WAV", ".OGG", ".WMA",".FLAC" };    //Check if ffmpeg can even use crap like .wma
                double totalSecondsOfVideo = 0;

                    //Segregate the audio and video files
                foreach (var item in args)
                {
                    var mediaInfoo = FFProbe.Analyse(item);
                    if (!audioFileTypes.Contains(Path.GetExtension(item).ToUpper()))
                    {  //Is there a non-shit way to do this?
                        allVideos.Add(item);
                        totalSecondsOfVideo += mediaInfoo.Duration.TotalSeconds;
                    }
                    else
                        allMusic.Add(item);
                }
                string baseName = Path.GetFileNameWithoutExtension(allVideos[0]);
                string baseNameInput = "";

                    //Get the base name, or default it to the settingsObj default OR default it to the file base if all others are empty
                if (settingsObj.PromptForBaseFilename)
                {
                    outputColorized("Enter the a base name, or press enter to use [{" + baseName + "}]");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    baseNameInput = Console.ReadLine();
                    Console.ResetColor();
                }
                else
                {
                    if (settingsObj.DefaulBaseFilename != null && settingsObj.DefaulBaseFilename != "")
                        baseNameInput = settingsObj.DefaulBaseFilename;
                    else
                        baseNameInput = baseName;
                }

                if (baseNameInput != null && baseNameInput != "")
                    baseName = baseNameInput;

                string mergedVideo = @".\"+ baseName+"_MergedVideo." + workingFormat;
                string mutedVideo = @".\" + baseName + "_MutedVideo." + workingFormat;
                string compressedVideo = @".\" + baseName + "_CompressedVideo." + workingFormat;
                string textAddedVideo = @".\" + baseName + "_TextVideo." + workingFormat;
                string musicVideo = @".\" + baseName + "_MusicVideo." + workingFormat;
                string resultVideo = @".\" + baseName + "_ResultVideo." + resultFormat;

                if (totalSecondsOfVideo < lengthInSeconds)
                {
                    outputColorized("Total seconds of video is smaller than the designated setting ({" + totalSecondsOfVideo.ToString() + "}) vs ({" + lengthInSeconds.ToString() + "}), using lesser..");
                    lengthInSeconds = totalSecondsOfVideo;
                }

                    //Sort all videos alphabetically because why would you want a later part of the piece in the front?
                allVideos.Sort();

                    //Merge all videos into one big file
                Console.WriteLine("Merging");
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var item in allVideos)
                {
                    Console.WriteLine(item.ToString());
                }
                Console.ResetColor();
                FFMpeg.Join(mergedVideo,
                   allVideos.ToArray()
                );

                    //Mute the result
                Console.WriteLine("Removing existing audio");

                FFMpeg.Mute(mergedVideo, mutedVideo);
                var mergedVideoInfo = FFProbe.Analyse(mutedVideo);

                    //Calculate the total length of the result
                    //Expecting all framerates to be the same as the first vid
                var videoFramerate = mergedVideoInfo.PrimaryVideoStream.FrameRate;
                var totalFrames = mergedVideoInfo.PrimaryVideoStream.FrameRate * mergedVideoInfo.PrimaryVideoStream.Duration.TotalSeconds;

                outputColorized("Total frames: {" + totalFrames.ToString()+"}");

                    //Compress the resulting video to the calculated length
                outputColorized("Compressing to {" + lengthInSeconds + " seconds}");

                    //TODO make this account for other framerates
                FFMpegArguments
                    .FromFileInput(mutedVideo)
                    .OutputToFile(compressedVideo, true, options => options
                        //.WithCustomArgument("-filter:v setpts=("+lengthInSeconds.ToString()+"/"+ totalFrames .ToString()+ ")*N/TB -r " + totalFrames.ToString() + "/" + lengthInSeconds.ToString() + "")
                        //.WithCustomArgument("-filter:v setpts=(" + lengthInSeconds.ToString() + "/" + totalFrames.ToString() + ")*N/TB -r 30")
                        .WithCustomArgument("-filter:v setpts=(" + lengthInSeconds.ToString() + "/" + totalFrames.ToString() + ")*N/TB -r "+ videoFramerate.ToString())
                        )
                    .ProcessSynchronously();

                string vidToAddMusicTo = compressedVideo;

                    //Remove the result if it already exists
                deleteFileIfExists(resultVideo);

                    //Add the audio to the result if there is any
                if (allMusic.Count > 0)
                {
                    //Fug dis
                    /*Console.WriteLine("Merging audio");
                    foreach (var item in allMusic)
                    {
                        Console.WriteLine(item.ToString());
                    }
                    FFMpeg.Join(mergedAudio,
                       allMusic.ToArray()
                    );
                    var mergedAudioInfo = FFProbe.Analyse(mergedAudio);
                    if (mergedAudioInfo.Duration.TotalSeconds < lengthInSeconds)
                        useShortest = false;

                    FFMpeg.ReplaceAudio(workingVideo1, mergedAudio, resultVideo, useShortest);
                    File.Delete(mergedAudio);*/
                    string fugDis = allMusic[0];
                    outputColorized("Adding audio: {" + fugDis+"}");
                    var mergedAudioInfo = FFProbe.Analyse(fugDis);
                    if (mergedAudioInfo.Duration.TotalSeconds < lengthInSeconds)
                        useShortest = false;
                    FFMpeg.ReplaceAudio(vidToAddMusicTo, fugDis, musicVideo, useShortest);
                    //System.IO.File.Move(musicVideo, resultVideo);

                        //Determine text to write
                    if (settingsObj.WriteSongTitleOverlay)
                    {
                        FileInfo musicFileInfo = new FileInfo(fugDis);
                        string textToOverlayOverride = "";
                        textToOverlay = "Music - " + musicFileInfo.Name;
                        if (settingsObj.PromptForSongTitleOverride)
                        {
                            outputColorized("Enter the song title overlay, or press enter to use [{" + textToOverlay + "}]");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            textToOverlayOverride = Console.ReadLine();
                            Console.ResetColor();
                        }
                        if (textToOverlayOverride != null && textToOverlayOverride != "")
                            textToOverlay = textToOverlayOverride;
                    }
                }
                else
                {
                    Console.WriteLine("No audio to add");
                    System.IO.File.Move(vidToAddMusicTo, musicVideo);
                }

                if (textToOverlay != "")
                {
                    outputColorized("Writing '{" + textToOverlay + "}' onto vid...");
                    writeTextOntoVid(settingsObj, textToOverlay, musicVideo, textAddedVideo);
                    System.IO.File.Move(textAddedVideo, resultVideo, true);
                }
                else
                {
                    Console.WriteLine("No text to write");
                    System.IO.File.Move(musicVideo, resultVideo, true);
                }




                if (settingsObj.ExportResultAsWEBM)
                {
                    Console.WriteLine("Generating webm version");
                    FFMpegArguments
                    .FromFileInput(resultVideo)
                    .OutputToFile(resultVideo.Replace("mp4", "webm"), true)
                    .ProcessSynchronously();
                }

                Console.WriteLine("Removing work-files");

                if (!settingsObj.ExportResultAsMP4)
                    deleteFileIfExists(resultVideo);
                if (settingsObj.DeleteMasterMergedFile)
                    deleteFileIfExists(mergedVideo);

                deleteFileIfExists(mutedVideo);
                deleteFileIfExists(compressedVideo);
                deleteFileIfExists(textAddedVideo);
                deleteFileIfExists(musicVideo);

                FileInfo f = new FileInfo(resultVideo);
                string fullname = f.FullName;

                outputColorized("Result: {" + fullname+"}");
                outputColorized("{Done}", ConsoleColor.Green);

                if(settingsObj.PromptOnCompletion)
                    Console.ReadKey();

            }
            catch(Exception e)
            {
                Console.WriteLine("***********************************************");
                Console.WriteLine("Error encountered: " +e.Message);
                if(e.Message.Contains("ffprobe.exe") || e.Message.Contains("FFMpegCore"))
                    Console.WriteLine("FFMPEG may not be properly installed. Check that FFMPEG and FFPROBE are in your path.");
                else
                    Console.WriteLine("This may be your antivirus preventing the app from accessing the files, try adding an exception.");
                
                Console.WriteLine("***********************************************");
                Console.ReadKey();
            }
            return;
        }


        public static void writeTextOntoVid(settings settingsObj, string textToOverlay, string inputVideo, string outputVideo)
        {
            string exeDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string fontFileLoc = exeDirectory + @"./Resources/Calibri.ttf";
            int textOnScreenTime = settingsObj.TextOverlayDisplaySeconds + settingsObj.TextOverlayFadeSeconds;
            if (settingsObj.FontFileLocationOverride != "")
                fontFileLoc = settingsObj.FontFileLocationOverride;
            Console.WriteLine("Font location: " + fontFileLoc);
            FFMpegArguments
                    .FromFileInput(inputVideo)
                    .OutputToFile(outputVideo, true, options => options
                    //.WithCustomArgument("-filter:v setpts=(" + lengthInSeconds.ToString() + "/" + totalFrames.ToString() + ")*N/TB -r 30")
                    //.WithCustomArgument("-vf drawtext=\"fontfile=./Calibri.ttf: text='Stack Overflow': fontcolor=white: fontsize=24: box=1: boxcolor=black@0.5: boxborderw=5: x=5: y=5\" -codec:a copy")
                    //.WithCustomArgument("-vf drawtext=\"fontfile=./Calibri.ttf: text='Stack Overflow':enable='between(t,0,6)': fontcolor=white:alpha='if(lt(t,0),0,if(lt(t,0),(t-0)/0,if(lt(t,3),1,if(lt(t,4),(1-(t-3))/1,0))))': fontsize=32: shadowcolor=black:shadowx=3:shadowy=3: x=25: y=25\" -codec:a copy")
                    //.WithCustomArgument(" -filter_complex \"subtitles=your_subtitles_file.srt:force_style='Outline=5,OutlineColour=&H000000&'\" output")
                    //.WithCustomArgument("-vf drawtext=\"fontfile="+ textToOverlay + ": text='"+settingsObj.TextOverlayOverride+"': fontcolor=white: fontsize=" + settingsObj.TextOverlaySize.ToString()+": box=1: boxcolor=black@" +settingsObj.TextOverlayBoxOpacity.ToString()+": boxborderw=" +settingsObj.TextOverlayBoxSize.ToString()+": x=5: y=5\" -codec:a copy")

                    //shadowcolor=black:shadowx=3:shadowy=3:
                    .WithCustomArgument("-vf drawtext=\"fontfile='" + fontFileLoc + "': text ='" + textToOverlay + "':enable='between(t,0," + (textOnScreenTime + 1).ToString() + ")': fontcolor=white:alpha='if(lt(t,0),0,if(lt(t,0),(t-0)/0,if(lt(t," + settingsObj.TextOverlayDisplaySeconds.ToString() + ")," + settingsObj.TextOverlayFadeSeconds.ToString() + ",if(lt(t," + textOnScreenTime + "),(" + settingsObj.TextOverlayFadeSeconds.ToString() + "-(t-" + settingsObj.TextOverlayDisplaySeconds.ToString() + "))/" + settingsObj.TextOverlayFadeSeconds.ToString() + ",0))))':bordercolor=black:borderw=3: fontsize=32:  x="+settingsObj.TextOverlayPlacementX+ ": y=" + settingsObj.TextOverlayPlacementY + "\" -codec:a copy")
                    )
                    .ProcessSynchronously();

        }
        public static void deleteFileIfExists(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
        public static void mergeMultipleAudios(List<string> allMusic, string inputVideo, string outputVideo)
        {
            string commandy = "";
            string mapy = "";
            commandy += "-i " + inputVideo;
            mapy += " -map 0";
            //foreach (var music in allMusic)
            for (var i = 0; i < allMusic.Count; i++)
            {
                var music = allMusic[i];
                commandy += " -i " + music;
                mapy += " -map " + (i + 1).ToString() + ":a";
            }
            commandy += mapy + " -c:a mp3 ";
            FFMpegArguments
                    .FromFileInput(inputVideo)
                    .OutputToFile(outputVideo, true, options => options
                    .WithCustomArgument(@"-i .\test.webm -i .\1.wav -i .\2.wav -map 0:0 -map 0:1 -map 1:0 -map 2:0 -c:v copy -c:a copy")
                    )
                    .ProcessSynchronously();
        }

        public static settings loadSettings()
        {
            string exeDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string settingsFileLoc = exeDirectory + @"/settings.json";
            Console.WriteLine("Loading settings file from: " + settingsFileLoc);
            settings settingsObj = new settings();
            if (File.Exists(settingsFileLoc))
                using (StreamReader r = new StreamReader(settingsFileLoc))
                {
                    string json = r.ReadToEnd();
                    settingsObj = JsonConvert.DeserializeObject<settings>(json);
                }
            return settingsObj;
        }

        //OutputColorized("Hmm I want {this} colored");
        public static void outputColorized(string outputMessage, ConsoleColor color = ConsoleColor.Yellow)
        {
            //Cut the message up around {}'s
            string[] segments = Regex.Split(outputMessage, @"({[^\]]*})");

            //Output each segment and color it if it's surrounded by {}
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];

                if (segment.StartsWith("{") && segment.EndsWith("}"))
                {
                    Console.ForegroundColor = color;
                    segment = segment.Substring(1, segment.Length - 2);
                }
                Console.Write(segment);
                Console.ResetColor();
            }
            //Next line
            Console.WriteLine();
        }
    }
}
