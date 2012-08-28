using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PvNET;

namespace EERIL.ControlSystem.Avt
{
    public class ImageFormat
    {
        public tImageFormat pixelformat;
        public uint bytesperframe;
        public uint height;
        public uint regionx;
        public uint regiony;
        public uint width;

        private const int FULL_BIT_DEPTH = 12; // ADC max res. = 12 for Prosilica GC1380

        public ImageFormat()
        {
            pixelformat = tImageFormat.eFmtMono8;
            bytesperframe = 1392640;
            height = 1024;
            regionx = 0;
            regiony = 0;
            width = 1360;
        }

        public uint GetDepth()
        {
            switch (pixelformat)
            {
                case tImageFormat.eFmtMono8:
                case tImageFormat.eFmtBayer8:
                case tImageFormat.eFmtRgb24:
                case tImageFormat.eFmtBgr24:
                case tImageFormat.eFmtYuv411:
                case tImageFormat.eFmtYuv422:
                case tImageFormat.eFmtYuv444:
                case tImageFormat.eFmtRgba32:
                case tImageFormat.eFmtBgra32:
                    return 8;

                case tImageFormat.eFmtMono12Packed:
                case tImageFormat.eFmtBayer12Packed:
                    return 12;

                case tImageFormat.eFmtMono16:
                case tImageFormat.eFmtBayer16:
                case tImageFormat.eFmtRgb48:
                    return FULL_BIT_DEPTH; // depends on hardware

                default:
                    return 0;
            }
        }

        public float GetBytesPerPixel() {
            switch (pixelformat)
            {
                case tImageFormat.eFmtMono8:
                case tImageFormat.eFmtBayer8:
                    return 1;

                case tImageFormat.eFmtYuv411:
                case tImageFormat.eFmtMono12Packed:
                case tImageFormat.eFmtBayer12Packed:
                    return 1.5F;

                case tImageFormat.eFmtMono16:
                case tImageFormat.eFmtBayer16:
                case tImageFormat.eFmtYuv422:
                    return 2;

                case tImageFormat.eFmtRgb24:
                case tImageFormat.eFmtBgr24:
                case tImageFormat.eFmtYuv444:
                    return 3;

                case tImageFormat.eFmtRgba32:
                case tImageFormat.eFmtBgra32:
                    return 4;

                case tImageFormat.eFmtRgb48:
                    return 6;

                default:
                   return 0;
            }
        }
    }
}
