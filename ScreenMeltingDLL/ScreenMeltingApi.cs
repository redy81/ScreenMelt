using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

//#pragma warning disable CA1416 // Validate platform compatibility

namespace ScreenMeltingDLL
{
    public class ScreenMeltingOptions
    {
        public string StartImagePath;
        public string EndImagePath;
        public string TempPath;

        public string VideoOutputPath;

        public int StripeWidth;
        public int StripeDisplacementPerFrame;
        public int FrameCount;

        public int MaxStripeRandomDisplacement;
        public int RandomSeed;

        public bool KeepTempImages;
        public string FfmpegOptions;

        public bool ForceEvenDimensions;

        public int AlgorithmType;

        public ScreenMeltingOptions()
        {
            StartImagePath = string.Empty;
            EndImagePath = string.Empty;
            VideoOutputPath = string.Empty;
            TempPath = null;
            StripeWidth = 5;
            StripeDisplacementPerFrame = 10;
            FrameCount = 0;
            MaxStripeRandomDisplacement = 15;
            RandomSeed = -1;
            KeepTempImages = false;
            FfmpegOptions = string.Empty;
            ForceEvenDimensions = false;
            AlgorithmType = 0;
        }

    }

    public static class ScreenMeltingApi
    {
        static readonly int[] DoomRndTable = new int[256]
        {
            0,   8, 109, 220, 222, 241, 149, 107,  75, 248, 254, 140,  16,  66 ,
            74,  21, 211,  47,  80, 242, 154,  27, 205, 128, 161,  89,  77,  36 ,
            95, 110,  85,  48, 212, 140, 211, 249,  22,  79, 200,  50,  28, 188 ,
            52, 140, 202, 120,  68, 145,  62,  70, 184, 190,  91, 197, 152, 224 ,
            149, 104,  25, 178, 252, 182, 202, 182, 141, 197,   4,  81, 181, 242 ,
            145,  42,  39, 227, 156, 198, 225, 193, 219,  93, 122, 175, 249,   0 ,
            175, 143,  70, 239,  46, 246, 163,  53, 163, 109, 168, 135,   2, 235 ,
            25,  92,  20, 145, 138,  77,  69, 166,  78, 176, 173, 212, 166, 113 ,
            94, 161,  41,  50, 239,  49, 111, 164,  70,  60,   2,  37, 171,  75 ,
            136, 156,  11,  56,  42, 146, 138, 229,  73, 146,  77,  61,  98, 196 ,
            135, 106,  63, 197, 195,  86,  96, 203, 113, 101, 170, 247, 181, 113 ,
            80, 250, 108,   7, 255, 237, 129, 226,  79, 107, 112, 166, 103, 241 ,
            24, 223, 239, 120, 198,  58,  60,  82, 128,   3, 184,  66, 143, 224 ,
            145, 224,  81, 206, 163,  45,  63,  90, 168, 114,  59,  33, 159,  95 ,
            28, 139, 123,  98, 125, 196,  15,  70, 194, 253,  54,  14, 109, 226 ,
            71,  17, 161,  93, 186,  87, 244, 138,  20,  52, 123, 251,  26,  36 ,
            17,  46,  52, 231, 232,  76,  31, 221,  84,  37, 216, 165, 212, 106 ,
            197, 242,  98,  43,  39, 175, 254, 145, 190,  84, 118, 222, 187, 136 ,
            120, 163, 236, 249
        };

#pragma warning disable CA1416 // Validate platform compatibility
        public static void GenerateVideo( ScreenMeltingOptions options, bool consoleOut = false, string ffmpegPath = "." )
        {
            void Log( string text )
            {
                if (consoleOut)
                {
                    Console.WriteLine( text );
                }
            }

            void DoomMelt( int[] startFrameArray, int maxRandom, Random rng )
            {
                int seed = rng.Next( 0, 255 );

                startFrameArray[0] = rng.Next( 0, maxRandom );

                for (int i = 1; i < startFrameArray.Length; i++)
                {
                    int next = (DoomRndTable[seed % 256] % 3) - 1;

                    startFrameArray[i] = startFrameArray[i - 1] + next;

                    if (startFrameArray[i] < 0)
                    {
                        startFrameArray[i] = 0;
                    }

                    if (startFrameArray[i] > maxRandom)
                    {
                        startFrameArray[i] = maxRandom;
                    }

                    seed++;
                }
            }

            string TempPath;
            int RandomMax;
            Random Rng;
            float StripeStepY;

            if (string.IsNullOrEmpty( options.TempPath ))
            {
                TempPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "ScreenMeltingTemp" );
            }
            else
            {
                TempPath = options.TempPath;
            }

            if (options.RandomSeed >= 0)
            {
                Rng = new Random( options.RandomSeed );
            }
            else
            {
                Rng = new Random();
            }

            Bitmap StartImage = new( options.StartImagePath );
            Bitmap EndImage = new( options.EndImagePath );

            if ((StartImage.Width != EndImage.Width) ||
                (StartImage.Height != EndImage.Height))
            {
                throw new Exception( "Images have different dimensions!" );
            }

            int WorkWidth;
            int WorkHeight;

            if (options.ForceEvenDimensions)
            {
                WorkWidth = StartImage.Width % 2 == 0 ? StartImage.Width : StartImage.Width - 1;
                WorkHeight = StartImage.Height % 2 == 0 ? StartImage.Height : StartImage.Height - 1;
            }
            else
            {
                WorkWidth = StartImage.Width;
                WorkHeight = StartImage.Height;
            }


