# ScreenMelt
Doom-like screen melting generator

ScreenMelt is a simple DLL/CMD that allows to create a video with the Doom screen melting effect.

![License](https://img.shields.io/badge/license-MIT-red.svg)

### Requirements

ScreenMelt requires **FFmpeg** to be installed to generate the video. If `ffmpeg.exe` is not in the PATH, the executable folder can be specified.

If `ffmpeg.exe` is not installed, the frames can be generated in png format.

_Note_: at the moment, the code is using System.Drawing, so it works only on Windows.

### Usage (DLL)

The namespace `ScreenMeltDLL` contains two classes: `ScreenMeltOptions` and `ScreenMeltApi`.

`ScreenMeltOptions` contains the parameters that controls the generation process. `ScreenMeltApi` contains a static metod to start the process. Actually, a third class is present, `ScreenMeltApiWrapper`, which is included for the applications which cannot work with static classes/methods.

The class `ScreenMeltOptions` contains the following properties:
* `StartImagePath`: the path of the start image. Using the full path is recommanded.
* `EndImagePath`: the path of the end image. Using the full path is recommanded.
* `TempPath`: the path where to store the intermediate frames generated. If it's null or an empty string, a folder will be created in AppData\Local. This can be useful if the frames are needed.
* `VideoOutputPath`: the path where to save the output video. If this is null or an empty string, no video is generated (e.g. when using `KeepTempImages`).
* `StripeWidth`: the width in pixels of the vertical stripes the image will be divided into. Minimum: 1, maximum: image width. Narrower stripes will increase the computing time.
* `StripeDisplacementPerFrame`: how many pixels each stripe is shifted down at each frame. If `FrameCount`<=0, then this value is automatically calculated and this property is ignored.
* `FrameCount`: Number of frames the video should last. The first frame is the _Start Image_ the last frame is the _End Image_. If this value is set to 0 (or negative), the `StripeDisplacementPerFrame` is used and the video will take the amount of frames required to clear the image.
* `MaxStripeRandomDisplacement`: The maximum displacement amount of the melt effect (being random, it is not guaranteed this value is actually achieved). The displacement is in frames, so in pixels it is `MaxStripeRandomDisplacement*StripeDisplacementPerFrame`
* `RandomSeed`: The seed of the RNG used to create the melting effect. If 0 (or negative) a random seed is used.
* `KeepTempImages`: If this is set to `true`, the images of the frames are not deleted at the end of the process.
* `FfmpegOptions`: This string is added to the arguments of the FFmpeg call (and the default arguments are removed). The video output path and the temp image files are still included in the arguments.
* `AlgorithmType`: Select the type of algorithm to use. See below for a description.

The class `ScreenMeltApi` has only one static function:

`void GenerateVideo( ScreenMeltOptions options, bool consoleOut = false, string ffmpegPath = "." )`

* `options` is an object of `ScreenMeltOptions` type.
* `consoleOut` if set to true will show text on the console during the process.
* `ffmpegPath` is a string with the path to `ffmpeg.exe`.


The `GenerateVideo()` method is syncronous.

### Usage (CMD)

This is a command line wrapper for the DLL. All the options are the same as described before.

The help for the command line can be shown using the -h switch:

```
ScreenMeltCmd <StartImg> <EndImg> [options]

StartImg        Path of the start image.
EndImg          Path of the end image.

Options:
-o [videoOut]   Set the path of the output video file.
-a [type]       Select the melting algorithm type.
-sw [width]     Set the stripes' width.
-sd [disp]      Set the stripes' displacement per frame.
-sr [delay]     Set the max stripes' start delay (frames).
-f [frames]     Set number of frames of the transition.
-r [seed]       Set random number generator seed.
-e              Force image dimensions to be multiple of 2 (remove one pixel).
-nv             Disable console output.
-k              Keep temporary images.
-t [tempPath]   Set the path of temporary images.
-ffp [ffmPath]  Path to ffmpeg.exe (if not in the path variable).
-ffo [options]  String with options for FFmpeg (override defaults).
```

### Algorithms
The following algoritms are implemented:
* Type-0: original Doom melting algortihm. The maximum vertical displacement is `MaxStripeRandomDisplacement`.
* Type-1: original Doom melting algortihm stretched to fill the `MaxStripeRandomDisplacement`.
* Type-2: random displacement; each stripe is displaced Random( 0, `MaxStripeRandomDisplacement`).

### Notes

* Default FFmpeg options are: `-framerate 30 -c:v libx264 -pix_fmt yuv420p`. These are not included when using the override option.
* Some video codecs might require an image size multiple of 2.
* The dimensions of start image and end image should be the same. An exception is thrown if this is not true.



#### Donation

If you find this software useful, or it helped you to save time, you can offer me a coffee. :smile:

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate/?hosted_button_id=VNMV7XY9J5HBG)

Bitcoin: bc1q4379vq6jfg8swgqajyaetm02fyk2mwj0mwy8wj

Bitcoin: 1DbK9AoXxMRYYENwsoDEzqyxppo1cMUQUN

Ethereum: 0x381C29dE5781EEa0182568146a1B2c32205DF85B

Doge: DGbL57EA844QzgqRc1Z9kXLJKj261J138i
