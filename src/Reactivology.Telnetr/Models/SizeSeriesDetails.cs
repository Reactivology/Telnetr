using Newtonsoft.Json;
using System.Collections.Generic;

namespace Reactivology.Telnetr.Models {
    public class SizeSeriesDetails {
        public string Donations { get; set; }

        public ICollection<long> Sizes { get; private set; }

        public ICollection<string> Symbols { get; private set; }

        public SizeSeriesDetails() {
            Sizes = new HashSet<long>();
            Symbols = new HashSet<string>();
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
            //return string.Format("[SizeSeriesDetails: Donations={0}, Sizes={1}, Symbols={2}]", Donations, Sizes, Symbols);
        }
    }
}