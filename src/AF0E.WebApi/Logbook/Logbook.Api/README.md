# AF0E Logbook Web API
[http://localhost:5200/scalar](http://localhost:5200/scalar) - Scalar

## DX Cluster phase 1

- Configure one or more servers in the `DxCluster` section of `appsettings.json`
- `GET /api/v1/dxcluster/status` starts the singleton service on demand and returns connection status
- `GET /api/v1/dxcluster/spots?since=2026-05-06T18:00:00Z` returns cached recent spots, optionally filtered by spot time
- The service reconnects automatically and stops itself after the configured inactivity timeout

