using System;
using System.Text.Json.Serialization;

namespace Remote
{
	namespace Data
	{
		/// <summary>
		/// Diff. Depth stream
		/// </summary>
		public class DiffDataRemote
		{
			[JsonPropertyName("e")]
			public string Type { get; set; } // "e": "depthUpdate", // DiffData type
			[JsonPropertyName("E")]
			public Int64 Time { get; set; } //"E": 123456789,     // DiffData time
			[JsonPropertyName("s")]
			public string Symbol { get; set; } //"s": "BNBBTC",      // Symbol
			[JsonPropertyName("U")]
			public Int64 FirstUpdateID { get; set; } //"U": 157,           // First update ID in event
			[JsonPropertyName("u")]
			public Int64 FinalUpdateID { get; set; } //"u": 160,           // Final update ID in event
			[JsonPropertyName("b")]
			public string[][] Bids { get; set; } // "b": [              // Bids to be updated
												 //[
												 // "0.0024",       // Price level to be updated
												 // "10"            // Quantity
												 //]
												 //],
			[JsonPropertyName("a")]
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
			[JsonPropertyName("lastUpdateId")]
			public Int64 LastUpdateId { get; set; }
			[JsonPropertyName("bids")]
			public string[][] Bids { get; set; }
			[JsonPropertyName("asks")]
			public string[][] Asks { get; set; }
		}

	}
}