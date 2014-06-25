using Newtonsoft.Json;
using Reactivology.Telnetr.Models;
using Reactivology.Telnetr.Net;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Reactivology.Telnetr {
    public class TelnetrClient : IObservable<Ohlcv> {    
        private const int TimeSeriesPort = 40000;
        private const int SizeSeriesPort = 50000;

        private ConcurrentDictionary<string, TelnetrSocket> _sockets = new ConcurrentDictionary<string, TelnetrSocket>();
        private Subject<Ohlcv> _ohlvc = new Subject<Ohlcv>();
        private string _host;

        public TelnetrClient(string host) {
            _host = host;
        }

        public IObservable<Ohlcv> Connect(TimeSpan timespan) {
            return Subscribe("{0:hh\\:mm\\:ss}".FormatWith(timespan), _host, TimeSeriesPort + (int)timespan.TotalSeconds);
        }

        public IObservable<Ohlcv> Connect(int size) {
            return Subscribe("{0:00000000}".FormatWith(size), _host, SizeSeriesPort + size);
        }

        private IObservable<Ohlcv> Subscribe(string name, string host, int port, int receiveBufferSize = 1024 * 32) {
            var socket = _sockets.GetOrAdd(name, n => {
                var ts = new TelnetrSocket();
                ts.Connect(x => x
                    .Address(host)
                    .Port(port)
                    .ReceiveBufferSize(receiveBufferSize)
                );
                ts.Subscribe(_ohlvc);
                return ts;
            });
            return (IObservable<Ohlcv>)socket;
        }

        public IDisposable Subscribe(IObserver<Ohlcv> observer) {
            return _ohlvc.Subscribe(observer);
        }

        public Task<TimeSeriesDetails> GetTimeSeriesDetailsAsync() {
            return GetDetailsAsync<TimeSeriesDetails>(_host, TimeSeriesPort);           
        }

        public Task<SizeSeriesDetails> GetSizeSeriesDetailsAsync() {
            return GetDetailsAsync<SizeSeriesDetails>(_host, SizeSeriesPort);
        }

        private async Task<T> GetDetailsAsync<T>(string host, int port) {
            return await Task.Run(() => {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var endPoint = new DnsEndPoint(host, port);
                var buffer = new byte[1024 * 32];

                socket.Connect(endPoint);

                var recv = socket.Receive(buffer);
                var json = Encoding.UTF8.GetString(buffer, 0, recv);

                return JsonConvert.DeserializeObject<T>(json);
            });
        }
    }
}
