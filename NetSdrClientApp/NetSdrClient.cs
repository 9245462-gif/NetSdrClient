﻿using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        private ITcpClient _tcpClient;
        private IUdpClient _udpClient;

        public bool IQStarted { get; set; }

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;

            _tcpClient.MessageReceived += _tcpClient_MessageReceived;
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();
            }

            var sampleRate = BitConverter.GetBytes((long)100000).Take(5).ToArray();
            var automaticFilterMode = BitConverter.GetBytes((ushort)0).ToArray();
            var adMode = new byte[] { 0x00, 0x03 };

            //Host pre setup
            var msgs = new List<byte[]>
            {
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.IQOutputDataSampleRate, sampleRate),
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, automaticFilterMode),
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ADModes, adMode),
            };

            foreach (var msg in msgs)
            {
                await _tcpClient.SendMessageAsync(msg);
            }
        }

        public void Disconect()
        {
            _tcpClient.Disconnect();
        }

        public async Task StartIQAsync()
        {
;           var iqDataMode = (byte)0x80;
            var start = (byte)0x02;
            var fifo16bitCaptureMode = (byte)0x01;
            var n = (byte)100;

            var args = new[] { iqDataMode, start, fifo16bitCaptureMode, n };

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);
            
            await _tcpClient.SendMessageAsync(msg);

            IQStarted = true;

            _ = _udpClient.StartListeningAsync();
        }

        public async Task StopIQAsync()
        {
            var stop = (byte)0x01;

            var args = new byte[] { 0, stop, 0, 0 };

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await _tcpClient.SendMessageAsync(msg);

            IQStarted = false;

            _udpClient.StopListening();
        }

        public async Task ChangeFrequencyAsync(long hz, int channel)
        {
            var channelArg = (byte)channel;
            var frequencyArg = BitConverter.GetBytes(hz).Take(5);
            var args = new[] { channelArg }.Concat(frequencyArg).ToArray();

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);

            await _tcpClient.SendMessageAsync(msg);
        }

        private void _udpClient_MessageReceived(object? sender, byte[] e)
        {

        }

        private void _tcpClient_MessageReceived(object? sender, byte[] e)
        {
            //TODO: handling responses, ACK, NAK and Unsolicited messages
            Console.Write("Response recieved: ");
            foreach (var item in e.Select(b => Convert.ToString(b, toBase: 16)))
            {
                Console.Write(item + " ");
            }
            Console.WriteLine();
        }
    }
}
