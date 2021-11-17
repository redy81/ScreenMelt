using System;
using System.IO;

namespace ScreenMeltCmd
{
    internal static class Utils
    {
        const int ExitCodeParameterError = 4;

        public static int GetIntParameter( string[] argsBuffer, int idx )
        {
            if (idx >= argsBuffer.Length)
            {
                Console.WriteLine( "Not enough parameters specified. Error at '" + argsBuffer[idx - 1] + "'" );
                Environment.Exit( ExitCodeParameterError );
            }
            else
            {
                if (int.TryParse( argsBuffer[idx], out int value ))
                {
                    return value;
                }
                else
                {
                    Console.WriteLine( "Invalid integer specified. Error at '" + argsBuffer[idx - 1] + "'" );
                    Environment.Exit( ExitCodeParameterError );
                }
            }

            return -1;
        }

        public static string GetStringParameter( string[] argsBuffer, int idx )
        {
            if (idx >= argsBuffer.Length)
            {
                Console.WriteLine( "Not enough parameters specified. Error at '" + argsBuffer[idx - 1] + "'" );
                Environment.Exit( ExitCodeParameterError );
            }
            else
            {
                return argsBuffer[idx];
            }

            return "";
        }
    }

    class Program
    {
        static void ShowHelp()
        {
            Console.WriteLine( "" );
            Console.WriteLine( "Syntax:" );
            Console.WriteLine( "ScreenMeltCmd <StartImg> <EndImg> [options]" );
            Console.WriteLine( "" );
            Console.WriteLine( "StartImg        Path of the start image." );
            Console.WriteLine( "EndImg          Path of the end image." );
            Console.WriteLine( "" );
            Console.WriteLine( "Options:" );
            Console.WriteLine( "-o [videoOut]   Set the path of the output video file." );
            Console.WriteLine( "-a [type]       Select the melting algorithm type." );
            Console.WriteLine( "-sw [width]     Set the stripes' width." );
            Console.WriteLine( "-sd [disp]      Set the stripes' displacement per frame." );
            Console.WriteLine( "-sr [delay]     Set the max stripes' start delay (frames)." );
            Console.WriteLine( "-f [frames]     Set number of frames of the transition." );
            Console.WriteLine( "-r [seed]       Set random number generator seed." );
            Console.WriteLine( "-e              Force image dimensions to be multiple of 2 (remove one pixel)." );
            Console.WriteLine( "-nv             Disable console output." );
            Console.WriteLine( "-k              Keep temporary images." );
            Console.WriteLine( "-t [tempPath]   Set the path of temporary images." );
            Console.WriteLine( "-ffp [ffmPath]  Path to ffmpeg.exe (if not in the path variable)." );
            Console.WriteLine( "-ffo [options]  String with options for FFmpeg (override defaults)." );
            Console.WriteLine( "-ffo [options]  String with options for FFmpeg (override defaults)." );
            Console.WriteLine( "" );
        }

        static void Main( string[] args )
        {
            Console.WriteLine( "Screen Melting Command Line v1.0" );

            if (args.Length < 2)
            {
                Console.WriteLine( "Not enough parameter specified!" );
                ShowHelp();
                Environment.Exit( 1 );
                return;
            }

            if (!File.Exists( args[0] ))
            {
                Console.WriteLine( "Start image was not found!" );
                Environment.Exit( 2 );
                return;
            }

            if (!File.Exists( args[1] ))
            {
                Console.WriteLine( "End image was not found!" );
                Environment.Exit( 3 );
                return;
            }

            bool verboseOutput = true;
            string ffmpegPath = ".";
            var cmdOptions = new ScreenMeltingDLL.ScreenMeltingOptions
            {
                StartImagePath = args[0],
                EndImagePath = args[1]
            };

            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i].Trim().ToLower())
                {
                    case "-o":
                        cmdOptions.VideoOutputPath = Utils.GetStringParameter( args, i + 1 );
                        i++;
                        break;

                    case "-t":
                        cmdOptions.TempPath = Utils.GetStringParameter( args, i + 1 );
                        i++;
                        break;

                    case "-k":
                        cmdOptions.KeepTempImages = true;
                        break;

                    case "-e":
                        cmdOptions.ForceEvenDimensions = true;
                        break;

                    case "-sw":
                        cmdOptions.StripeWidth = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-a":
                        cmdOptions.AlgorithmType = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-sd":
                        cmdOptions.StripeDisplacementPerFrame = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-sr":
                        cmdOptions.MaxStripeRandomDisplacement = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-f":
                        cmdOptions.FrameCount = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-r":
                        cmdOptions.RandomSeed = Utils.GetIntParameter( args, i + 1 );
                        i++;
                        break;

                    case "-nv":
                        verboseOutput = false;
                        break;

                    case "-ffp":
                        ffmpegPath = Utils.GetStringParameter( args, i + 1 );
                        i++;
                        break;

                    case "-ffo":
                        cmdOptions.FfmpegOptions = Utils.GetStringParameter( args, i + 1 );
                        i++;
                        break;

                    case "-h":
                        ShowHelp();
                        Environment.Exit( 0 );
                        break;
                }
            }

            if (verboseOutput)
            {
                Console.WriteLine( "" );

                Console.WriteLine( "Start image: " + cmdOptions.StartImagePath );
                Console.WriteLine( "End image: " + cmdOptions.EndImagePath );

                if (cmdOptions.FrameCount > 0)
                {
                    Console.WriteLine( "Frames: " + cmdOptions.FrameCount );
                }

                if (!string.IsNullOrWhiteSpace( cmdOptions.VideoOutputPath ))
                {
                    Console.WriteLine( "Output file: " + cmdOptions.VideoOutputPath );
                }

                Console.WriteLine( "" );
            }

            try
            {
                ScreenMeltingDLL.ScreenMeltingApi.GenerateVideo( cmdOptions, verboseOutput, ffmpegPath );
            }
            catch (Exception ex)
            {
                Console.WriteLine( "Processing error:" );
                Console.WriteLine( ex.Message );
                Environment.Exit( 5 );
            }

            Environment.Exit( 0 );
        }
    }
}
