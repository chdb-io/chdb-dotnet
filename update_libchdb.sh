#!/bin/bash
set -eo pipefail

determine_platform() {
    case "$(uname -s)" in
        Linux)
            if [[ $(uname -m) == "aarch64" ]]; then
                echo "linux-aarch64"
            else
                echo "linux-x86_64"
            fi
            ;;
        Darwin)
            if [[ $(uname -m) == "arm64" ]]; then
                echo "macos-arm64"
            else
                echo "macos-x86_64"
            fi
            ;;
        *)
            echo "Unsupported platform"
            exit 1
            ;;
    esac
}

# libchdb binaries are built in chdb-io/chdb-core and versioned after ClickHouse.
# Pick the latest stable (non-prerelease) release which contains the libchdb
# tarball for the requested platform.
REPO="chdb-io/chdb-core"

# unauthenticated github api requests are rate-limited (fails on shared CI runner
# IPs), so authenticate with GITHUB_TOKEN when available
CURL_AUTH=()
if [ -n "$GITHUB_TOKEN" ]; then
    CURL_AUTH=(--header "Authorization: Bearer $GITHUB_TOKEN")
fi

determine_latest_release() {
    local response tags tag url
    response=$(curl --silent "${CURL_AUTH[@]}" "https://api.github.com/repos/$REPO/releases?per_page=30")
    tags=$(echo "$response" \
        | grep -E '"(tag_name|prerelease)":' \
        | sed -E 's/.*"tag_name": "([^"]+)".*/\1/; s/.*"prerelease": (true|false).*/\1/' \
        | paste - - | awk '$2 == "false" { print $1 }')
    if [ -z "$tags" ]; then
        echo "Could not list releases of $REPO (rate limited?): $response" >&2
    fi
    for tag in $tags; do
        url="https://github.com/$REPO/releases/download/$tag/$PLATFORM-libchdb.tar.gz"
        if [ "$(curl --silent --head --location --output /dev/null --write-out '%{http_code}' "$url")" = "200" ]; then
            echo "$tag"
            return
        fi
    done
}

PLATFORM=${2:-$(determine_platform)}
RELEASE=${1:-$(determine_latest_release)}

if [ -z "$RELEASE" ]; then
    echo "No release with $PLATFORM-libchdb.tar.gz found"
    exit 1
fi

# Notify if selected release is not the latest
if [ -z "$1" ]; then
    echo "Using latest version $RELEASE"
else
    LATEST_RELEASE=$(determine_latest_release)
    if [ "$RELEASE" != "$LATEST_RELEASE" ]; then
        echo "Using version $RELEASE, while the latest version is $LATEST_RELEASE"
    fi
fi

# Download the file, untar into lib/<platform> and cleanup
DOWNLOAD_URL="https://github.com/$REPO/releases/download/$RELEASE/$PLATFORM-libchdb.tar.gz"
echo "Downloading $PLATFORM-libchdb.tar.gz from $DOWNLOAD_URL"
curl -L -o libchdb.tar.gz "$DOWNLOAD_URL"
tar -xzf libchdb.tar.gz
mkdir -p "lib/$PLATFORM"
mv libchdb.so "lib/$PLATFORM/libchdb.so"
chmod +x "lib/$PLATFORM/libchdb.so"
# refresh mtime, otherwise CopyToOutputDirectory=PreserveNewest may keep a stale copy
touch "lib/$PLATFORM/libchdb.so"
rm -f libchdb.tar.gz
echo "Extracted to lib/$PLATFORM/libchdb.so"
