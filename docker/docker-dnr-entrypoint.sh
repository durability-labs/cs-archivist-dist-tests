#!/bin/bash
echo "Running continuous tests..."
cd /app/Tests/ArchivistContinuousTests
exec "$@"

