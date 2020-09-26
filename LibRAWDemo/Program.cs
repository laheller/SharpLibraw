using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static System.Reflection.Assembly;
using static System.Runtime.InteropServices.Marshal;
using static System.Text.Encoding;
using static LibRAWDemo.RAWLib;

namespace LibRAWDemo {
    class Program {
        static void Main(string[] args) {
            Console.OutputEncoding = UTF8;

            if (args.Length == 0) {
                Console.WriteLine("Usage:");
                Console.WriteLine("\t" + GetExecutingAssembly().Location + " <PathToRawFile>");
                Environment.Exit(0);
            }

            if (!File.Exists(args[0])) {
                Console.WriteLine("Path to raw file seems to be invalid or file does not exist: " + args[0]);
                Environment.Exit(0);
            }

            // initialize library
            var handler = libraw_init(LibRaw_init_flags.LIBRAW_OPTIONS_NONE);

            // get basic information
            Console.WriteLine("Library version:             {0}", PtrToStringAnsi(libraw_version()));
            Console.WriteLine("Version number:              {0}", libraw_versionNumber());
            Console.WriteLine("LibRaw capabilities:         {0}", libraw_capabilities());
            Console.WriteLine("Number of supported cameras: {0}", libraw_cameraCount());

            const int N = 10;
            Console.WriteLine("\nCamera list - first {0} items:", N);
            var camListPtr = libraw_cameraList();
            for (int i = 0; i < N; i++) {
                var ptr0 = ReadIntPtr(camListPtr, 8 * i);
                Console.WriteLine(PtrToStringAnsi(ptr0));
            }

            // set output parameters
            libraw_set_bright(handler, 1.0f);
            libraw_set_demosaic(handler, LibRaw_interpolation_quality.VNG);
            libraw_set_fbdd_noiserd(handler, LibRaw_FBDD_noise_reduction.FULL_FBDD);
            libraw_set_gamma(handler, 0, 1.0f); // index can be 0 or 1
            libraw_set_highlight(handler, LibRaw_highlight_mode.CLIP);
            libraw_set_no_auto_bright(handler, 0);   // values: 0 or 1
            libraw_set_output_bps(handler, LibRaw_output_bps.BPS8);
            libraw_set_output_color(handler, LibRaw_output_color.RAW);
            libraw_set_output_tif(handler, LibRaw_output_formats.TIFF);
            libraw_set_user_mul(handler, 0, 1.5f); // index can be 0..3

            // set callback function for libraw processing
            libraw_set_progress_handler(handler, (unused_data, state, iter, expected) => {
                if (iter == 0) {
                    var progress = PtrToStringAnsi(libraw_strprogress(state));
                    Console.WriteLine("Callback: {0,30}, expected {1,6} iterations", progress, expected);
                }
                return 0;
            }, IntPtr.Zero);

            // open RAW file and get basic info
            var r = libraw_open_file(handler, args[0]);
            if (r != LibRaw_errors.LIBRAW_SUCCESS) {
                Console.WriteLine("Open file:       " + PtrToStringAnsi(libraw_strerror(r)));
                libraw_close(handler);
                return;
            }

            Console.WriteLine("\nRAW width:       " + libraw_get_raw_width(handler));
            Console.WriteLine("RAW height:      " + libraw_get_raw_height(handler));
            Console.WriteLine("IMG width:       " + libraw_get_iwidth(handler));
            Console.WriteLine("IMG height:      " + libraw_get_iheight(handler));

            // get decoder information
            var decp = AllocHGlobal(SizeOf<libraw_decoder_info_t>());
            r = libraw_get_decoder_info(handler, decp);
            var decoder = PtrToStructure<libraw_decoder_info_t>(decp);
            Console.WriteLine("\nDecoder function: " + decoder.decoder_name);
            Console.WriteLine("Decoder flags:    " + decoder.decoder_flags);
            FreeHGlobal(decp);

            // get image parameters
            Console.WriteLine("\nImage parameters:");
            var piparam = libraw_get_iparams(handler);
            var iparam = PtrToStructure<libraw_iparams_t>(piparam);
            Console.WriteLine("Guard:        {0}", iparam.guard);
            Console.WriteLine("Make:         {0}", iparam.make);
            Console.WriteLine("Model:        {0}", iparam.model);
            Console.WriteLine("Software:     {0}", iparam.software);
            Console.WriteLine("Norm. Make:   {0}", iparam.normalized_make);
            Console.WriteLine("Norm. Model:  {0}", iparam.normalized_model);
            Console.WriteLine("Vendor:       {0}", iparam.maker_index);
            Console.WriteLine("Num. of RAWs: {0}", iparam.raw_count);
            Console.WriteLine("DNG version:  {0}", iparam.dng_version);
            Console.WriteLine("Sigma Foveon: {0}", iparam.is_foveon);
            Console.WriteLine("Colors:       {0}", iparam.colors);
            Console.WriteLine("Filterbits:   {0:X8}", iparam.filters);
            Console.WriteLine("Color desc:   {0}", iparam.cdesc);
            Console.WriteLine("XMP data len: {0:X8}", iparam.xmplen);

            // other image parameters
            Console.WriteLine("\nOther image parameters:");
            var poparam = libraw_get_imgother(handler);
            var oparam = PtrToStructure<libraw_imgother_t>(poparam);
            Console.WriteLine("ISO:          {0}", oparam.iso_speed);
            Console.WriteLine("Shutter:      {0}s", oparam.shutter);
            Console.WriteLine("Aperture:     f/{0}", oparam.aperture);
            Console.WriteLine("Focal length: {0}mm", oparam.focal_len);
            // C-style time_t equals to seconds elapsed since 1970-1-1
            var ts = new DateTime(1970, 1, 1).AddSeconds(oparam.timestamp).ToLocalTime();
            Console.WriteLine("Timestamp:    {0}", ts.ToString("yyyy-MMM-dd HH:mm:ss"));
            Console.WriteLine("Img serial no {0}", oparam.shot_order);
            Console.WriteLine("Description:  {0}", oparam.desc);
            Console.WriteLine("Artist:       {0}", oparam.artist);
            Console.WriteLine("Analog balance: {0}", oparam.analogbalance[0]);

            // get lens info
            Console.WriteLine("\nLens information:");
            var plensparam = libraw_get_lensinfo(handler);
            var lensparam = PtrToStructure<libraw_lensinfo_t>(plensparam);
            Console.WriteLine("Minimum focal length:                     {0}mm", lensparam.MinFocal);
            Console.WriteLine("Maximum focal length:                     {0}mm", lensparam.MaxFocal);
            Console.WriteLine("Maximum aperture at minimum focal length: {0}mm", lensparam.MaxAp4MinFocal);
            Console.WriteLine("Maximum aperture at maximum focal length: {0}mm", lensparam.MaxAp4MaxFocal);
            Console.WriteLine("EXIF tag 0x9205:                          {0}", lensparam.EXIF_MaxAp);
            Console.WriteLine("Lens make:                                {0}", lensparam.LensMake);
            Console.WriteLine("Lens:                                     {0}", lensparam.Lens);
            Console.WriteLine("Lens serial:                              {0}", lensparam.LensSerial);
            Console.WriteLine("Internal lens serial:                     {0}", lensparam.InternalLensSerial);
            Console.WriteLine("EXIF tag 0xA405:                          {0}", lensparam.FocalLengthIn35mmFormat);
            Console.WriteLine("Makernotes lens:                          {0}\n", lensparam.makernotes.Lens);

            // unpack data from raw file
            r = libraw_unpack(handler);
            if (r != LibRaw_errors.LIBRAW_SUCCESS) {
                Console.WriteLine("Unpack: " + PtrToStringAnsi(libraw_strerror(r)));
                libraw_close(handler);
                return;
            }

            // process data using previously defined settings
            r = libraw_dcraw_process(handler);
            if (r != LibRaw_errors.LIBRAW_SUCCESS) {
                Console.WriteLine("Process: " + PtrToStringAnsi(libraw_strerror(r)));
                libraw_close(handler);
                return;
            }

            // extract raw data into allocated memory buffer
            var ptr = libraw_dcraw_make_mem_image(handler, ref r);
            if (r != LibRaw_errors.LIBRAW_SUCCESS) {
                Console.WriteLine("Mem_thumb: " + PtrToStringAnsi(libraw_strerror(r)));
                libraw_close(handler);
                return;
            }

            // convert pointer to structure to get image info and raw data
            var img = PtrToStructure<libraw_processed_image_t>(ptr);
            Console.WriteLine("\nImage type:   " + img.type);
            Console.WriteLine("Image height: " + img.height);
            Console.WriteLine("Image width:  " + img.width);
            Console.WriteLine("Image colors: " + img.colors);
            Console.WriteLine("Image bits:   " + img.bits);
            Console.WriteLine("Data size:    " + img.data_size);
            Console.WriteLine("Checksum:     " + img.height * img.width * img.colors * (img.bits / 8));

            // rqeuired step before accessing the "data" array
            Array.Resize(ref img.data, (int)img.data_size);
            var adr = ptr + OffsetOf(typeof(libraw_processed_image_t), "data").ToInt32();
            Copy(adr, img.data, 0, (int)img.data_size);

            // calculate padding for lines and add padding
            var num = img.width % 4;
            var padding = new byte[num];
            var stride = img.width * img.colors * (img.bits / 8);
            var line = new byte[stride];
            var tmp = new List<byte>();
            for (var i = 0; i < img.height; i++) {
                Buffer.BlockCopy(img.data, stride * i, line, 0, stride);
                tmp.AddRange(line);
                tmp.AddRange(padding);
            }

            // release memory allocated by [libraw_dcraw_make_mem_image]
            libraw_dcraw_clear_mem(ptr);

            // create/save bitmap from mem image/array
            var bmp = new Bitmap(img.width, img.height, PixelFormat.Format24bppRgb);
            var bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            Copy(tmp.ToArray(), 0, bmd.Scan0, (int)img.data_size);
            bmp.UnlockBits(bmd);
            var outJPEG = args[0].Replace(Path.GetExtension(args[0]),".jpg");
            Console.WriteLine("Saving image to: " + outJPEG);
            bmp.Save(outJPEG, ImageFormat.Jpeg);

            // save to TIFF
            var outTIFF = args[0].Replace(Path.GetExtension(args[0]), ".tiff");
            Console.WriteLine("\nSaving TIFF to: " + outTIFF);
            r = libraw_dcraw_ppm_tiff_writer(handler, outTIFF);
            if (r != LibRaw_errors.LIBRAW_SUCCESS) Console.WriteLine("TIFF writer:     " + PtrToStringAnsi(libraw_strerror(r)));

            // close RAW file
            libraw_close(handler);
        }
    }
}
