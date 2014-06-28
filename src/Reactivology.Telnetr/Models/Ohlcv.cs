using System;

namespace Reactivology.Telnetr.Models {
    public class Ohlcv {
        public string Symbol { get; set; }

        public DateTimeOffset Datetime { get; set; }

        public TimeSpan Timespan { get; set; }

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }

        public double Volume { get; set; }

        public long Count { get; set; }

        public double Vwap { get; set; }

        public override string ToString() {
            return string.Format("[Ohlcv: Datetime={0}, Timespan={1}, Symbol={2}, Open={3}, High={4}, Low={5}, Close={6}, Volume={7}, Count={8}, Vwap={9}]", Datetime, Timespan, Symbol, Open, High, Low, Close, Volume, Count, Vwap);
        }
    }    
}
