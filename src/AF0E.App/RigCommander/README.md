# Rig Commander

Lightweight **.NET console application** that exposes a simple HTTP API to control amateur radio equipment.

Currently supported devices:

- **Icom IC-9100** via CI‑V over USB
- **Yaesu FT-8x7** via CI‑V over USB
- **K1EL WinKeyer 3 (WK3)** for CW keying

The server exposes an HTTP interface that allows automation, remote control, and integration with logging or contest software.

---

# Server

The server listens on:

```
http://localhost:5000
```

---

# Configuration

Example CI‑V radio configuration:

```csharp
var civ = new CivIcomSerial(
    portName: "COM3",
    baudRate: 19200,
    radioAddress: 0x7C,
    controllerAddress: 0xE0
);
```

Example WinKeyer configuration:

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

## Radio Control (IC‑9100)

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
| POST | `/winkeyer/wpm` | Set persistent CW speed |
| POST | `/winkeyer/send` | Send CW text/script |

---

# Health Check

```
curl http://localhost:5000/health
```

Response

```json
{ "ok": true }
```

---

# WinKeyer Send Examples

Basic send:

```
curl -X POST http://localhost:5000/winkeyer/send -H "Content-Type: application/json" -d "{"Text":"CQ CQ DE AF0E K"}"
```

Set speed and send:

```
curl -X POST http://localhost:5000/winkeyer/send -H "Content-Type: application/json" -d "{"Text":"CQ CQ DE AF0E K","Wpm":20}"
```

Repeat message:

```
curl -X POST http://localhost:5000/winkeyer/send -H "Content-Type: application/json" -d "{"Text":"CQ CQ DE AF0E K","Repeat":3}"
```

Repeat with delay:

```
curl -X POST http://localhost:5000/winkeyer/send -H "Content-Type: application/json" -d "{"Text":"CQ CQ DE AF0E K","Repeat":3,"RepeatDelaySeconds":5}"
```

---

# Embedded WinKeyer Commands

| Command | Description |
|-------|-------------|
| `/Snn` | Temporary speed change |
| `/Wnn` | Wait nn seconds |
| `/Knn` | Key down nn seconds |
| `/Rxy` | Send prosign |
| `/X` | Cancel buffered speed |

Examples

```
CQ CQ /S25 DE AF0E
```

```
TU /RAR
```

```
CQ CQ /W05 DE AF0E
```

---

# Notes

- Frequency values are expressed in **Hz**
- Radio filter values represent **filter slots**
- WinKeyer uses **host mode**
- `/Snn` affects only the current message
- `"Wpm"` in `/winkeyer/send` sets persistent speed
- If radio is in split mode, the first WK send command will be ignored, returning the split flag