            RandomMax = options.MaxStripeRandomDisplacement;

            Bitmap WorkingImage = new( WorkWidth, WorkHeight, PixelFormat.Format32bppArgb );

            WorkingImage.SetResolution( StartImage.HorizontalResolution, StartImage.VerticalResolution );

            Graphics Output = Graphics.FromImage( WorkingImage );
            Output.FillRectangle( Brushes.White, new Rectangle( 0, 0, StartImage.Width, StartImage.Height ) );

            Output.CompositingMode = CompositingMode.SourceCopy;

            Output.DrawImageUnscaled( EndImage, 0, 0 );

            Directory.CreateDirectory( TempPath );

            List<string> OutputFileList = new();

            int StripesWidth = options.StripeWidth;
            int StripeCount = (WorkWidth / StripesWidth) + 1;

            float[] StripePositionY = new float[StripeCount];
            int[] StartFrames = new int[StripeCount];

            Array.Fill( StripePositionY, 0 );

            switch (options.AlgorithmType)
            {
                case 0:
                {
                    DoomMelt( StartFrames, RandomMax, Rng );

                    int minValue = StartFrames.Min();

                    for (int i = 0; i < StartFrames.Length; i++)
                    {
                        StartFrames[i] = StartFrames[i] - minValue;
                    }
                }
                break;

                case 1:
                {
                    DoomMelt( StartFrames, RandomMax, Rng );

                    int minValue = StartFrames.Min();
                    int maxValue = StartFrames.Max();

                    double factor = (double)RandomMax / (maxValue - minValue);

                    for (int i = 0; i < StartFrames.Length; i++)
                    {
                        StartFrames[i] = (int)((StartFrames[i] - minValue) * factor);
                    }
                }
                break;

                case 2:
                {
                    for (int i = 0; i < StartFrames.Length; i++)
                    {
                        StartFrames[i] = Rng.Next( 0, RandomMax );
                    }
                }
                break;
            }

            if (options.FrameCount <= 0)
            {
                StripeStepY = options.StripeDisplacementPerFrame;
            }
            else
            {
                StripeStepY = (float)WorkHeight / (options.FrameCount - RandomMax);
            }

            if (StripeStepY < 0)
            {
                throw new Exception( "Invalid parameter combination! Frame count must be greater than max start delay." );
            }

            int CurrentFrame = 0;
            bool ProcessCompleted = false;

            while (!ProcessCompleted)
            {
                Log( "Processing frame " + CurrentFrame + "..." );

                ProcessCompleted = true;

                Output.DrawImageUnscaled( EndImage, 0, 0 );

                for (int stripe = 0; stripe < StripeCount; stripe++)
                {
                    if (CurrentFrame > StartFrames[stripe])
                    {
                        StripePositionY[stripe] += StripeStepY;
                    }

                    if (ProcessCompleted && StripePositionY[stripe] < WorkHeight)
                    {
                        ProcessCompleted = false;
                    }

                    Output.DrawImage( StartImage, stripe * StripesWidth, StripePositionY[stripe],
                                      new Rectangle( stripe * StripesWidth, 0, StripesWidth, (int)(WorkHeight - StripePositionY[stripe] + 1) ),
                                      GraphicsUnit.Pixel );
                }

                string OutputPath = Path.Combine( TempPath, "Frame" + CurrentFrame.ToString( "D4" ) + ".png" );

                WorkingImage.Save( OutputPath, ImageFormat.Png );
                OutputFileList.Add( OutputPath );

                CurrentFrame++;
            }

            if (!string.IsNullOrWhiteSpace( options.VideoOutputPath ))
            {
                // Generate video
                Process ffmpgProcess = new Process();

                string ffProcessArgs = "-y ";

                if (!consoleOut)
                {
                    ffProcessArgs += "-loglevel quiet ";
                }

                if (string.IsNullOrWhiteSpace( options.FfmpegOptions ))
                {
                    ffProcessArgs += string.Format( "-framerate {0} -i \"{1}\" -c:v libx264 -pix_fmt yuv420p \"{2}\"", 30, Path.Combine( TempPath, "Frame%04d.png" ), options.VideoOutputPath );
                }
                else
                {
                    ffProcessArgs += string.Format( "-i \"{0}\" {1} \"{2}\"", Path.Combine( TempPath, "Frame%04d.png" ), options.FfmpegOptions, options.VideoOutputPath );
                }

                ffmpgProcess.StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = ffmpegPath,
                    FileName = Path.Combine( ffmpegPath, "ffmpeg.exe" ),
                    Arguments = ffProcessArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                ffmpgProcess.Start();
                ffmpgProcess.WaitForExit();

                if (ffmpgProcess.ExitCode != 0)
                {

                }
            }

            if (!options.KeepTempImages)
            {
                foreach (var f in OutputFileList)
                {
                    try
                    {
                        File.Delete( f );
                    }
                    catch (Exception ex)
                    {
                        Log( "Cannot delete: " + f );
                        Log( ex.Message );
                    }
                }
            }

            Log( "Processing completed!" );
        }
    }

    public class ScreenMeltingApiWrapper
    {
        public void GenerateVideo( ScreenMeltingOptions options, bool consoleOut = false, string ffmpegPath = "." )
        {
            ScreenMeltingApi.GenerateVideo( options, consoleOut, ffmpegPath );
        }
    }
}
