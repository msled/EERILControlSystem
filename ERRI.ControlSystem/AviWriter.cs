﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Video.VFW;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using EERIL.ControlSystem.Avt;

namespace EERIL.ControlSystem
{
    class AviWriter
    {
        private Stream videoStream;
        private AVIWriter videoWriter;
        public AviWriter(String path, int width = 0, int height = 0)
        {
            if (width == 0 || height == 0)
            {
                path = ".stream";
                videoStream = File.Create(path, 1392640);
            }
            else
            {
                path = ".avi";
                videoWriter = new AVIWriter();
                videoWriter.FrameRate = 15;
                videoWriter.Open(path, width, height);
            }
        }
        public void AddFrame(IFrame frame)
        {
            if (videoStream == null)
            {
                videoStream.Write(frame.Buffer, 0, frame.Buffer.Length);
            }
            else
            {
                videoWriter.AddFrame(frame.ToBitmap());
            }
        }
        public void Dispose()
        {
            if (videoStream == null)
            {
                videoStream.Close();
                videoStream.Dispose();
            }
            else
            {
                videoWriter.Close();
                videoWriter.Dispose();
            }
        }
    }
}