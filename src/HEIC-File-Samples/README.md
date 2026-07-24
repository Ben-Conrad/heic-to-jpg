# HEIC sample fixtures

Real-world HEIC files used to manually exercise both decode paths in
[`HeicConverter`](../HeicToJpg.Core/HeicConverter.cs). Not wired into an automated
test suite (there isn't one yet) — run the converter against these by hand when
touching decode or metadata-preservation logic.

## Layout

- `wic-fallback-path/` — files that libheif (capped at 1.15.1 by the native NuGet
  package) can't open, so they fall back to the WIC decoder. Currently iPhone 17
  Pro photos, which trip libheif's "too many auxiliary image references" limit.
- `libheif-path/` — files libheif can decode directly. Currently two iPhone 14
  Pro photos (pre-iOS 18 capture date), used to exercise this path's EXIF/XMP/
  ICC/IPTC handling.

Within each folder, filenames are suffixed `-gps` or `-nogps` to flag whether the
file carries real GPS EXIF data, since GPS preservation/stripping
(`ConversionOptions.PreserveGps`) is a separate thing worth testing on its own.

## Before adding a new file

This repo is public. **`*-gps.HEIC` files are gitignored** (see `.gitignore`) —
any file with real GPS coordinates stays local-only for manual testing and is
never committed. Keep the `-gps`/`-nogps` suffix accurate so this stays
effective; a geotagged file added without the `-gps` suffix would slip past the
gitignore rule and get committed with real location data intact.
