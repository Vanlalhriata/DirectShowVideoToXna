using DirectShowLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace DirectShowVideoToXna
{
    /// <summary>
    /// A video capture component that uses DirectShow to generate Texture2Ds from a camera for use in XNA.
    /// This class also generates a grayscale byte array for image processing.
    /// References:
    /// - http://www.alessandrocolla.com/xna-and-webcam-stream-as-background/
    /// - VideoCapturePlayer from WPFMediaKit (for setting custom resolutions)
    /// </summary>
    public class VideoCaptureComponent : GameComponent, ISampleGrabberCB, IVideoCaptureService
    {

        #region Fields

        private const int DEVICE_ID = 0;

        private Texture2D textureRGBA;
        private byte[] bufferRGBA;

        private int pixelCount;

        private int width;
        private int height;
        private int fps;

        private IGraphBuilder graphBuilder;
        private IMediaControl mediaControl;
        private ICaptureGraphBuilder2 captureGraphBuilder;

        private ISampleGrabber sampleGrabber;

        private static readonly object lockObject = new object();
        private int hr;

        #endregion Fields

        #region Properties

        #region TextureRGBA

        /// <summary>
        /// Gets or sets the TextureRGBA property. This observable property 
        /// indicates a texture from DirectShow. This needs to be flipped both horozontally
        /// and vertically, and RB channels need to be swapped. This has been deferred to the GPU.
        /// </summary>
        public Texture2D TextureRGBA
        {
            get
            {
                if (textureRGBA.GraphicsDevice.Textures[0] == textureRGBA)
                    textureRGBA.GraphicsDevice.Textures[0] = null;

                lock(lockObject)
                {
                    textureRGBA.SetData(bufferRGBA);
                }

                return textureRGBA; 
            }
        }

        #endregion

        public byte[] BufferGray { get; private set; }

        #endregion Properties

        #region Initialization

        public VideoCaptureComponent(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IVideoCaptureService), this);
        }

        public override void Initialize()
        {
            width = Game.GraphicsDevice.Viewport.Width;
            height = Game.GraphicsDevice.Viewport.Height;
            fps = 2;

            pixelCount = width * height;

            textureRGBA = new Texture2D(Game.GraphicsDevice, width, height);
            bufferRGBA = new byte[pixelCount * 4];
            BufferGray = new byte[pixelCount];

            InitializeCapture();
            mediaControl.Run();

            base.Initialize();
        }

        private void InitializeCapture()
        {
            graphBuilder = (IGraphBuilder)new FilterGraph();
            mediaControl = (IMediaControl)graphBuilder;

            captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            hr = captureGraphBuilder.SetFiltergraph(graphBuilder);
            DsError.ThrowExceptionForHR(hr);

            IBaseFilter videoInput = GetVideoInputObject();
            if (null != videoInput)
            {
                SetConfigurations(videoInput);

                sampleGrabber = new SampleGrabber() as ISampleGrabber;
                hr = graphBuilder.AddFilter((IBaseFilter)sampleGrabber, "Render");
                DsError.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(videoInput, "Camera");
                DsError.ThrowExceptionForHR(hr);

                AMMediaType type = new AMMediaType() { majorType = MediaType.Video, subType = MediaSubType.ARGB32, formatType = FormatType.VideoInfo };
                hr = sampleGrabber.SetMediaType(type);
                DsError.ThrowExceptionForHR(hr);
                DsUtils.FreeAMMediaType(type);

                sampleGrabber.SetBufferSamples(false);
                sampleGrabber.SetOneShot(false);
                sampleGrabber.GetConnectedMediaType(new AMMediaType());

                sampleGrabber.SetCallback((ISampleGrabberCB)this, 1);
                hr = captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, videoInput, null, sampleGrabber as IBaseFilter);
                DsError.ThrowExceptionForHR(hr);

                Marshal.ReleaseComObject(videoInput);
            }
        }

        #endregion Initialization

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            Thread.Sleep(100); // Allow some time to process the cam buffer

            if (null != mediaControl)
                mediaControl.StopWhenReady();

            Marshal.ReleaseComObject(mediaControl);
            Marshal.ReleaseComObject(graphBuilder);
            Marshal.ReleaseComObject(captureGraphBuilder);
            Marshal.ReleaseComObject(sampleGrabber);
            captureGraphBuilder = null;
            graphBuilder = null;
            mediaControl = null;
            sampleGrabber = null;

            textureRGBA.Dispose();
            textureRGBA = null;

            base.Dispose(disposing);
        }

        #endregion Dispose

        #region ISampleGrabber implementation callbacks

        public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            lock (lockObject)
            {
                Marshal.Copy(pBuffer, bufferRGBA, 0, BufferLen);

                // Use this block for managed code
                //for (int i = BufferGray.Length - 1; i >= 0; i--)
                //{
                //    BufferGray[i] = Marshal.ReadByte(pBuffer, i * 4);
                //}

                unsafe
                {
                    byte* p = (byte*)pBuffer.ToPointer();

                    for (int i = pixelCount - 1; i >= 0; i--)
                    {
                        BufferGray[pixelCount - i - 1] = *(p + (i * 4));
                    }
                }
            }
            
            return 0;
        }

        public int SampleCB(double SampleTime, IMediaSample pSample)
        {
            return 0;
        }

        #endregion ISampleGrabber implementation callbacks

        #region Utilitary methods

        private IBaseFilter GetVideoInputObject()
        {
            object videoInputObject = null;
            IEnumMoniker classEnum;
            ICreateDevEnum devEnum = (ICreateDevEnum)new CreateDevEnum();
            hr = devEnum.CreateClassEnumerator(FilterCategory.VideoInputDevice, out classEnum, 0);
            DsError.ThrowExceptionForHR(hr);
            Marshal.ReleaseComObject(devEnum);

            if (null != classEnum)
            {
                IMoniker[] moniker = new IMoniker[1];
                if (classEnum.Next(moniker.Length, moniker, IntPtr.Zero) == DEVICE_ID)
                {
                    Guid iid = (typeof(IBaseFilter)).GUID;
                    moniker[0].BindToObject(null, null, ref iid, out videoInputObject);
                }

                Marshal.ReleaseComObject(moniker[0]);
                Marshal.ReleaseComObject(classEnum);
            }

            return (IBaseFilter)videoInputObject;
        }

        private void SetConfigurations(IBaseFilter videoInput)
        {
            // Get the stream's configuration interface
            object streamConfig;
            hr = captureGraphBuilder.FindInterface(PinCategory.Capture, MediaType.Video, videoInput, typeof(IAMStreamConfig).GUID, out streamConfig);
            DsError.ThrowExceptionForHR(hr);
            var videoStreamConfig = streamConfig as IAMStreamConfig;

            // If QueryInterface fails
            if (videoStreamConfig == null)
            {
                throw new Exception("Failed to get IAMStreamConfig");
            }

            // The media type of the video
            AMMediaType media;
            hr = videoStreamConfig.GetFormat(out media);
            DsError.ThrowExceptionForHR(hr);

            // Make the VIDEOINFOHEADER 'readable'
            var videoInfo = new VideoInfoHeader();
            Marshal.PtrToStructure(media.formatPtr, videoInfo);

            // Setup the VIDEOINFOHEADER with the parameters we want
            videoInfo.AvgTimePerFrame = fps;
            videoInfo.BmiHeader.Width = width;
            videoInfo.BmiHeader.Height = height;

            // Copy the data back to unmanaged memory and set the format
            Marshal.StructureToPtr(videoInfo, media.formatPtr, false);
            hr = videoStreamConfig.SetFormat(media);
            //DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(media);
        }

        // TODO : Remove from production code
        private DateTime last = DateTime.Now;
        private void DebugFps()
        {
            DateTime now = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("FPS: " + 1.0 / (now - last).TotalSeconds);
            last = now;
        }

        #endregion Utilitary methods

    }
}
