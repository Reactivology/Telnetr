using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Reactivology.Telnetr.Models {
    public class TimeSeriesDetails {
        public string Donations { get; set; }

        public ICollection<TimeSpan> Times { get; private set; }

        public ICollection<string> Symbols { get; private set; }

        public TimeSeriesDetails() {
            Times = new HashSet<TimeSpan>();
            Symbols = new HashSet<string>();
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
            //return string.Format("[TimeSeriesDetails: Donations={0}, Times={1}, Symbols={2}]", Donations, Times, Symbols);
        }
    }
}