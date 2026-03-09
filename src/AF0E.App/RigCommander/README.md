
# Rig Commander

Lightweight **.NET console application** that exposes a simple HTTP API to control amateur radio equipment.

Currently supported devices:

- **Icom IC-9100** via CI-V over USB
- **Yaesu FT-8x7** via CI-V over USB
- **K1EL WinKeyer 3 (WK3)** for CW keying

The server exposes an HTTP interface that allows automation, remote control, and integration with logging or contest software.

---

# Server

The server listens on:

```
http://localhost:5050
```

---

# Configuration

Example CI-V radio configuration:

```csharp
var civ = new CivIcomSerial(
    portName: "COM3",
    baudRate: 19200,
    radioAddress: 0x7C,
    controllerAddress: 0xE0
);
```

Example Winkeyer configuration:

```
Port: COM5
Baud: 1200
Device: WinKeyer 3
```

---

# API Summary

## General

| Method | Endpoint | Purpose |
|------:|----------|--------|
| GET | `/health` | Health check |

---

## Radio Control (IC-9100)

| Method | Endpoint | Purpose |
|------:|----------|--------|
| GET | `/radio/frequency` | Read current frequency |
| GET | `/radio/mode` | Read current mode |
| GET | `/radio/status` | Read full radio state |
| POST | `/radio/status` | Set frequency and/or mode |

---

## CW Keyer (WinKeyer 3)

| Method | Endpoint | Purpose |
|------:|----------|--------|
| GET | `/winkeyer/status` | Keyer status |
| POST | `/winkeyer/wpm` | Set CW speed |
| POST | `/winkeyer/send` | Send CW text/script |

---

# Health Check

```
curl http://localhost:5050/health
```

Response

```json
{
  "ok": true
}
```

---

# Radio API

## Read Frequency

Reads current radio frequency in Hz.

```
curl http://localhost:5050/radio/frequency
```

Response

```json
{
  "ok": true,
  "frequencyHz": 145500000
}
```

---

## Read Mode

Reads the current mode and filter slot.

```
curl http://localhost:5050/radio/mode
```

Response

```json
{
  "ok": true,
  "mode": "USB-D",
  "filter": 1,
  "data": true
}
```

Note:

```
filter = FIL1–FIL3
```

---

## Read Full Radio Status

```
curl http://localhost:5050/radio/status
```

Response

```json
{
  "ok": true,
  "frequencyHz": 145500000,
  "mode": "FM",
  "filter": 1,
  "data": false
}
```

---

# Set Frequency and/or Mode

Endpoint

```
POST /radio/status
```

Content type

```
application/json
```

---

## Set Frequency

```
curl -X POST http://localhost:5050/radio/status \
-H "Content-Type: application/json" \
-d "{\"FrequencyHz\":14074000}"
```

---

## Set Mode

```
curl -X POST http://localhost:5050/radio/status \
-H "Content-Type: application/json" \
-d "{\"Mode\":\"USB\"}"
```

---

## Set Frequency and Mode

```
curl -X POST http://localhost:5050/radio/status \
-H "Content-Type: application/json" \
-d "{\"FrequencyHz\":145500000,\"Mode\":\"FM\"}"
```

---

## Set USB Digital

```
curl -X POST http://localhost:5050/radio/status \
-H "Content-Type: application/json" \
-d "{\"Mode\":\"USB-D\"}"
```

---

# Supported Modes

```
LSB
USB
AM
FM
WFM
CW
RTTY
RTTYR
USB-D
LSB-D
```

---

# WinKeyer API

## Read Keyer Status

```
curl http://localhost:5050/winkeyer/status
```

Example response

```json
{
  "portOpen": true,
  "hostOpen": true,
  "revision": 23,
  "busy": false,
  "wait": false,
  "xoff": false,
  "hostWpm": null,
  "speedPotRaw": 12,
  "speedPotWpm": 22,
  "effectiveWpm": 22
}
```

Meaning:

| Field | Meaning |
|------|--------|
| hostWpm | Speed set by host |
| speedPotWpm | Speed from hardware knob |
| effectiveWpm | Actual keying speed |

If `hostWpm` is `null`, the **speed pot controls the speed**.

---

# Set CW Speed

Endpoint

```
POST /winkeyer/wpm
```

Example

```
curl -X POST http://localhost:5050/winkeyer/wpm \
-H "Content-Type: application/json" \
-d "{\"Wpm\":20}"
```

Response

```json
{
  "ok": true,
  "hostWpm": 20,
  "effectiveWpm": 20
}
```

---

## Return to Speed Pot Control

```
curl -X POST http://localhost:5050/winkeyer/wpm \
-H "Content-Type: application/json" \
-d "{\"Wpm\":0}"
```

---

# Send CW

Endpoint

```
POST /winkeyer/send
```

Example

```
curl -X POST http://localhost:5050/winkeyer/send \
-H "Content-Type: application/json" \
-d "{\"Text\":\"CQ CQ DE AF0E AF0E K\"}"
```

---

# Embedded WinKeyer Commands (WK3)

The `/winkeyer/send` endpoint supports **embedded WK3 commands**.

These commands are passed directly to the keyer.

| Command | Description |
|-------|-------------|
| `/Snn` | Set CW speed |
| `/Wnn` | Wait nn seconds |
| `/Knn` | Key down nn seconds |
| `/R` | Merge next two letters into prosign |
| `/X` | Cancel previous buffered command |

Examples

### Change speed mid-message

```
CQ CQ /S25 DE AF0E
```

---

### Send prosign AR

```
TU /RAR
```

---

### Insert pause

```
CQ CQ /W05 DE AF0E
```

---

### Key down carrier

```
/K03 TEST
```

---

# Notes

- Frequency is expressed in **Hz**
- Radio filter values represent **filter slots (FIL1–FIL3)**, not bandwidth
- Serial communication is synchronized internally
- WinKeyer communication uses **host mode**

---

# Typical Uses

- Radio automation
- CW beacon control
- Contest automation
- Integration with logging software
- Remote station control
