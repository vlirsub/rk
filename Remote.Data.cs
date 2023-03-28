using System;
using Newtonsoft.Json;

namespace Remote
{
    namespace Data
    {
        /// <summary>
        /// Diff. Depth stream
        /// </summary>
        public class DiffDataRemote
        {
            [JsonProperty("e")]
            public string Type { get; set; } // "e": "depthUpdate", // DiffData type
            [JsonProperty("E")]
            public Int64 Time { get; set; } //"E": 123456789,     // DiffData time
            [JsonProperty("s")]
            public string Symbol { get; set; } //"s": "BNBBTC",      // Symbol
            [JsonProperty("U")]
            public Int64 FirstUpdateID { get; set; } //"U": 157,           // First update ID in event
            [JsonProperty("u")]
            public Int64 FinalUpdateID { get; set; } //"u": 160,           // Final update ID in event
            [JsonProperty("b")]
            public string[][] Bids { get; set; } // "b": [              // Bids to be updated
                                                 //[
                                                 // "0.0024",       // Price level to be updated
                                                 // "10"            // Quantity
                                                 //]
                                                 //],
            [JsonProperty("a")]
            public string[][] Asks { get; set; } //"a": [              // Asks to be updated
                                                 //[
                                                 // "0.0026",       // Price level to be updated
                                                 //"100"           // Quantity
                                                 //]
                                                 //]
        }


        /// <summary>
        /// Снимок стакана - данные от сервера
        /// </summary>
        public class Level2SnapshotRemote
        {
            [JsonProperty("lastUpdateId")]
            public Int64 LastUpdateId { get; set; }

            [JsonProperty("bids")]
            public string[][] Bids { get; set; }

            [JsonProperty("asks")]
            public string[][] Asks { get; set; }
        }
    }
}