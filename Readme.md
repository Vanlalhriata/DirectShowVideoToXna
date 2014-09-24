#DirectShowVideoToXna

This is an XNA 4.0 game component that generates and updates a Texture2D instance from a connected camera's feed using [DirectShow.NET](http://directshownet.sourceforge.net/).

To achieve a decent framerate at higher resolutions, some tasks like swapping red and blue channels have been deferred to the GPU. This can be seen in the Demo game.

#####References:

 * http://www.alessandrocolla.com/xna-and-webcam-stream-as-background (Directshow video capture to XNA)
 * [WPFMediaKit](https://wpfmediakit.codeplex.com/) (Configurable resolutions and framerates)