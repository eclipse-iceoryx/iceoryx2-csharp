#!/bin/bash
cd "$(dirname "$(readlink -f "$0")")" || exit

# Start publisher in background
dotnet run -c Release --framework net8.0 -- publisher &
PUB_PID=$!

# Wait for publisher to start
sleep 2

# Run subscriber for 5 seconds
dotnet run -c Release --framework net8.0 -- subscriber &
SUB_PID=$!

# Wait 5 seconds to see if data flows
sleep 5

# Kill both processes
kill $PUB_PID $SUB_PID 2>/dev/null

echo "Test complete"
