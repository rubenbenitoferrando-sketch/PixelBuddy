// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "PixelBuddy",
    platforms: [.macOS(.v14)],
    targets: [
        .executableTarget(name: "PixelBuddy", path: "Sources/PixelBuddy")
    ]
)
