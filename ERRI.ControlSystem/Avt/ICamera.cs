﻿using System;
using EERIL.ControlSystem.Communication;
using PvNET;

namespace EERIL.ControlSystem.Avt {
    public delegate void FrameReadyHandler(object sender, IFrame frame);
    public interface ICamera {
        event FrameReadyHandler FrameReady;
        string DisplayName { get; }
        uint InterfaceId { get; }
        uint Reference { get; }
        tInterface InterfaceType { get; }
        uint PartNumber { get; }
        uint PartVersion { get; }
        uint PermittedAccess { get; }
        string SerialString { get; }
        uint UniqueId { get; }
        float FrameRate { get; set; }
        ICommunicationsManager CommunicationManager { get; }
        DSP DSP { get; set; }
        ImageFormat ImageFormat { get; set; }
        Gain Gain { get; set; }
        Exposure Exposure { get; set; }
        WhiteBalance WhiteBalance { get; set; }
        void BeginCapture(tImageFormat fmt);
        void EndCapture();
        void Open();
        void AdjustPacketSize();
        void Close();
    }
}
