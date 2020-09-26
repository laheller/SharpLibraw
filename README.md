# SharpLibraw

C#.NET wrapper around [Libraw](https://github.com/LibRaw/LibRaw).
Wrapper class *rawlib.cs* includes C# signatures for all external native methods from *libraw.dll*.

Try the demo console application *Program.cs* which demonstrates key libraw features, like:
- initialize library
- open raw file
- export raw file to TIFF
- export thumbnail from raw file to JPEG
- load raw data, process it and export to Windows bitmap
- error handling

# Demo project

To successfully build wrapper and demo application the following steps are needed:
- Download and install Visual Studio 2017 or 2019 Community Edition.
- Open *LibRAWDemo.sln* solution file and start build.
- The built executable *LibRAWDemo.exe* requires to have the included *libraw.dll* library in the same folder (should be there automatically)!
- Note: **64bit build** of the project is **required**, since the included *libraw.dll* is also a 64bit library! 

# Sample usage of wrapper

```C#
using System;
using static LibRAWDemo.RAWLib;

class Program {
	static void Main(string[] args) {
		var handler = libraw_init(LibRaw_init_flags.LIBRAW_OPTIONS_NONE);
		libraw_set_output_tif(handler, LibRaw_output_formats.TIFF);
		libraw_set_no_auto_bright(handler, 0);
		var r = libraw_open_file(handler, @"C:\Temp\RawFile01.cr2");
		if (r != LibRaw_errors.LIBRAW_SUCCESS) {
			Console.WriteLine("Open file:       " + PtrToStringAnsi(libraw_strerror(r)));
			libraw_close(handler);
			return;
		}
		r = libraw_unpack(handler);
		r = libraw_dcraw_process(handler);
		r = libraw_dcraw_ppm_tiff_writer(handler, @"C:\Temp\Processed01.tiff");
		libraw_close(handler);
	}
}
```