# HEIC to JPG Windows Converter — Project Research

## Project Goal
Build a polished Windows desktop app that makes it easy to convert HEIC photos 
(downloaded from Google Photos / iPhone) to JPG. Target users are non-technical 
people frustrated with existing Windows HEIC tools.

## Recommended Tech Stack
| Layer | Choice | Rationale |
|---|---|---|
| Language | C# (.NET 8) | Best Windows ecosystem fit |
| UI | WPF | Modern look, great drag-and-drop, tons of resources |
| HEIC decode | LibHeifSharp + libheif native DLL | Most capable, LGPL-friendly |
| JPEG encode | ImageSharp or System.Drawing | Easy NuGet or built-in |
| Distribution | MSIX or ClickOnce | Clean Windows installer |

## HEIC Decoding Library Options (ranked by recommendation)

### 1. LibHeifSharp (top pick)
- NuGet: `LibHeifSharp`
- .NET Standard 2.0 bindings for libheif
- Supports decoding, thumbnails, metadata
- Requires libheif native DLL bundled with app
- LGPL licensed

### 2. Imaging.Heif
- NuGet: `FileOnQ.Imaging.Heif`
- Simple API: `new HeifImage("image.heic").Primary().Write("output.jpeg", 90)`
- Requires Visual C++ Redistributable on user machine

### 3. Openize.HEIC
- Lightweight open-source .NET API
- Supports JPG, PNG, PDF output
- Good for simpler use cases

### 4. Windows Imaging Component (WIC) API
- Built into Windows, zero extra dependencies
- Requires user to have HEIC codec extension already installed
- Not reliable as a standalone solution

### 5. pillow-heif (Python alternative)
- Easier to prototype but harder to distribute cleanly on Windows
- Not recommended for this project

## Patent / Legal Considerations
- HEVC (the codec inside HEIC) is patent-encumbered
- For personal/open-source use: generally fine using libheif (LGPL)
- For commercial distribution: consult a lawyer about HEVC patent licensing
- Most open-source tools ship without a commercial license in practice

## UI Framework
- **WPF** is the recommendation: modern enough, mature ecosystem, 
  great drag-and-drop support, huge community resources
- WinUI 3 is newer/shinier but has a steeper learning curve
- WinForms is too dated-looking for a consumer tool

## Key Features to Build (priority order)
1. Drag-and-drop files and/or folders onto the window
2. Batch conversion with progress bar
3. EXIF/metadata preservation (date taken, GPS, camera settings)
4. JPEG quality slider (default ~90%)
5. Output folder selection (same folder as source, or custom destination)
6. File naming options (keep original name, add suffix, etc.)
7. Before/after preview thumbnails
8. (Stretch) Right-click shell extension for Windows Explorer integration

## Pain Points in Existing Tools (differentiation opportunities)
- Most free tools bundle adware or are nagware
- Online tools cap free conversions (10–25/day) and require upload
- Many strip EXIF metadata
- UIs are either too minimal (CLI) or overwhelming
- Poor default compression settings cause quality loss
- Batch processing is slow or single-threaded
- Many charge $20+ for unlimited conversions

## What Good Looks Like
- Clean, simple window — drop files, pick quality, click Convert
- Fast multi-threaded batch processing
- EXIF preserved by default
- No network calls, fully offline
- Free, no adware, open source
- Single EXE or clean MSIX installer, no bloat

## Sample Code Snippet (LibHeifSharp decode path)
```csharp
using LibHeifSharp;

using var context = new HeifContext();
context.ReadFromFile("input.heic");
using var handle = context.GetPrimaryImageHandle();
using var image = handle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgb24);
// then encode to JPEG using ImageSharp or System.Drawing
```

## References
- libheif GitHub: https://github.com/strukturag/libheif (LGPL)
- LibHeifSharp NuGet: https://www.nuget.org/packages/LibHeifSharp/
- FileOnQ.Imaging.Heif: https://github.com/FileOnQ/Imaging.Heif
- Openize.HEIC: https://products.openize.com/heic/net/
- WPF docs: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- Windows App frameworks overview: https://learn.microsoft.com/en-us/windows/apps/get-started/