# AF0E Logbook Web API
[http://localhost:5200/scalar](http://localhost:5200/scalar) - Scalar

## DX Cluster

- Configure one or more servers in the `DxCluster` section of `appsettings.json`
- `GET /api/v1/dxcluster/status` starts the singleton service on demand and returns connection status
- `GET /api/v1/dxcluster/spots?since=2026-05-06T18:00:00Z&filter=Rare DX` returns cached recent spots, optionally filtered by spot time and a named filter
- The service reconnects automatically and stops itself after the configured inactivity timeout
- Changes to `DxCluster:Filters` in `dxcluster.filters.json` are hot-reloaded; connection/server settings are still loaded at service startup
- DX cluster spots are also enriched with a best-effort DXCC entity name and country code by matching the spotted callsign against `Dxcc.PrefixRegExp`; the UI renders a small flag image from that country code when available

### Named DX Cluster filters

You can define reusable named filters in `dxcluster.filters.json`:

```json
{
  "DxCluster": {
    "Filters": [
	  {
		"Name": "20m CW",
		"Modes": [ "CW" ],
		"FrequencyWindows": [
		  { "MinFrequencyKhz": 14000.0, "MaxFrequencyKhz": 14070.0 }
		]
	  }
    ]
  }
}
```

Full example with callsign patterns, modes, and frequency windows:

```json
{
  "DxCluster": {
	"Filters": [
	  {
		"Name": "Rare DX 20m/15m",
		"CallsignPatterns": "3Y0.*|FT8.*|P5.*|VP6.*|ZS8.*",
		"Modes": [ "CW", "SSB", "FT8" ],
		"FrequencyWindows": [
		  { "MinFrequencyKhz": 14000.0, "MaxFrequencyKhz": 14350.0 },
		  { "MinFrequencyKhz": 21000.0, "MaxFrequencyKhz": 21450.0 }
		]
	  },
	  {
		"Name": "20m CW",
		"Modes": [ "CW" ],
		"FrequencyWindows": [
		  { "MinFrequencyKhz": 14000.0, "MaxFrequencyKhz": 14070.0 }
		]
	  }
	]
  }
}
```

Optional environment-specific overrides are also loaded from `dxcluster.filters.{Environment}.json`, for example `dxcluster.filters.Development.json`.

- `CallsignPatterns`, `Modes`, and `FrequencyWindows` are all optional; omit any section to leave that dimension unfiltered
- when present, `CallsignPatterns` is a pipe-delimited list of regex patterns and each pattern is anchored as `^pattern$`
- invalid regex fragments are ignored and reported back in the DX cluster status payload
- mode detection is best-effort: explicit mode text in the spot/comment wins, otherwise the service falls back to common amateur band-plan frequency windows
- DXCC matching is also best-effort: the matcher prefers the most specific prefix/regex match available, but some entities remain inherently ambiguous from callsign alone
- when a DXCC entity is matched, the feed also classifies your log status into four cases: verified on the same band/mode, verified on another band/mode, worked but not verified, or not worked yet; the DX cluster UI colors rows green, yellow, orange, and red respectively
