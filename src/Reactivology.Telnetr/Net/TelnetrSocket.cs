using Newtonsoft.Json;
using Reactivology.Telnetr.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;

namespace Reactivology.Telnetr.Net {
    public class TelnetrSocket : IObservable<Ohlcv> {
        private static readonly int ReceiveBufferSize = 8096;

        private Socket _socket;
        private ConnectSettings _settings;
        private SocketAsyncEventArgs _receive;
        private Subject<Ohlcv> _ohlcv = new Subject<Ohlcv>();
        private Subject<byte[]> _packets = new Subject<byte[]>();
        private Message _message = new Message { Buffer = new byte[ReceiveBufferSize] };
        

        public TelnetrSocket() {
            _packets.Subscribe(ProcessPacket);
        }

        private static TResult Configure<TSource, TResult>(Action<TSource> configure) where TResult : TSource, new() {
            var result = new TResult();
            configure(result);
            return result;
        }

        internal void Connect(Action<IConnectConfigurator> configure) {
            var c = Configure<IConnectConfigurator, ConnectConfigurator>(configure);
            _settings = c.Build();
            Connect(_settings);
        }

        private void Connect(ConnectSettings settings) {
            var completed = default(EventHandler<SocketAsyncEventArgs>);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var saea = new SocketAsyncEventArgs {
                RemoteEndPoint = new DnsEndPoint(_settings.Address, _settings.Port),
                UserToken = socket
            };
            
            saea.Completed += completed = (s, e) => {
                e.Completed -= completed;
                _socket = (Socket)e.UserToken;
                if(e.SocketError == SocketError.Success) {                    
                    _receive = _receive ?? (_receive = CreateSocketAsyncEventArgs());
                    Receive(_receive);
                } else {
                    Disconnect();
                    Connect(settings);
                }
            };

            if(false == socket.ConnectAsync(saea)) {
                completed(this, saea);
            }
        }

        public void Disconnect() {
            if(null != _socket) {
                if(_socket.Connected) {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Disconnect(true);
                }
                _socket.Close();
            }
        }

        private SocketAsyncEventArgs CreateSocketAsyncEventArgs() {
            var buffer = new byte[ReceiveBufferSize];
            var message = new Message {
                Buffer = buffer,
                Count = 0,
            };
            var saea = new SocketAsyncEventArgs();
            saea.SetBuffer(buffer, 0, buffer.Length);
            saea.Completed += ProcessCompleted;
            saea.UserToken = message;
            return saea;
        }

        private void Receive(SocketAsyncEventArgs e) {
            var message = e.UserToken as Message;
            e.SetBuffer(message.Count, message.Buffer.Length - message.Count);
            _socket.ReceiveAsync(e);
        }

        private void ProcessCompleted(object sender, SocketAsyncEventArgs e) {
            ProcessCompleted(e);
        }

        private void ProcessCompleted(SocketAsyncEventArgs e) {
            switch(e.LastOperation) {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                default:
                    break;
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e) {
            if(e.SocketError == SocketError.Success) {
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e) {            
            if(e.SocketError == SocketError.Success && e.BytesTransferred > 0) {
                var buffer = e.Buffer;
                var count = e.BytesTransferred;
                var message = e.UserToken as Message;

                if(count <= 0) {
                    return;
                }

                message.Count += count;
                if(message.Count > message.Buffer.Length) {
                    return;
                }

                for(int i = message.Count - 1; i >= 1; i--) {
                    if(buffer[i] == 0x0a && buffer[i - 1] == 0x0d) {
                        count = i + 1;
                        message.Count = message.Count - count;
                        break;
                    }
                }

                var packet = new byte[count];
                Buffer.BlockCopy(buffer, 0, packet, 0, count);
                _packets.OnNext(packet);

                if(message.Count != 0) {
                    Buffer.BlockCopy(message.Buffer, count, message.Buffer, 0, message.Count);
                }
                Receive(e);
            } else if(e.SocketError == SocketError.ConnectionReset) {
                
                Connect(_settings);
            }
        }

        private void ProcessDisconnect(SocketAsyncEventArgs e) {
            if(e.SocketError == SocketError.Success) {
                Connect(_settings);
            }
        }

        private void ProcessPacket(byte[] packet) {
            var start = 0;
            for(int i = 1; i < packet.Length; i++) {
                if(packet[i] == 0x0a && packet[i - 1] == 0x0d) {
                    ProcessSegment(packet, start, (i - start) + 1);
                    start = i + 1;
                }
            }            
        }

        private void ProcessSegment(byte[] buffer, int offset, int count) {
            try {
                var json = Encoding.UTF8.GetString(buffer, offset, count);
                var ohlcv = JsonConvert.DeserializeObject<Ohlcv>(json);                
                _ohlcv.OnNext(ohlcv);
            } catch {
            }
        }

        public IDisposable Subscribe(IObserver<Ohlcv> observer) {
            return _ohlcv.Subscribe(observer);
        }

        internal interface IConnectConfigurator {
            IConnectConfigurator Address(string value);
            IConnectConfigurator Port(int value);
            IConnectConfigurator ReceiveBufferSize(int value);
        }

        internal class ConnectConfigurator : IConnectConfigurator {
            private ConnectSettings _settings = new ConnectSettings {
                ReceiveBufferSize = 8 * 1024
            };

            public IConnectConfigurator Address(string value) {
                _settings.Address = value;
                return this;
            }

            public IConnectConfigurator Port(int value) {
                _settings.Port = value;
                return this;
            }

            public IConnectConfigurator ReceiveBufferSize(int value) {
                _settings.ReceiveBufferSize = value;
                return this;
            }

            public ConnectSettings Build() {
                return _settings;
            }
        }
    }

    internal class Message {
        public byte[] Buffer;
        public int Count;
    }

    internal class ConnectSettings {
        public string Address { get; set; }
        public int Port { get; set; }
        public int ReceiveBufferSize { get; set; }

        public override string ToString() {
            return (new { Address, Port, ReceiveBufferSize }).ToString();
        }
    }
}
