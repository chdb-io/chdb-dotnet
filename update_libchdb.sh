#!/bin/bash

# Download the correct version based on the platform
case "$(uname -s)" in
    Linux)
        if [[ $(uname -m) == "aarch64" ]]; then
            PLATFORM="linux-aarch64"
        else
            PLATFORM="linux-x86_64"
        fi
        ;;
    Darwin)
        if [[ $(uname -m) == "arm64" ]]; then
            PLATFORM="macos-arm64"
        else
            PLATFORM="macos-x86_64"
        fi
        ;;
    *)
        echo "Unsupported platform"
        exit 1
        ;;
esac

# Get the newest release version
# LATEST_RELEASE=$(curl --silent "https://api.github.com/repos/chdb-io/chdb/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
LATEST_RELEASE=v2.0.4
RELEASE=${1:-$LATEST_RELEASE}

DOWNLOAD_URL="https://github.com/chdb-io/chdb/releases/download/$RELEASE/$PLATFORM-libchdb.tar.gz"

echo "Downloading $PLATFORM-libchdb.tar.gz from $DOWNLOAD_URL (latest is $LATEST_RELEASE)"

# Download the file
curl -L -o libchdb.tar.gz "$DOWNLOAD_URL"

# Untar the file
tar -xzf libchdb.tar.gz

# Set execute permission for libchdb.so
chmod +x libchdb.so

# Clean up
rm -f libchdb.tar.gz
