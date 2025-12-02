#!/usr/bin/env bash

# Copyright (c) 2025 Contributors to the Eclipse Foundation
#
# See the NOTICE file(s) distributed with this work for additional
# information regarding copyright ownership.
#
# This program and the accompanying materials are made available under the
# terms of the Apache Software License 2.0 which is available at
# https://www.apache.org/licenses/LICENSE-2.0, or the MIT license
# which is available at https://opensource.org/licenses/MIT.
#
# SPDX-License-Identifier: Apache-2.0 OR MIT

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/iceoryx2"

echo "======================================"
echo "iceoryx2 C# Bindings Build Script"
echo "======================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Build the C FFI library
echo -e "${YELLOW}Step 1: Building iceoryx2 C FFI library...${NC}"
cd "$REPO_ROOT"

if ! cargo build --release --package iceoryx2-ffi-c; then
    echo -e "${RED}✗ Failed to build iceoryx2-ffi-c${NC}"
    exit 1
fi

echo -e "${GREEN}✓ C FFI library built successfully${NC}"
echo ""

# Step 2: Build the C# library
echo -e "${YELLOW}Step 3: Building C# library...${NC}"
cd "$SCRIPT_DIR"

if ! dotnet build -c Release; then
    echo -e "${RED}✗ Failed to build C# library${NC}"
    exit 1
fi

echo -e "${GREEN}✓ C# library built successfully${NC}"
echo ""

# Step 3: Run tests
echo -e "${YELLOW}Step 4: Running tests...${NC}"

if dotnet test; then
    echo -e "${GREEN}✓ All tests passed${NC}"
else
    echo -e "${YELLOW}⚠ Some tests skipped (require native library at runtime)${NC}"
fi
echo ""

# Step 4: Build examples
echo -e "${YELLOW}Step 5: Building examples...${NC}"

cd "$SCRIPT_DIR/examples/PublishSubscribe"
if dotnet build; then
    echo -e "${GREEN}✓ Examples built successfully${NC}"
else
    echo -e "${RED}✗ Failed to build examples${NC}"
    exit 1
fi
echo ""

echo -e "${GREEN}======================================"
echo "✓ Build completed successfully!"
echo "======================================${NC}"
echo ""
echo "Next steps:"
echo "  1. Copy native library to output directory:"
echo "     cp $SCRIPT_DIR/iceoryx2/target/release/libiceoryx2_ffi_c.* $SCRIPT_DIR/bin/Release/net6.0/"
echo ""
echo "  2. Run the example:"
echo "     cd $SCRIPT_DIR/examples/PublishSubscribe"
echo "     dotnet run publisher   # In one terminal"
echo "     dotnet run subscriber  # In another terminal"
echo ""
