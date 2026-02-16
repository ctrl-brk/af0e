# Rig Commander

Lightweight .NET console application that exposes a simple HTTP API to control a radio (IC-9100) over CI-V via the built-in USB port.

It allows:

- Set frequency and mode
- Read current frequency and mode

This tool is intended for automation, remote control, and integration with other systems.

------------------------------------------------------------------------

```csharp
var civ = new CivIcomSerial(
    portName: "COM3",
    baudRate: 19200,
    radioAddress: 0x7C,
    controllerAddress: 0xE0
);
```

The server listens on: http://localhost:5000

### API summary

| Method | Endpoint           | Purpose                             |
| -----: | ------------------ | ------------------------------------|
|    GET | `/health`          | Simple health check                 |
|    GET | `/radio/frequency` | Read current frequency (Hz)         |
|    GET | `/radio/mode`      | Read current mode and filter slot   |
|    GET | `/radio/status`    | Read frequency + mode + filter slot |
|   POST | `/radio/status`    | Set frequency and/or mode           |


### Health check
```
curl http://localhost:5000/health
```
#### Response
```
{ "ok": true }
```

------------------------------------------------------------------------

### Read Frequency
Reads current frequency in Hz (CI-V command 0x03).
```
curl http://localhost:5000/radio/frequency
```
#### Response
```
{
  "ok": true,
  "frequencyHz": 145500000
}
```

### Read Mode
Reads current mode and the active filter slot (CI-V command 0x04).
#### Request
```
curl http://localhost:5000/radio/mode
```
#### Response
```
{
  "ok": true,
  "mode": "USB-D",
  "filter": 1,
  "data": true
}
```
Note: filter is filter slot number (FIL1--FIL3), not bandwidth in Hz.

### Read Status
```
curl http://localhost:5000/radio/status
```
#### Response
```
{
  "ok": true,
  "frequencyHz": 145500000,
  "mode": "FM",
  "filter": 1,
  "data": false
}
```
------------------------------------------------------------------------

## Set Frequency and/or Mode

POST /radio/status

Content-Type: application/json

### Set Frequency Only

```
curl -X POST http://localhost:5000/radio/status -H "Content-Type: application/json" -d "{\"FrequencyHz\":14074000}"
```

### Set Mode Only

``` bash
curl -X POST http://localhost:5000/radio/status -H "Content-Type: application/json" -d "{\"Mode\":\"USB\"}"
```

### Set Frequency and Mode

``` bash
curl -X POST http://localhost:5000/radio/status -H "Content-Type: application/json" -d "{\"FrequencyHz\":145500000,\"Mode\":\"FM\"}"
```

### Set USB Digital

``` bash
curl -X POST http://localhost:5000/radio/status -H "Content-Type: application/json" -d "{\"Mode\":\"USB-D\"}"
```

------------------------------------------------------------------------

## Supported Modes

-   LSB
-   USB
-   AM
-   FM
-   WFM
-   CW
-   RTTY
-   RTTYR
-   USB-D
-   LSB-D

------------------------------------------------------------------------

## Notes

-   Frequency is in Hz
-   Filter is slot number (FIL1--FIL3)
-   Serial access is synchronized internally
