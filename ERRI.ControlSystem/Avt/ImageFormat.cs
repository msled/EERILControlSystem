using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt {
	public enum ImageFormat : uint {
        Mono8           = 0,            // Monochrome, 8 bits
        Mono16          = 1,            // Monochrome, 16 bits, data is LSB aligned
        Bayer8          = 2,            // Bayer-color, 8 bits
        Bayer16         = 3,            // Bayer-color, 16 bits, data is LSB aligned
        Rgb24           = 4,            // RGB, 8 bits x 3
        Rgb48           = 5,            // RGB, 16 bits x 3, data is LSB aligned
        Yuv411          = 6,            // YUV 411
        Yuv422          = 7,            // YUV 422
        Yuv444          = 8,            // YUV 444
        Bgr24           = 9,            // BGR, 8 bits x 3
        Rgba32          = 10,           // RGBA, 8 bits x 4
        Bgra32          = 11,           // BGRA, 8 bits x 4
        Mono12Packed    = 12,           // Monochrome, 12 bits, 
        Bayer12Packed   = 13            // Bayer-color, 12 bits, packed 
	}
}
