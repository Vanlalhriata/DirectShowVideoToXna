#DirectShowVideoToXna

This is an XNA 4.0 game component that generates and updates a Texture2D instance from a connected camera's feed using [DirectShow.NET](http://directshownet.sourceforge.net/). A buffer of grayscale bytes is also exposed for use in image processing

The exposed Texture2D is flipped vertically and has the red and blue channels swapped with respect to XNA colors. To achieve a decent framerate at higher resolutions, some tasks have been deferred to the GPU. These tasks are the swapping of red and blue channels, and horizontal and/or vertical flipping. This can be seen in the Demo game.

The exposed buffer of grayscale bytes is a mirror (flipped horizontally) of the camera feed. Again, this is to avoid CPU load while processing the camera feed. This flip has to be taken into account if this mirrored feed is not desired. The Demo game uses this mirror feed.

#####References:

 * http://www.alessandrocolla.com/xna-and-webcam-stream-as-background (Directshow video capture to XNA)
 * [WPFMediaKit](https://wpfmediakit.codeplex.com/) (Configurable resolutions and framerates)