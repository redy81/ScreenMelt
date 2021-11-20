using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;


namespace ScreenMeltDLL
{
    public class ScreenMeltOptions
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

        public ScreenMeltOptions()
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

    public static class ScreenMeltApi
    {
        static readonly int[] DoomRndTable = new int[256]
        {
            11, 143, 192, 176, 138, 204, 198, 240, 230, 180, 220, 78, 172, 210, 239, 236,
            203, 31, 194, 27, 179, 152, 36, 103, 96, 90, 163, 207, 147, 107, 40, 50,
            105, 118, 86, 158, 213, 30, 108, 34, 199, 95, 61, 77, 106, 144, 42, 171,
            177, 67, 66, 187, 88, 245, 125, 37, 68, 142, 141, 13, 63, 14, 10, 218,
            253, 229, 51, 112, 2, 217, 104, 26, 15, 211, 87, 212, 29, 4, 80, 25,
            101, 92, 75, 124, 154, 53, 9, 44, 146, 43, 243, 38, 109, 6, 74, 182,
            121, 3, 161, 156, 114, 223, 195, 48, 254, 193, 32, 21, 246, 205, 18, 153,
            113, 58, 119, 84, 97, 132, 54, 242, 169, 174, 46, 117, 249, 123, 233, 136,
            91, 52, 209, 248, 173, 168, 185, 93, 20, 8, 111, 255, 102, 83, 76, 89,
            122, 16, 151, 64, 247, 134, 241, 49, 206, 237, 190, 85, 99, 150, 201, 1,
            57, 252, 228, 157, 183, 216, 162, 159, 219, 22, 238, 70, 197, 139, 222, 225,
            128, 116, 235, 131, 41, 165, 81, 19, 232, 79, 221, 214, 7, 0, 188, 186,
            191, 135, 12, 71, 231, 234, 140, 200, 56, 208, 100, 181, 148, 244, 55, 73,
            166, 60, 115, 226, 62, 130, 59, 170, 28, 215, 110, 127, 23, 145, 178, 227,
            39, 160, 24, 65, 155, 69, 137, 120, 129, 250, 202, 82, 224, 72, 167, 5,
            47, 45, 196, 184, 98, 17, 189, 164, 35, 33, 126, 94, 149, 175, 251, 133,        
        };

#pragma warning disable CA1416 // Validate platform compatibility
        public static void GenerateVideo( ScreenMeltOptions options, bool consoleOut = false, string ffmpegPath = "." )
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

            try
            {
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
                    Process ffmpgProcess = new();

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
            }
            finally // Like this the exception will go through, but the temp files will be erased anyway
            {
                if (!options.KeepTempImages)
                {
                    Log( "Erasing temp!" );

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
            }

            Log( "Processing completed!" );
        }
    }

    public class ScreenMeltApiWrapper
    {
        public void GenerateVideo( ScreenMeltOptions options, bool consoleOut = false, string ffmpegPath = "." )
        {
            ScreenMeltApi.GenerateVideo( options, consoleOut, ffmpegPath );
        }
    }
}
