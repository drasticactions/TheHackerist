#!/bin/sh
# Installs what the Linux package needs on Ubuntu 22.04 (glibc 2.35, the oldest system the app supports): the NativeAOT
# toolchain, the AppImage tools' helpers and, with --dotnet, the .NET 10 SDK. The build image (Containerfile) runs
# this; so does the package-linux workflow's ubuntu:22.04 job.
# Usage: tools/linux/deps.sh [--dotnet]
set -eu

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends \
  build-essential clang zlib1g-dev git curl ca-certificates file gnupg libicu70
rm -rf /var/lib/apt/lists/*

if [ "${1:-}" = --dotnet ]; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
  rm /tmp/dotnet-install.sh
fi
