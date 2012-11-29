using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using PvNET;

namespace EERIL.ControlSystem.Avt
{
    internal class CameraSerial
    {
        // Camera registers dealing with Serial IO
        private const int REG_SIO_INQUIRY = 0x16000;
        private const int REG_SIO_MODE_INQUIRY = 0x16100;
        private const int REG_SIO_MODE = 0x16104;
        private const int REG_SIO_TX_INQUIRY = 0x16120;
        private const int REG_SIO_TX_STATUS = 0x16124;
        private const int REG_SIO_TX_CONTROL = 0x16128;
        private const int REG_SIO_TX_LENGTH = 0x1612C;
        private const int REG_SIO_RX_INQUIRY = 0x16140;
        private const int REG_SIO_RX_STATUS = 0x16144;
        private const int REG_SIO_RX_CONTROL = 0x16148;
        private const int REG_SIO_RX_LENGTH = 0x1614C;
        private const int REG_SIO_TX_BUFFER = 0x16400;
        private const int REG_SIO_RX_BUFFER = 0x16800;

        static readonly uint[] RegSioRxLengthAddress = new uint[] { REG_SIO_RX_LENGTH };

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe bool FWriteMem(uint camera, uint address, byte[] buffer, uint length)
        {
            uint numRegs = (length + 3) / 4;
            uint[] pAddressArray = new uint[numRegs];
            uint[] pDataArray = new uint[numRegs];
            uint written = 0;


            //
            // We want to write an array of bytes from the camera.  To do this, we
            // write sequential registers with the data array.  The register MSB
            // is the first byte of the array.
            //

            // 1.  Generate write addresses, and convert from byte array to MSB-packed
            // registers.
            fixed (byte* bufferPointer = buffer)
            {
                byte* incrementablePointer = bufferPointer;
                for (uint i = 0; i < numRegs; i++)
                {
                    pAddressArray[i] = address + (i * 4);

                    pDataArray[i] = (uint)*(incrementablePointer++) << 24;
                    pDataArray[i] |= (uint)*(incrementablePointer++) << 16;
                    pDataArray[i] |= (uint)*(incrementablePointer++) << 8;
                    pDataArray[i] |= *(incrementablePointer++);
                }

                // 2.  Execute write.
                tErr error = (tErr)Pv.RegisterWrite(camera, numRegs, pAddressArray, pDataArray, ref written);
                if (error != tErr.eErrSuccess)
                    throw new PvException(error);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void FReadMem(uint camera, uint address, byte[] buffer, uint length)
        {
            uint numRegs = (length + 3) / 4;
            uint[] pAddressArray = new uint[numRegs];
            uint[] pDataArray = new uint[numRegs];
            uint read = 0;


            //
            // We want to read an array of bytes from the camera.  To do this, we
            // read sequential registers which contain the data array.  The register
            // MSB is the first byte of the array.
            //

            // 1.  Generate read addresses
            for (uint i = 0; i < numRegs; i++)
                pAddressArray[i] = address + (i * 4);

            // 2.  Execute read.
            tErr error = (tErr)Pv.RegisterRead(camera, numRegs, pAddressArray, pDataArray, ref read);
            if (error != tErr.eErrSuccess)
                throw new PvException(error);

            uint data = 0;

            // 3.  Convert from MSB-packed registers to byte array
            for (uint i = 0; i < length; i++)
            {
                if (i % 4 == 0)
                    data = pDataArray[i / 4];

                buffer[i] = Convert.ToByte((data >> 24) & 0xFF);
                data <<= 8;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public CameraSerial(uint camera)
        {
            uint[] regAddresses = new uint[4];
            uint[] regValues = new uint[4];


            regAddresses[0] = REG_SIO_MODE;
            regValues[0] = 0x00000C09; // 9600, N, 8, 1

            regAddresses[1] = REG_SIO_TX_CONTROL;
            regValues[1] = 3; // Reset & enable transmitter

            regAddresses[2] = REG_SIO_RX_CONTROL;
            regValues[2] = 3; // Reset & enable receiver

            regAddresses[3] = REG_SIO_RX_STATUS;
            regValues[3] = 0xFFFFFFFF; // Clear status bits
            uint written = 0;
            tErr error = (tErr)Pv.RegisterWrite(camera, 4, regAddresses, regValues, ref written);
            if (error != tErr.eErrSuccess)
                throw new PvException(error);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool WriteBytesToSerialIo(uint camera, byte[] buffer, uint length)
        {
            uint[] value = new uint[2];
            uint[] addressData = new uint[] { REG_SIO_TX_STATUS };
            uint read = 0;
            tErr error;

            // Wait for transmitter ready.
            do
            {
                error = (tErr)Pv.RegisterRead(camera, 1, addressData, value, ref read);
                //if (error != tErr.eErrSuccess)
                  //  throw new PvException(error);
            } while (value[0] == 0U); // Waiting for transmitter-ready bit

            // Write the buffer.
            if (!FWriteMem(camera, REG_SIO_TX_BUFFER, buffer, length))
                return false;

            // Write the buffer length.  This triggers transmission.
            value = new[] { length };
            addressData = new uint[] { REG_SIO_TX_LENGTH };
            uint written = 0;
            error = (tErr)Pv.RegisterWrite(camera, 1, addressData, value, ref written);
            if (error != tErr.eErrSuccess)
                throw new PvException(error);

            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ReadBytesFromSerialIo(uint camera, byte[] buffer, uint bufferLength,
                                                        ref uint receiveLength)
        {
            uint[] lengthData = new uint[1];
            uint read = 0, written = 0;

            // How many characters to read?
            tErr error = (tErr)Pv.RegisterRead(camera, 1, RegSioRxLengthAddress, lengthData, ref read);
            if (error != tErr.eErrSuccess)
                throw new PvException(error);

            // It must fit in the user's buffer.
            uint dataLength = lengthData[0]; 
            if (dataLength > bufferLength)
                dataLength = bufferLength;

            if (dataLength > 0)
            {
                // Read the data.
                FReadMem(camera, REG_SIO_RX_BUFFER, buffer, dataLength);

                // Decrement the camera's read index.
                error = (tErr)Pv.RegisterWrite(camera, 1, RegSioRxLengthAddress, lengthData, ref written);
                if (error != tErr.eErrSuccess)
                    throw new PvException(error);
            }

            receiveLength = dataLength;

            return true;
        }
    }
}