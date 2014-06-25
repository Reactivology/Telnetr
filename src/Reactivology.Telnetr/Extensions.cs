using Reactivology.Telnetr.Models;
using System;

namespace Reactivology.Telnetr {
    public static class Extensions {
        internal static string FormatWith(this string format, params object[] args) {
            return string.Format(format, args);
        }

        public static IObservable<Ohlcv> Connect(this TelnetrClient client, params object[] values) {
            foreach(var value in values) {
                client.Subscribe(value as dynamic);
            }

            return (IObservable<Ohlcv>)client;
        }

        public static TimeSpan Seconds(this int value) {
            return TimeSpan.FromSeconds(value);
        }

        public static TimeSpan Minutes(this int value) {
            return TimeSpan.FromMinutes(value);
        }
    }
}