﻿///<author> Sathvik Alugonda </author>
///<summary> This file contains the Screenshot Class that implements the screenshot functionality. ///</summary>
///<reference> https://github.com/0x2E757/ScreenCapturer ///</reference>
///<reference> https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/Direct3D11.1/ScreenCapture/Program.cs ///</reference>

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;


namespace MessangerScreenshare.Client
{
    /// <summary>
    /// Class contains the necessary functions for taking the screenshot of the current screen.
    /// </summary>
    public class Screenshot
    {
        private static readonly object _lock = new();
        private static Screenshot? instance;
        public Boolean CaptureActive { get; private set; }
        private Factory1? _factory1;
        private Adapter1? _adapter1;
        private Device? _device;
        private Output? _output;
        private Output1? _output1;
        private Int32 _width;
        private Int32 _height;
        private Rectangle _bounds;
        private Texture2DDescription _texture2DDescription;
        private Texture2D? _texture2D;
        private OutputDuplication? _outputDuplication;
        private Bitmap? _bitmap;

        private Int32 MakeScreenshot_LastDisplayIndexValue;
        private Int32 MakeScreenshot_LastAdapterIndexValue;

        protected Screenshot()
        {
            CaptureActive = false;
            InitializeVariables(0, 0, true);
        }

        public static Screenshot Instance()
        {
            lock (_lock)
            {
                if (instance == null)
                {
                    instance = new Screenshot();
                }
                return instance;
            }
        }

        /// Core function of class for taking the screenshot. Uses SharpDX for faster image capture.
        /// </summary>
        /// <param name="displayIndex">Index for the display which is to be captured. Defaults to 0 (Primary Display)</param>
        /// <param name="adapterIndex">Index for the display card to be used. Defaults to 0 (Primary graphics card)</param>
        /// <param name="maxTimeout">Timeout to get duplicated frame</param>
        /// <returns>The bitmap image for the screenshot</returns>
        public Bitmap MakeScreenshot( Int32 displayIndex = 0 , Int32 adapterIndex = 0 , Int32 maxTimeout = 5000 )
        {
            InitializeVariables( displayIndex , adapterIndex );

            using (Resource screenResource)
            {
                // acquire the next frame and directly convert it into a bitmap
                if (_outputDuplication.TryAcquireNextFrame( maxTimeout , out var frameInfo , out screenResource ) != Result.Ok)
                    return null;

                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                {
                    _device.ImmediateContext.CopyResource( screenTexture2D , _texture2D );

                    DataBox dataBox = _device.ImmediateContext.MapSubresource( _texture2D , 0 , MapMode.Read , MapFlags.None );

                    Bitmap screenshot = new Bitmap( _width , _height , PixelFormat.Format32bppRgb );
                    BitmapData bitmapData = screenshot.LockBits( _bounds , ImageLockMode.WriteOnly , screenshot.PixelFormat );

                    IntPtr dataBoxPointer = dataBox.DataPointer;
                    IntPtr bitmapDataPointer = bitmapData.Scan0;

                    for (Int32 y = 0; y < _height; y++)
                    {
                        Utilities.CopyMemory( bitmapDataPointer , dataBoxPointer , _width * 4 );
                        dataBoxPointer = IntPtr.Add( dataBoxPointer , dataBox.RowPitch );
                        bitmapDataPointer = IntPtr.Add( bitmapDataPointer , bitmapData.Stride );
                    }

                    screenshot.UnlockBits( bitmapData );
                    _device.ImmediateContext.UnmapSubresource( _texture2D , 0 );

                    // release the acquired frame
                    _outputDuplication.ReleaseFrame();
                    return screenshot;
                }
            }

            /// <summary>
            /// Initializes the members of the class.
            /// </summary>
            /// <param name="displayIndex">Index for the display which is to be captured. Defaults to 0 (Primary Display)</param>
            /// <param name="adapterIndex">Index for the display card to be used. Defaults to 0 (Primary graphics card)</param>
            /// <param name="forcedInitialization"></param>
            private void InitializeVariables(Int32 displayIndex, Int32 adapterIndex, Boolean forcedInitialization = false)
        {
            Boolean displayIndexChanged = MakeScreenshot_LastDisplayIndexValue != displayIndex;
            Boolean adapterIndexChanged = MakeScreenshot_LastAdapterIndexValue != adapterIndex;

            // reset all values in case of change in display, adapter or forced init.
            if (displayIndexChanged || adapterIndexChanged || forcedInitialization)
            {
                DisposeVariables();
                _factory1 = new Factory1();
                _adapter1 = _factory1.GetAdapter1(adapterIndex);
                _device = new Device(_adapter1);
                _output = _adapter1.GetOutput(displayIndex);
                _output1 = _output.QueryInterface<Output1>();
                _width = _output1.Description.DesktopBounds.Right - _output1.Description.DesktopBounds.Left;
                _height = _output1.Description.DesktopBounds.Bottom - _output1.Description.DesktopBounds.Top;
                _bounds = new Rectangle(Point.Empty, new Size(_width, _height));
                _texture2DDescription = new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = _width,
                    Height = _height,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging
                };
                _texture2D = new Texture2D(_device, _texture2DDescription);
                _outputDuplication = _output1.DuplicateOutput(_device);
                _outputDuplication.TryAcquireNextFrame(1000, out _, out _);
                _outputDuplication.ReleaseFrame();
                _bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppRgb);
                MakeScreenshot_LastAdapterIndexValue = adapterIndex;
                MakeScreenshot_LastDisplayIndexValue = displayIndex;
            }
        }

        /// <summary>
        /// Disposes the class memebers to avoid memory hogging.
        /// </summary>
        public void DisposeVariables()
        {
            _bitmap?.Dispose();
            _outputDuplication?.Dispose();
            _texture2D?.Dispose();
            _output1?.Dispose();
            _output?.Dispose();
            _device?.Dispose();
            _adapter1?.Dispose();
            _factory1?.Dispose();
            MakeScreenshot_LastAdapterIndexValue = -1;
            MakeScreenshot_LastDisplayIndexValue = -1;
        }
    }


}
