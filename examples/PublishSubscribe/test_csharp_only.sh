#!/bin/bash
cd /Users/patdhlk/src/patdhlk/iceoryx2/iceoryx2-ffi/csharp/examples/PublishSubscribe

echo "=== Starting C# Publisher ==="
dotnet run -c Release --no-build -- publisher 2>&1 &
PUB_PID=$!

sleep 2

echo ""
echo "=== Starting C# Subscriber ==="
dotnet run -c Release --no-build -- subscriber 2>&1 &
SUB_PID=$!

sleep 10

echo ""
echo "=== Killing processes ==="
kill $PUB_PID $SUB_PID 2>/dev/null
wait $PUB_PID $SUB_PID 2>/dev/null

echo "=== Test complete ==="
