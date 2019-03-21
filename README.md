# LowPoints Testing

![Platforms](https://img.shields.io/badge/Plugins-Windows-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7-blue.svg)
[![AutoCAD](https://img.shields.io/badge/Civil3D-2019-lightblue.svg)](http://developer.autodesk.com/)

# Description

Civil 3D plugin that looks for low points on Surfaces.

**findBowlsByVertex** command tries to find low points by looking at the vertex elevation and surrounding vertices

**findBowlsByWaterdrop** command tries to look at split points in water drop paths

In both cases, it adds a `DBPoint`, so use `PTYPE` to change point style to Circles (or some other visible style).

# Setup

## Prerequisites

1. **Visual Studio** 2017
2. **Civil 3D** 2019 required to compile changes into the plugin


## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Written by

Augusto Goncalves [@augustomaia](https://twitter.com/augustomaia), [Forge Partner Development](http://forge.autodesk.com)