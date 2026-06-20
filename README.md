# powmada

## Power Market Data

`powmada` is a market data feed handler and real-time order book reconstructor built in C# (.NET 10). It is designed to handle streaming and real-time energy exchange event data.

## System Prerequisites

**.NET 10 SDK** (with Native AOT capabilities).
**OS-Native Compiler Toolchain:** Xcode Command Line Tools (`xcode-select --install` on macOS) or Desktop Development with C++ (on Windows).
**Docker Desktop** (for running the containerized infrastructure cluster).

## Infrastructure Setup (ClickHouse)

To initialize an optimized, local ClickHouse testing cluster that natively permits unauthenticated connection strings, spin up the container block using Docker:

```bash
docker run -d \
  --name powmada-clickhouse \
  -p 8123:8123 \
  -p 9000:9000 \
  --ulimit nofile=262144:262144 \
  -e CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT=1 \
  clickhouse/clickhouse-server \
  -- \
  --default_password=""
