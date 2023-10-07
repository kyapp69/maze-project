# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2023-10-07

### Added

- Maze cell prefabs with cut corners.
- Cut corner probability for generating mazes.
- Occlusion culling support for both cut and full corners.

### Changed

- Upgraded to Unity 2022.3.10f1.
- **Game** uses two visualization scriptable objects, one for cut and one for full corners.
- `MazeVisualization.Visualize` acts on a single cell instead of the entire maze.

### Fixed

- Correct file name for `FieldOfView` script asset.

## [2.1.0] - 2023-09-25

### Added

- Realtime dynamic occlusion system.

### Changed

- Updated Unity to version 2022.3.9f1.
