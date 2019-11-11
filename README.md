# Secure Video Share

This tool was written because YouTube has a tendency to annoy me with copyright claims on my
own videos because it's AIs screen all videos and grab any background music comparing it with
existing databases. What's most anoying here is that the videos I post are unlisted, so I
clearly don't want people I didn't give a link to routinely looking at them.

# What it does

This tool will encode videos to HLS and use DRM encryption to make files at rest un-scannable.
The videos could be played using supplied player if a correct link is given in form of:

    https://my-host/path/video/index.html#decryption-key

The #decryption-key part is not sent to the server, therefore the provider hosting videos will
not be able to derive a descryption key and perform any mass-scan of the content. This allows
securely storing personal video content on internet without granting provider an access

# Build and Install

Currently tested on ubuntu 19.10 linux.

Required:
* .NET core 3.0
* ffmpeg 4.1 or higher

To build and install:

```
$ dotnet build svisha.csproj --configuration Release
$ cp -va bin/Release/netcoreapp3.0 ~/.local/share/secure_video_share
$ alias svisha=~/.local/share/secure_video_share/svisha
```

# Use

```
svisha encode ~/Videos/my_video.mp4 
```

This should produce the output ending with:

```
Encoding complete. Load using:
    cd /home/galets/Videos/svisha; http-server -p 8080 -a 127.0.0.1 -c 5
    firefox http://127.0.0.1:8080/3e9bd1703f4e4913ae5c4fa0dcc82da1/index.html#63a595efc8b44fddc3cb8fa54aac602b
```

The `http-server` command is just given so that you can test the result. The resulting video will be 
located in `~/Videos/svisha/3e9bd1703f4e4913ae5c4fa0dcc82da1`. This folder can be safely copied to an
external web server. In order to playback the key after a hash character must be supplied in browser
(e.g.: `#63a595efc8b44fddc3cb8fa54aac602b`). Without such key video will not play.

# Example

Sample video hosted on Backblaze bucket could be viewed at: 
https://f000.backblazeb2.com/file/mystaticweb/8beb6bb0a812404fa7359e0a94ee2653/index.html#b65d07f1e3739d32548b1e5c6cc716de

