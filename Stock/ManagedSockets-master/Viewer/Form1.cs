﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sockets;
using Sockets.EventArgs;

namespace Viewer {
    public partial class Form1 : Form {
        private ClientSocket _mySock;
        private byte[] _receiveBuffer = new byte[0];
        public Form1() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            _mySock = new ClientSocket(textBox1.Text, 11111);
            _mySock.Connected += MySockOnConnected;
            _mySock.Disconnected += MySockOnDisconnected;
            _mySock.DataReceived += MySockOnDataReceived;
            _mySock.Connect();
        }

        private void MySockOnDataReceived(DataReceivedArgs args) {
            lock (_receiveBuffer) {
                if (_receiveBuffer.Length == 0) {
                    _receiveBuffer = args.Data;
                    return;
                }

                var tempBuff = new byte[_receiveBuffer.Length + args.Data.Length];
                Buffer.BlockCopy(_receiveBuffer, 0, tempBuff, 0, _receiveBuffer.Length);
                Buffer.BlockCopy(args.Data, 0, tempBuff, _receiveBuffer.Length, args.Data.Length);
                _receiveBuffer = tempBuff;
            }
        }

        private void ParseLoop() {
            WriteToBox("Parse loop started");
            while (_mySock.IsConnected) {
                if (_receiveBuffer.Length == 0) {
                    Thread.Sleep(1);
                    continue;
                }

                List<string> outputs;

                lock (_receiveBuffer) {
                    string asAscii = Encoding.ASCII.GetString(_receiveBuffer);

                    if (!asAscii.Contains("\r\n")) {
                        continue;
                    }

                    outputs = asAscii.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (!asAscii.EndsWith("\r\n")) {
                        // -- Last message was incomplete
                        _receiveBuffer = Encoding.ASCII.GetBytes(outputs[outputs.Count - 1]);
                            // -- Place what was there back onto the stack..
                        outputs.RemoveAt(outputs.Count - 1);
                    }
                    else {
                        _receiveBuffer = new byte[0];
                    }
                } // -- We're done handling all this data. we can let the socket use it again :)

                foreach (string output in outputs) {
                    WriteToBox(output);

                    if (output.StartsWith("PING")) {
                        var id = output.Substring(output.IndexOf(":"));
                        _mySock.Send(Encoding.ASCII.GetBytes("PONG " + id));
                        WriteToBox("Ponged " + id);
                    }

                    if (output.Contains("451")) {
                        _mySock.Send(Encoding.ASCII.GetBytes("NICK u24something\r\n"));
                        _mySock.Send(Encoding.ASCII.GetBytes("USER u24something u24something bla :u24something\r\n"));
                        _mySock.Send(Encoding.ASCII.GetBytes("MODE u24something +B-x\r\n"));
                    }
                }
            }

            WriteToBox("Parse loop stopped");
        }

        private void MySockOnDisconnected(SocketDisconnectedArgs args) {
            WriteToBox(" **** Disconnected ! **** ");
        }

        private void MySockOnConnected(SocketConnectedArgs args) {
            WriteToBox("Connected to Server!");

            _mySock.Send(Encoding.ASCII.GetBytes("NICK u24something\r\n"));
            _mySock.Send(Encoding.ASCII.GetBytes("USER u24something u24something bla :u24something\r\n"));
            _mySock.Send(Encoding.ASCII.GetBytes("MODE u24something +B-x\r\n"));
            Task.Run(() => ParseLoop());
        }

        public delegate void StringArgs(string msg);

        private void WriteToBox(string message) {
            if (InvokeRequired) {
                Invoke(new StringArgs(WriteToBox), message);
                return;
            }

            richTextBox1.Text += message + Environment.NewLine;
        }

        private void button2_Click(object sender, EventArgs e) {
            _mySock?.Disconnect("Ending connection");
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
