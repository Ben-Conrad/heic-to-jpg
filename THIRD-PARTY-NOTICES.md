# Third-Party Notices

This project is licensed under the MIT License (see [LICENSE](LICENSE)). It depends
on the following third-party packages, each under its own license. These packages
are not included in this repository — they are restored from NuGet at build time —
but their license terms still apply to any binary built from this code.

## LibHeifSharp

- **Used for:** primary HEIC decoding (`HeicToJpg.Core`)
- **Version:** 3.2.0
- **License:** GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later)
- **Project:** https://github.com/0xC0000054/LibHeifSharp
- **License text:** https://www.gnu.org/licenses/lgpl-3.0.txt

## libheif (native library)

- **Used for:** underlying native HEIF/HEVC decoding engine consumed by LibHeifSharp,
  distributed via the `LibHeif.Native.win-x64` NuGet package
- **Version:** 1.15.1
- **License:** GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later)
- **Project:** https://github.com/strukturag/libheif
- **License text:** https://www.gnu.org/licenses/lgpl-3.0.txt

This application dynamically loads libheif rather than statically linking it, and
does not modify its source. The corresponding source code for the exact version
used is publicly available at the project link above.

## SixLabors.ImageSharp

- **Used for:** JPEG encoding and EXIF metadata handling (`HeicToJpg.Core`)
- **Version:** 3.1.12
- **License:** Six Labors Split License
- **Project:** https://github.com/SixLabors/ImageSharp
- **License terms:** https://sixlabors.com/pricing

The Six Labors Split License permits free use (under Apache-2.0 terms) for
open-source consuming projects, nonprofits, and organizations under $1M USD
annual gross revenue; a commercial license is otherwise required for closed-source
use as a direct dependency. Because this project's own source is MIT-licensed and
publicly available, it qualifies for the free/open-source terms.

## CommunityToolkit.Mvvm

- **Used for:** MVVM infrastructure in the WPF UI (`HeicToJpg.App`)
- **Version:** 8.3.2
- **License:** MIT License
- **Project:** https://github.com/CommunityToolkit/dotnet
