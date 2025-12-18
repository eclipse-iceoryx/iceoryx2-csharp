#!/bin/bash
cd /Users/patdhlk/src/patdhlk/iceoryx2/iceoryx2-ffi/csharp/examples/PublishSubscribe

# Start publisher in background
dotnet run -c Release -- publisher &
PUB_PID=$!

# Wait for publisher to start
sleep 2

# Run subscriber for 5 seconds
dotnet run -c Release -- subscriber &
SUB_PID=$!

# Wait 5 seconds to see if data flows
sleep 5

# Kill both processes
kill $PUB_PID $SUB_PID 2>/dev/null

echo "Test complete"
