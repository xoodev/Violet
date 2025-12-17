# Violet

[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/download)
[![AvaloniaUI](https://img.shields.io/badge/Avalonia-00AEEF?style=for-the-badge&logo=dotnet&logoColor=white)](https://avaloniaui.net/)

A high-performance, lightweight image viewer built with **C#** and **Avalonia UI**.

Violet is designed to be a fast, minimal alternative to the standard Windows Photos app. It focuses on instant loading, smooth transformations, and low memory consumption.


## ✨ Features

* 🚀 **Instant Start:** Optimized window lifecycle to ensure images load only when the UI is ready.
* 🧠 **Smart Decoding:** Uses target-width decoding to keep RAM usage low, even when opening massive 4K+ images.
* 🖱️ **Pro Navigation:**
    * **Smooth Zoom:** Precision zooming toward the mouse cursor.
    * **Infinite Pan:** Move around high-resolution images with zero lag.
    * **90° Rotation:** Quick rotation with the `R` key, featuring corrected coordinate-space panning.
* 📦 **Single File Executable:** Published as a self-contained ~100MB package with no dependencies required.


## 🛠️ Technical Stack

* **Framework:** .NET 10
* **UI Toolkit:** Avalonia UI (Cross-platform XAML-based UI)
* **Graphics:** Hardware-accelerated `RenderTransform` pipeline.
* **Deployment:** Inno Setup for Windows distribution.


## 🚀 Installation

1.  Download the latest `Violet_Setup.exe` from the [Releases](../../releases) page.
2.  Run the installer.
3.  (Optional) Set Violet as your default image viewer in **Windows Settings > Default Apps**.


## ⌨️ Controls

| Action | Control |
| :--- | :--- |
| **Open Image** | Click "Open" or double-click an associated file |
| **Zoom** | `Ctrl` + `Mouse Wheel` |
| **Pan** | `Left Click` + `Drag` |
| **Rotate 90°** | Press `R` |


## 🏗️ Building from Source

To build this project locally, ensure you have the .NET SDK installed:

```bash
# Clone the repository
git clone https://github.com/xoodev/Violet.git

# Navigate to the directory
cd Violet

# Restore and Build
dotnet restore
dotnet build -c Release
