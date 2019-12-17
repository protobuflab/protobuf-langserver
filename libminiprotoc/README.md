# libminiprotoc

This folder contains the native wrapper for `libprotoc`

This project vcpkg to build. Follow the instuctions at https://github.com/microsoft/vcpkg to install vcpkg.

## Building

```
mkdir build
cd build
cmake .. -DCMAKE_TOOLCHAIN_FILE=C:\src\vcpkg\scripts\buildsystems\vcpkg.cmake -G "Visual Studio 16 2019"
msbuild .\libminiprotoc.sln
```
