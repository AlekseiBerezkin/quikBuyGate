using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace quikBuyGate.Model
{
    class DataTicker
    {
        [JsonProperty("result")]
        public Result result { get; set; }
}

    class Result
    {
        [JsonProperty("lowest_ask")]
        public string last { get; set; }
    }

    
}
