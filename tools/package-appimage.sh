#!/usr/bin/env bash
# Packages the desktop Linux app (NativeAOT, linux-x64) as an AppImage: <out>/Hackerist-<version>-x86_64.AppImage.
#
# Everything is built in Ubuntu 22.04 (tools/linux/container.sh; run directly inside it), so the package needs glibc
# 2.35 at most. appimagetool (pinned, checksum verified) is downloaded once into the cache folder; it runs without
# FUSE. With HACKERIST_APPIMAGE_SIGN_KEY set (a gpg key id), appimagetool signs the image; without it the image is
# unsigned.
# Usage: tools/package-appimage.sh [output folder, default out/]
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
out=$(mkdir -p "${1:-$root/out}" && cd "${1:-$root/out}" && pwd)

if [[ ${HACKERIST_IN_CONTAINER:-0} != 1 ]] && ! grep -qs '^VERSION_ID="22.04"' /etc/os-release; then
  exec "$root/tools/linux/container.sh" "$root/tools/package-appimage.sh" "$out"
fi

version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props" | head -n 1)

tool_version=1.9.1
tool_sha256=ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0
cache=${XDG_CACHE_HOME:-$HOME/.cache}/hackerist
tool=$cache/appimagetool-$tool_version-x86_64.AppImage

if [[ ! -x $tool ]]; then
  mkdir -p "$cache"
  echo "Downloading appimagetool $tool_version"
  curl -fsSL -o "$tool.part" "https://github.com/AppImage/appimagetool/releases/download/$tool_version/appimagetool-x86_64.AppImage"
  echo "$tool_sha256  $tool.part" | sha256sum -c --quiet
  chmod +x "$tool.part"
  mv "$tool.part" "$tool"
fi

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

# Separate build folders, so the container's restore does not mix with the host's obj/ and bin/.
dotnet publish "$root/src/TheHackerist.Desktop" -c Release -r linux-x64 -o "$work/publish" -warnaserror \
  --artifacts-path "$work/artifacts"

appdir=$work/Hackerist.AppDir
bin=$appdir/usr/bin
share=$appdir/usr/share
doc=$share/doc/hackerist
mkdir -p "$bin" "$share/applications" "$share/pixmaps" "$doc"

# The program and the native libraries it loads from its own folder (symbols and .pdb files stay out).
cp "$work/publish/Hackerist" "$work/publish/"*.so "$bin/"

# Nothing in the package may need a glibc newer than the baseline.
newest=$(for f in "$bin"/*; do objdump -T "$f"; done | grep -o 'GLIBC_[0-9.]*' | sort -uV | tail -1)
if [[ $(printf '%s\n' "$newest" GLIBC_2.35 | sort -V | tail -1) != GLIBC_2.35 ]]; then
  echo "The package needs $newest, newer than glibc 2.35." >&2
  exit 1
fi
echo "Newest glibc symbol: $newest"

icon=$root/src/TheHackerist.Core/Assets/Icon.png
cp "$icon" "$share/pixmaps/com.drasticactions.hackerist.png"
cp "$icon" "$appdir/com.drasticactions.hackerist.png"

cp "$root/LICENSE.md" "$root/THIRD-PARTY-NOTICES.md" "$doc/"

cat > "$share/applications/com.drasticactions.hackerist.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Hackerist
Comment=Read Hacker News
Exec=Hackerist
Icon=com.drasticactions.hackerist
Categories=Network;News;
StartupWMClass=Hackerist
Terminal=false
EOF
cp "$share/applications/com.drasticactions.hackerist.desktop" "$appdir/"

# .NET needs the system's ICU for culture data; where there is none (minimal systems), the app runs with invariant
# culture data instead of not starting.
cat > "$appdir/AppRun" <<'EOF'
#!/bin/sh
here=$(dirname "$(readlink -f "$0")")
if [ -z "${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-}" ] \
    && ! { /sbin/ldconfig -p 2>/dev/null || ldconfig -p 2>/dev/null; } | grep -q 'libicuuc\.so\.[0-9]'; then
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
fi
exec "$here/usr/bin/Hackerist" "$@"
EOF
chmod +x "$appdir/AppRun"

sign=()
[[ -n ${HACKERIST_APPIMAGE_SIGN_KEY:-} ]] && sign=(--sign --sign-key "$HACKERIST_APPIMAGE_SIGN_KEY")
image=$out/Hackerist-$version-x86_64.AppImage
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$tool" --no-appstream "${sign[@]}" "$appdir" "$image"
echo "$image"
