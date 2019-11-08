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

