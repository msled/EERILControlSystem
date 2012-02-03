using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using PvNET;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace EERIL.ControlSystem.Avt
{
    public sealed class Frame : IFrame
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private readonly IntPtr framePointer;
        private readonly Camera camera;
        private bool disposed;
        private tFrame frame;
        private byte[] buffer;

        public byte[] Buffer
        {
            get
            {
                byte[] returnBuffer = new byte[frame.ImageBufferSize];
                Array.Copy(buffer, returnBuffer, Convert.ToInt32(frame.ImageBufferSize));
                return returnBuffer;
            }
        }

        public uint Size
        {
            get
            {
                return frame.ImageSize;
            }
        }

        public uint AncillarySize
        {
            get
            {
                return frame.AncillarySize;
            }
        }

        public uint Width
        {
            get
            {
                return frame.Width;
            }
        }

        public uint Height
        {
            get
            {
                return frame.Height;
            }
        }

        public uint RegionX
        {
            get
            {
                return frame.RegionX;
            }
        }

        public uint RegionY
        {
            get
            {
                return frame.RegionY;
            }
        }

        public tImageFormat Format
        {
            get
            {
                return (tImageFormat)frame.Format;
            }
        }

        public uint BitDepth
        {
            get
            {
                return frame.BitDepth;
            }
        }

        public tBayerPattern BayerPattern
        {
            get
            {
                return (tBayerPattern)frame.BayerPattern;
            }
        }

        public uint FrameCount
        {
            get
            {
                return frame.FrameCount;
            }
        }
        public long Timestamp
        {
            get
            {
                return (Convert.ToInt64(frame.TimestampHi) >> 32) + frame.TimestampLo;
            }
        }

        public Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap((int)frame.Width, (int)frame.Height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(new Point(0, 0), new Size((int)frame.Width, (int)frame.Height));
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            if (!Frame2Data(ref frame, ref buffer, ref data))
            {
                throw new PvException(tErr.eErrWrongType);
            }
            bitmap.UnlockBits(data);
            return bitmap;
        }

        public BitmapSource ToBitmapSource()
        {
            Bitmap bitmap = ToBitmap();
            IntPtr hbitmap = bitmap.GetHbitmap();
            BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(hbitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hbitmap);
            bitmap.Dispose();
            return source;
        }

        internal Frame(Camera camera, IntPtr framePointer, tFrame frame, byte[] buffer)
        {
            this.camera = camera;
            this.framePointer = framePointer;
            this.frame = frame;
            this.buffer = buffer;
        }

        ~Frame()
        {
            if (!disposed)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                camera.ReleaseFrame(framePointer);
                disposed = true;
            }
        }

        static void Yuv2Rgb(int y, int u, int v, ref int r, ref int g, ref int b)
        {
            // u and v are +-0.5
            u -= 128;
            v -= 128;

            // Conversion (clamped to 0..255)
            r = Math.Min(Math.Max(0, (int)(y + 1.370705 * v)), 255);
            g = Math.Min(Math.Max(0, (int)(y - 0.698001 * v - 0.337633 * u)), 255);
            b = Math.Min(Math.Max(0, (int)(y + 1.732446 * u)), 255);
        }

        // convert the raw data in the frame's buffer into the bitmap's data, this method doesn't support 
        // the following Pixel format: eFmtRgb48, eFmtYuv411 and eFmtYuv444
        static unsafe bool Frame2Data(ref tFrame frame, ref byte[] buffer, ref BitmapData data)
        {
            switch (frame.Format)
            {
                case tImageFormat.eFmtMono8:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;

                        while (lOffset < frame.ImageBufferSize)
                        {
                            lDst[lPos] = buffer[lOffset];
                            lDst[lPos + 1] = buffer[lOffset];
                            lDst[lPos + 2] = buffer[lOffset];

                            lOffset++;
                            lPos += 3;

                            // take care of the padding in the destination bitmap
                            if ((lOffset % frame.Width) == 0)
                                lPos += (UInt32)data.Stride - (frame.Width * 3);
                        }

                        return true;
                    }
                case tImageFormat.eFmtMono16:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;
                        byte bitshift = (byte)((int)frame.BitDepth - 8);
                        UInt16* lSrc = (UInt16*)frame.ImageBuffer;

                        while (lOffset < frame.Width * frame.Height)
                        {
                            lDst[lPos] = (byte)(lSrc[lOffset] >> bitshift);
                            lDst[lPos + 1] = lDst[lPos];
                            lDst[lPos + 2] = lDst[lPos];

                            lOffset++;
                            lPos += 3;

                            // take care of the padding in the destination bitmap
                            if ((lOffset % frame.Width) == 0)
                                lPos += (UInt32)data.Stride - (frame.Width * 3);
                        }

                        return true;
                    }
                case tImageFormat.eFmtBayer8:
                    {
                        UInt32 widthSize = frame.Width * 3;
                        GCHandle pFrame = GCHandle.Alloc(frame, GCHandleType.Pinned);
                        UInt32 remainder = (((widthSize + 3U) & ~3U) - widthSize);

                        // interpolate the colors
                        IntPtr pRed = (IntPtr)((byte*)data.Scan0 + 2);
                        IntPtr pGreen = (IntPtr)((byte*)data.Scan0 + 1);
                        IntPtr pBlue = (IntPtr)((byte*)data.Scan0);
                        Pv.ColorInterpolate(pFrame.AddrOfPinnedObject(), pRed, pGreen, pBlue, 2, remainder);

                        pFrame.Free();

                        return true;
                    }
                case tImageFormat.eFmtBayer16:
                    {
                        UInt32 widthSize = frame.Width * 3;
                        UInt32 lOffset = 0;
                        byte bitshift = (byte)((int)frame.BitDepth - 8);
                        UInt16* lSrc = (UInt16*)frame.ImageBuffer;
                        byte* lDst = (byte*)frame.ImageBuffer;
                        UInt32 remainder = (((widthSize + 3U) & ~3U) - widthSize);

                        frame.Format = tImageFormat.eFmtBayer8;

                        GCHandle pFrame = GCHandle.Alloc(frame, GCHandleType.Pinned);

                        // shift the pixel
                        while (lOffset < frame.Width * frame.Height)
                            lDst[lOffset] = (byte)(lSrc[lOffset++] >> bitshift);

                        // interpolate the colors
                        IntPtr pRed = (IntPtr)((byte*)data.Scan0 + 2);
                        IntPtr pGreen = (IntPtr)((byte*)data.Scan0 + 1);
                        IntPtr pBlue = (IntPtr)((byte*)data.Scan0);
                        Pv.ColorInterpolate(pFrame.AddrOfPinnedObject(), pRed, pGreen, pBlue, 2, remainder);

                        pFrame.Free();

                        return true;
                    }
                case tImageFormat.eFmtRgb24:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;

                        while (lOffset < frame.ImageBufferSize)
                        {
                            // copy the data
                            lDst[lPos] = buffer[lOffset + 2];
                            lDst[lPos + 1] = buffer[lOffset + 1];
                            lDst[lPos + 2] = buffer[lOffset];

                            lOffset += 3;
                            lPos += 3;
                            // take care of the padding in the destination bitmap
                            if ((lOffset % (frame.Width * 3)) == 0)
                                lPos += (UInt32)data.Stride - (frame.Width * 3);
                        }

                        return true;
                    }
                case tImageFormat.eFmtRgb48:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        UInt32 lLength = frame.ImageBufferSize / sizeof(UInt16);
                        UInt16* lSrc = (UInt16*)frame.ImageBuffer;
                        byte* lDst = (byte*)data.Scan0;
                        byte bitshift = (byte)((int)frame.BitDepth - 8);

                        while (lOffset < lLength)
                        {
                            // copy the data
                            lDst[lPos] = (byte)(lSrc[lOffset + 2] >> bitshift);
                            lDst[lPos + 1] = (byte)(lSrc[lOffset + 1] >> bitshift);
                            lDst[lPos + 2] = (byte)(lSrc[lOffset] >> bitshift);

                            lOffset += 3;
                            lPos += 3;

                            // take care of the padding in the destination bitmap
                            if ((lOffset % (frame.Width * 3)) == 0)
                                lPos += (UInt32)data.Stride - (frame.Width * 3);
                        }

                        return true;
                    }
                case tImageFormat.eFmtYuv411:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;
                        int y1, y2, y3, y4, u, v;
                        int r, g, b;

                        r = g = b = 0;

                        while (lOffset < frame.ImageBufferSize)
                        {
                            u = buffer[lOffset++];
                            y1 = buffer[lOffset++];
                            y2 = buffer[lOffset++];
                            v = buffer[lOffset++];
                            y3 = buffer[lOffset++];
                            y4 = buffer[lOffset++];

                            Yuv2Rgb(y1, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                            Yuv2Rgb(y2, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                            Yuv2Rgb(y3, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                            Yuv2Rgb(y4, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                        }

                        return true;
                    }
                case tImageFormat.eFmtYuv422:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;
                        int y1, y2, u, v;
                        int r, g, b;

                        r = g = b = 0;

                        while (lOffset < frame.ImageBufferSize)
                        {
                            u = buffer[lOffset++];
                            y1 = buffer[lOffset++];
                            v = buffer[lOffset++];
                            y2 = buffer[lOffset++];

                            Yuv2Rgb(y1, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                            Yuv2Rgb(y2, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                        }

                        return true;
                    }
                case tImageFormat.eFmtYuv444:
                    {
                        UInt32 lOffset = 0;
                        UInt32 lPos = 0;
                        byte* lDst = (byte*)data.Scan0;
                        int y1, y2, u, v;
                        int r, g, b;

                        r = g = b = 0;

                        while (lOffset < frame.ImageBufferSize)
                        {
                            u = buffer[lOffset++];
                            y1 = buffer[lOffset++];
                            v = buffer[lOffset++];
                            lOffset++;
                            y2 = buffer[lOffset++];
                            lOffset++;

                            Yuv2Rgb(y1, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                            Yuv2Rgb(y2, u, v, ref r, ref g, ref b);
                            lDst[lPos++] = (byte)b;
                            lDst[lPos++] = (byte)g;
                            lDst[lPos++] = (byte)r;
                        }

                        return true;
                    }
                default:
                    return false;
            }
        }
    }
}