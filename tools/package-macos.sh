#!/usr/bin/env bash
# Packages the desktop macOS app (NativeAOT, osx-arm64) as Hackerist.app in <out>/Hackerist-<version>-arm64.dmg. Run
# on an Apple silicon Mac with the .NET SDK and the Xcode command line tools.
#
# Signing hooks, unsigned by default:
#   HACKERIST_MACOS_SIGN_IDENTITY  a "Developer ID Application: ..." identity: every dylib, then the app, is signed with
#                                  the hardened runtime.
#   HACKERIST_NOTARY_PROFILE       a notarytool keychain profile (xcrun notarytool store-credentials): the dmg is
#                                  notarized and stapled. Needs HACKERIST_MACOS_SIGN_IDENTITY.
# Unsigned, the app opens the first time with right-click > Open.
# Usage: tools/package-macos.sh [output folder, default out/]
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
out=$(mkdir -p "${1:-$root/out}" && cd "${1:-$root/out}" && pwd)
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props" | head -n 1)
# The oldest macOS .NET 10 supports.
minos=12.0

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

dotnet publish "$root/src/TheHackerist.Desktop" -c Release -r osx-arm64 -o "$work/publish" -warnaserror

app=$work/Hackerist.app
contents=$app/Contents
mkdir -p "$contents/MacOS" "$contents/Resources"

# The program and the native libraries it loads from its own folder (symbols and .pdb files stay out).
cp "$work/publish/Hackerist" "$work/publish/"*.dylib "$contents/MacOS/"

# Every native binary must run on the app's minimum macOS.
for bin in "$contents/MacOS/"*; do
  needs=$(vtool -show-build "$bin" | awk '$1 == "minos" {print $2}')
  if [[ $(printf '%s\n' "$needs" "$minos" | sort -V | tail -1) != "$minos" ]]; then
    echo "$(basename "$bin") needs macOS $needs, newer than the app's minimum ($minos)." >&2
    exit 1
  fi
done

sed -e "s/@VERSION@/$version/g" -e "s/@MINOS@/$minos/g" "$root/tools/macos/Info.plist.in" > "$contents/Info.plist"
plutil -lint "$contents/Info.plist"

# The icon, from the app's PNG (sips scales it; iconutil packs the set).
iconset=$work/AppIcon.iconset
mkdir -p "$iconset"
png=$root/src/TheHackerist.Core/Assets/Icon.png
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
  sips -z $((size * 2)) $((size * 2)) "$png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$contents/Resources/AppIcon.icns"

cp "$root/LICENSE.md" "$root/THIRD-PARTY-NOTICES.md" "$contents/Resources/"

if [[ -n ${HACKERIST_MACOS_SIGN_IDENTITY:-} ]]; then
  sign=(codesign --force --options runtime --timestamp --sign "$HACKERIST_MACOS_SIGN_IDENTITY")
  for lib in "$contents/MacOS/"*.dylib; do
    "${sign[@]}" "$lib"
  done
  "${sign[@]}" "$app"
  codesign --verify --deep --strict "$app"
else
  # Apple silicon runs no code without a signature: sign ad hoc (no identity), which Gatekeeper still treats as
  # unsigned.
  codesign --force --sign - "$contents/MacOS/"*.dylib
  codesign --force --sign - "$app"
  codesign --verify --deep --strict "$app"
  echo "HACKERIST_MACOS_SIGN_IDENTITY is not set: the app is unsigned (ad hoc signature)."
fi

stage=$work/dmg
mkdir -p "$stage"
cp -R "$app" "$stage/"
ln -s /Applications "$stage/Applications"
dmg=$out/Hackerist-$version-arm64.dmg
rm -f "$dmg"
hdiutil create -volname Hackerist -srcfolder "$stage" -format UDZO -ov "$dmg"

if [[ -n ${HACKERIST_NOTARY_PROFILE:-} ]]; then
  [[ -n ${HACKERIST_MACOS_SIGN_IDENTITY:-} ]] || { echo "Notarizing needs HACKERIST_MACOS_SIGN_IDENTITY." >&2; exit 1; }
  codesign --force --timestamp --sign "$HACKERIST_MACOS_SIGN_IDENTITY" "$dmg"
  xcrun notarytool submit "$dmg" --keychain-profile "$HACKERIST_NOTARY_PROFILE" --wait
  xcrun stapler staple "$dmg"
fi
echo "$dmg"
