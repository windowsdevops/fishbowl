/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace Standard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    internal static partial class Utility
    {        
        private static readonly Version _osVersion = Environment.OSVersion.Version;

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        private static bool _MemCmp(IntPtr left, IntPtr right, long cb)
        {
            int offset = 0;

            for (; offset < (cb - sizeof(Int64)); offset += sizeof(Int64))
            {
                Int64 left64 = Marshal.ReadInt64(left, offset);
                Int64 right64 = Marshal.ReadInt64(right, offset);

                if (left64 != right64)
                {
                    return false;
                }
            }

            for (; offset < cb; offset += sizeof(byte))
            {
                byte left8 = Marshal.ReadByte(left, offset);
                byte right8 = Marshal.ReadByte(right, offset);

                if (left8 != right8)
                {
                    return false;
                }
            }

            return true;
        }

        public static Exception FailableFunction<T>(Func<T> function, out T result)
        {
            return FailableFunction(5, function, out result);
        }

        public static T FailableFunction<T>(Func<T> function)
        {
            T result;
            Exception e = FailableFunction(function, out result);
            if (e != null)
            {
                throw e;
            }
            return result;
        }

        public static T FailableFunction<T>(int maxRetries, Func<T> function)
        {
            T result;
            Exception e = FailableFunction(maxRetries, function, out result);
            if (e != null)
            {
                throw e;
            }
            return result;
        }

        public static Exception FailableFunction<T>(int maxRetries, Func<T> function, out T result)
        {
            Assert.IsNotNull(function);
            Assert.BoundedInteger(1, maxRetries, 100);
            int i = 0;
            while (true)
            {
                try
                {
                    result = function();
                    return null;
                }
                catch (Exception e)
                {
                    if (i == maxRetries)
                    {
                        result = default(T);
                        return e;
                    }
                }
                ++i;
            }
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static byte[] GetBytesFromBitmapSource(BitmapSource bmp)
        {
            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            int stride = width * ((bmp.Format.BitsPerPixel + 7) / 8);

            var pixels = new byte[height * stride];

            bmp.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static BitmapSource GenerateBitmapSource(ImageSource img)
        {
            return GenerateBitmapSource(img, img.Width, img.Height);
        }

        public static BitmapSource GenerateBitmapSource(ImageSource img, double renderWidth, double renderHeight)
        {
            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(img, new Rect(0, 0, renderWidth, renderHeight));
            }
            var bmp = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            return bmp;
        }

        public static BitmapSource GenerateBitmapSource(Visual visual, double renderWidth, double renderHeight)
        {
            var bmp = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, renderWidth, renderHeight));
            }
            bmp.Render(dv);
            return bmp;
        }

        // This can be cached.  It's not going to change under reasonable circumstances.
        private static int s_bitDepth = 0;
        private static int _GetBitDepth()
        {
            if (s_bitDepth == 0)
            {
                using (SafeDC dc = SafeDC.GetDesktop())
                {
                    s_bitDepth = NativeMethods.GetDeviceCaps(dc, DeviceCap.BITSPIXEL) * NativeMethods.GetDeviceCaps(dc, DeviceCap.PLANES);
                }
            }
            return s_bitDepth;
        }

        public static BitmapFrame GetBestMatch(IList<BitmapFrame> frames, int width, int height)
        {
            return _GetBestMatch(frames, _GetBitDepth(), width, height);
        }

        private static BitmapFrame _GetBestMatch(IList<BitmapFrame> frames, int bitDepth, int width, int height)
        {
            return (from frame in frames
                    let frameBitDepth = frame.Decoder is IconBitmapDecoder
                        ? frame.Thumbnail.Format.BitsPerPixel
                        : frame.Format.BitsPerPixel
                    // Always prefer using the frame with the best dimensions.
                    orderby Math.Abs(frame.PixelWidth - width) + Math.Abs(frame.PixelHeight - height)
                    // If there are multiple frames that are equally good, prefer the one with the closest,
                    // but not exceeding, bit depth.
                    orderby frameBitDepth <= bitDepth ? bitDepth - frameBitDepth : int.MaxValue
                    select frame)
                .FirstOrDefault();
        }

        // Caller is responsible for destroying the HICON
        // Caller is responsible to ensure that GDI+ has been initialized.
        public static IntPtr GenerateHICON(ImageSource image, Size dimensions)
        {
            if (image == null)
            {
                return IntPtr.Zero;
            }

            // If we're getting this from a ".ico" resource, then it comes through as a BitmapFrame.
            // We can use leverage this as a shortcut to get the right 16x16 representation
            // because DrawImage doesn't do that for us.
            var bf = image as BitmapFrame;
            if (bf != null)
            {
                bf = GetBestMatch(bf.Decoder.Frames, (int)dimensions.Width, (int)dimensions.Height);
            }
            else
            {
                // Constrain the dimensions based on the aspect ratio.
                var drawingDimensions = new Rect(0, 0, dimensions.Width, dimensions.Height);

                // There's no reason to assume that the requested image dimensions are square.
                double renderRatio = dimensions.Width / dimensions.Height;
                double aspectRatio = image.Width / image.Height;

                // If it's smaller than the requested size, then place it in the middle and pad the image.
                if (image.Width <= dimensions.Width && image.Height <= dimensions.Height)
                {
                    drawingDimensions = new Rect((dimensions.Width - image.Width) / 2, (dimensions.Height - image.Height) / 2, image.Width, image.Height);
                }
                else if (renderRatio > aspectRatio)
                {
                    double scaledRenderWidth = (image.Width / image.Height) * dimensions.Width;
                    drawingDimensions = new Rect((dimensions.Width - scaledRenderWidth) / 2, 0, scaledRenderWidth, dimensions.Height);
                }
                else if (renderRatio < aspectRatio)
                {
                    double scaledRenderHeight = (image.Height / image.Width) * dimensions.Height;
                    drawingDimensions = new Rect(0, (dimensions.Height - scaledRenderHeight) / 2, dimensions.Width, scaledRenderHeight);
                }

                var dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawImage(image, drawingDimensions);
                dc.Close();

                var bmp = new RenderTargetBitmap((int)dimensions.Width, (int)dimensions.Height, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(dv);
                bf = BitmapFrame.Create(bmp);
            }

            // Using GDI+ to convert to an HICON.
            // I'd rather not duplicate their code.
            using (MemoryStream memstm = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(bf);
                enc.Save(memstm);

                using (var istm = new ManagedIStream(memstm))
                {
                    // We are not bubbling out GDI+ errors when creating the native image fails.
                    IntPtr bitmap = IntPtr.Zero;
                    try
                    {
                        Status gpStatus = NativeMethods.GdipCreateBitmapFromStream(istm, out bitmap);
                        if (Status.Ok != gpStatus)
                        {
                            return IntPtr.Zero;
                        }

                        IntPtr hicon;
                        gpStatus = NativeMethods.GdipCreateHICONFromBitmap(bitmap, out hicon);
                        if (Status.Ok != gpStatus)
                        {
                            return IntPtr.Zero;
                        }

                        // Caller is responsible for freeing this.
                        return hicon;
                    }
                    finally
                    {
                        Utility.SafeDisposeImage(ref bitmap);
                    }
                }
            }
        }

        public static int RGB(Color c)
        {
            return c.R | (c.G << 8) | (c.B << 16);
        }

        public static string GetHashString(string value)
        {
            byte[] signatureHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(value));
            string signature = signatureHash.Aggregate(
                new StringBuilder(),
                (sb, b) => sb.Append(b.ToString("x2"))).ToString();
            return signature;
        }

        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return LOWORD(lParam.ToInt32());
        }

        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return HIWORD(lParam.ToInt32());
        }

        public static int HIWORD(int i)
        {
            return (short)(i >> 16);
        }

        public static int LOWORD(int i)
        {
            return (short)(i & 0xFFFF);
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool AreImageSourcesEqual(ImageSource left, ImageSource right)
        {
            if (null == left)
            {
                return right == null;
            }
            if (null == right)
            {
                return false;
            }

            BitmapSource leftBmp = GenerateBitmapSource(left);
            BitmapSource rightBmp = GenerateBitmapSource(right);

            byte[] leftPixels = GetBytesFromBitmapSource(leftBmp);
            byte[] rightPixels = GetBytesFromBitmapSource(rightBmp);

            if (leftPixels.Length != rightPixels.Length)
            {
                return false;
            }

            return MemCmp(leftPixels, rightPixels, leftPixels.Length);
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file."),
            SuppressMessage(
                "Microsoft.Security",
                "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")
        ]
        public static bool AreStreamsEqual(Stream left, Stream right)
        {
            if (null == left)
            {
                return right == null;
            }
            if (null == right)
            {
                return false;
            }

            if (!left.CanRead || !right.CanRead)
            {
                throw new NotSupportedException("The streams can't be read for comparison");
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            var length = (int)left.Length;

            // seek to beginning
            left.Position = 0;
            right.Position = 0;

            // total bytes read
            int totalReadLeft = 0;
            int totalReadRight = 0;

            // bytes read on this iteration
            int cbReadLeft = 0;
            int cbReadRight = 0;

            // where to store the read data
            var leftBuffer = new byte[512];
            var rightBuffer = new byte[512];

            // pin the left buffer
            GCHandle handleLeft = GCHandle.Alloc(leftBuffer, GCHandleType.Pinned);
            IntPtr ptrLeft = handleLeft.AddrOfPinnedObject();

            // pin the right buffer
            GCHandle handleRight = GCHandle.Alloc(rightBuffer, GCHandleType.Pinned);
            IntPtr ptrRight = handleRight.AddrOfPinnedObject();

            try
            {
                while (totalReadLeft < length)
                {
                    Assert.AreEqual(totalReadLeft, totalReadRight);

                    cbReadLeft = left.Read(leftBuffer, 0, leftBuffer.Length);
                    cbReadRight = right.Read(rightBuffer, 0, rightBuffer.Length);

                    // verify the contents are an exact match
                    if (cbReadLeft != cbReadRight)
                    {
                        return false;
                    }

                    if (!_MemCmp(ptrLeft, ptrRight, cbReadLeft))
                    {
                        return false;
                    }

                    totalReadLeft += cbReadLeft;
                    totalReadRight += cbReadRight;
                }

                Assert.AreEqual(cbReadLeft, cbReadRight);
                Assert.AreEqual(totalReadLeft, totalReadRight);
                Assert.AreEqual(length, totalReadLeft);

                return true;
            }
            finally
            {
                handleLeft.Free();
                handleRight.Free();
            }
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool GuidTryParse(string guidString, out Guid guid)
        {
            Verify.IsNeitherNullNorEmpty(guidString, "guidString");

            try
            {
                guid = new Guid(guidString);
                return true;
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            // Doesn't seem to be a valid guid.
            guid = default(Guid);
            return false;
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool IsFlagSet(int value, int mask)
        {
            return 0 != (value & mask);
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool IsFlagSet(uint value, uint mask)
        {
            return 0 != (value & mask);
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool IsFlagSet(long value, long mask)
        {
            return 0 != (value & mask);
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static bool IsFlagSet(ulong value, ulong mask)
        {
            return 0 != (value & mask);
        }

        public static bool IsInterfaceImplemented(Type objectType, Type interfaceType)
        {
            Assert.IsNotNull(objectType);
            Assert.IsNotNull(interfaceType);
            Assert.IsTrue(interfaceType.IsInterface);

            return objectType.GetInterfaces().Any(type => type == interfaceType);
        }

        public static bool IsOSVistaOrNewer
        {
            get { return _osVersion >= new Version(6, 0); }
        }

        public static bool IsOSWindows7OrNewer
        {
            get { return _osVersion >= new Version(6, 1); }
        }

        /// <summary>
        /// Simple guard against the exceptions that File.Delete throws on null and empty strings.
        /// </summary>
        /// <param name="path">The path to delete.  Unlike File.Delete, this can be null or empty.</param>
        public static void SafeDeleteFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                File.Delete(path);
            }
        }

        public static void SafeDestroyIcon(ref IntPtr hicon)
        {
            IntPtr p = hicon;
            hicon = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                NativeMethods.DestroyIcon(p);
            }
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static void SafeDispose<T>(ref T disposable) where T : IDisposable
        {
            // Dispose can safely be called on an object multiple times.
            IDisposable t = disposable;
            disposable = default(T);
            if (null != t)
            {
                t.Dispose();
            }
        }

        /// <summary>GDI+'s DisposeImage</summary>
        /// <param name="gdipImage"></param>
        public static void SafeDisposeImage(ref IntPtr gdipImage)
        {
            IntPtr p = gdipImage;
            gdipImage = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                NativeMethods.GdipDisposeImage(p);
            }
        }


        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file."),
            SuppressMessage(
                "Microsoft.Security",
                "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")
        ]
        public static void SafeCoTaskMemFree(ref IntPtr ptr)
        {
            IntPtr p = ptr;
            ptr = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                Marshal.FreeCoTaskMem(p);
            }
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file."),
            SuppressMessage(
                "Microsoft.Security",
                "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")
        ]
        public static void SafeFreeHGlobal(ref IntPtr hglobal)
        {
            IntPtr p = hglobal;
            hglobal = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                Marshal.FreeHGlobal(p);
            }
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file."),
            SuppressMessage(
                "Microsoft.Security",
                "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")
        ]
        public static void SafeRelease<T>(ref T comObject) where T : class
        {
            T t = comObject;
            comObject = default(T);
            if (null != t)
            {
                Assert.IsTrue(Marshal.IsComObject(t));
                Marshal.ReleaseComObject(t);
            }
        }

        /// <summary>
        /// Utility to help classes catenate their properties for implementing ToString().
        /// </summary>
        /// <param name="source">The StringBuilder to catenate the results into.</param>
        /// <param name="propertyName">The name of the property to be catenated.</param>
        /// <param name="value">The value of the property to be catenated.</param>
        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static void GeneratePropertyString(StringBuilder source, string propertyName, string value)
        {
            Assert.IsNotNull(source);
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));

            if (0 != source.Length)
            {
                source.Append(' ');
            }

            source.Append(propertyName);
            source.Append(": ");
            if (string.IsNullOrEmpty(value))
            {
                source.Append("<null>");
            }
            else
            {
                source.Append('\"');
                source.Append(value);
                source.Append('\"');
            }
        }

        /// <summary>
        /// Generates ToString functionality for a struct.  This is an expensive way to do it,
        /// it exists for the sake of debugging while classes are in flux.
        /// Eventually this should just be removed and the classes should
        /// do this without reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <returns></returns>
        [
            Obsolete,
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static string GenerateToString<T>(T @object) where T : struct
        {
            var sbRet = new StringBuilder();
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (0 != sbRet.Length)
                {
                    sbRet.Append(", ");
                }
                Assert.AreEqual(0, property.GetIndexParameters().Length);
                object value = property.GetValue(@object, null);
                string format = null == value ? "{0}: <null>" : "{0}: \"{1}\"";
                sbRet.AppendFormat(format, property.Name, value);
            }
            return sbRet.ToString();
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static void CopyStream(Stream destination, Stream source)
        {
            Assert.IsNotNull(source);
            Assert.IsNotNull(destination);

            destination.Position = 0;

            // If we're copying from, say, a web stream, don't fail because of this.
            if (source.CanSeek)
            {
                source.Position = 0;

                // Consider that this could throw because 
                // the source stream doesn't know it's size...
                destination.SetLength(source.Length);
            }

            var buffer = new byte[4096];
            int cbRead;

            do
            {
                cbRead = source.Read(buffer, 0, buffer.Length);
                if (0 != cbRead)
                {
                    destination.Write(buffer, 0, cbRead);
                }
            }
            while (buffer.Length == cbRead);

            // Reset the Seek pointer before returning.
            destination.Position = 0;
        }

        [
            SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "Shared code file.")
        ]
        public static string HashStreamMD5(Stream stm)
        {
            stm.Position = 0;
            var hashBuilder = new StringBuilder();
            foreach (byte b in MD5.Create().ComputeHash(stm))
            {
                hashBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return hashBuilder.ToString();
        }

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
        }

        public static bool MemCmp(byte[] left, byte[] right, int cb)
        {
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);

            Assert.IsTrue(cb <= Math.Min(left.Length, right.Length));

            // pin this buffer
            GCHandle handleLeft = GCHandle.Alloc(left, GCHandleType.Pinned);
            IntPtr ptrLeft = handleLeft.AddrOfPinnedObject();

            // pin the other buffer
            GCHandle handleRight = GCHandle.Alloc(right, GCHandleType.Pinned);
            IntPtr ptrRight = handleRight.AddrOfPinnedObject();

            bool fRet = _MemCmp(ptrLeft, ptrRight, cb);

            handleLeft.Free();
            handleRight.Free();

            return fRet;
        }

        private class _UrlDecoder
        {
            private readonly Encoding _encoding;
            private readonly char[] _charBuffer;
            private readonly byte[] _byteBuffer;
            private int _byteCount;
            private int _charCount;

            public _UrlDecoder(int size, Encoding encoding)
            {
                _encoding = encoding;
                _charBuffer = new char[size];
                _byteBuffer = new byte[size];
            }

            public void AddByte(byte b)
            {
                _byteBuffer[_byteCount++] = b;
            }

            public void AddChar(char ch)
            {
                _FlushBytes();
                _charBuffer[_charCount++] = ch;
            }

            private void _FlushBytes()
            {
                if (_byteCount > 0)
                {
                    _charCount += _encoding.GetChars(_byteBuffer, 0, _byteCount, _charBuffer, _charCount);
                    _byteCount = 0;
                }
            }

            public string GetString()
            {
                _FlushBytes();
                if (_charCount > 0)
                {
                    return new string(_charBuffer, 0, _charCount);
                }
                return "";
            }
        }

        public static string UrlDecode(string url)
        {
            if (url == null)
            {
                return null;
            }

            var decoder = new _UrlDecoder(url.Length, Encoding.UTF8);
            int length = url.Length;
            for (int i = 0; i < length; ++i)
            {
                char ch = url[i];

                if (ch == '+')
                {
                    decoder.AddByte((byte)' ');
                    continue;
                }
                
                if (ch == '%' && i < length - 2)
                {
                    // decode %uXXXX into a Unicode character.
                    if (url[i + 1] == 'u' && i < length - 5)
                    {
                        int a = _HexToInt(url[i + 2]);
                        int b = _HexToInt(url[i + 3]);
                        int c = _HexToInt(url[i + 4]);
                        int d = _HexToInt(url[i + 5]);
                        if (a >= 0 && b >= 0 && c >= 0 && d >= 0)
                        {
                            decoder.AddChar((char)((a << 12) | (b << 8) | (c << 4) | d));
                            i += 5;

                            continue;
                        }
                    }
                    else
                    {
                        // decode %XX into a Unicode character.
                        int a = _HexToInt(url[i + 1]);
                        int b = _HexToInt(url[i + 2]);

                        if (a >= 0 && b >= 0)
                        {
                            decoder.AddByte((byte)((a << 4) | b));
                            i += 2;

                            continue;
                        }
                    }
                }
                    
                // Add any 7bit character as a byte.
                if ((ch & 0xFF80) == 0)
                {
                    decoder.AddByte((byte) ch);
                }
                else
                {
                    decoder.AddChar(ch);
                }
            }

            return decoder.GetString();
        }

        /// <summary>
        /// Encodes a URL string.  Duplicated functionality from System.Web.HttpUtility.UrlEncode.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <remarks>
        /// Duplicated from System.Web.HttpUtility because System.Web isn't part of the client profile.
        /// URL Encoding replaces ' ' with '+' and unsafe ASCII characters with '%XX'.
        /// Safe characters are defined in RFC2396 (http://www.ietf.org/rfc/rfc2396.txt).
        /// They are the 7-bit ASCII alphanumerics and the mark characters "-_.!~*'()".
        /// This implementation does not treat '~' as a safe character to be consistent with the System.Web version.
        /// </remarks>
        public static string UrlEncode(string url)
        {
            if (url == null)
            {
                return null;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(url);

            bool needsEncoding = false;
            int unsafeCharCount = 0;
            foreach (byte b in bytes)
            {
                if (b == ' ')
                {
                    needsEncoding = true;
                }
                else if (!_UrlEncodeIsSafe(b))
                {
                    ++unsafeCharCount;
                    needsEncoding = true;
                }
            }

            if (needsEncoding)
            {
                var buffer = new byte[bytes.Length + (unsafeCharCount * 2)];
                int writeIndex = 0;
                foreach (byte b in bytes)
                {
                    if (_UrlEncodeIsSafe(b))
                    {
                        buffer[writeIndex++] = b;
                    }
                    else if (b == ' ')
                    {
                        buffer[writeIndex++] = (byte)'+';
                    }
                    else
                    {
                        buffer[writeIndex++] = (byte)'%';
                        buffer[writeIndex++] = _IntToHex((b >> 4) & 0xF);
                        buffer[writeIndex++] = _IntToHex(b & 0xF);
                    }
                }
                bytes = buffer;
                Assert.AreEqual(buffer.Length, writeIndex);
            }

            return Encoding.ASCII.GetString(bytes);
        }

        // HttpUtility's UrlEncode is slightly different from the RFC.
        // RFC2396 describes unreserved characters as alphanumeric or
        // the list "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")"
        // The System.Web version unnecessarily escapes '~', which should be okay...
        // Keeping that same pattern here just to be consistent.
        private static bool _UrlEncodeIsSafe(byte b)
        {
            if (_IsAsciiAlphaNumeric(b))
            {
                return true;
            }

            switch ((char)b)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                //case '~':
                case '*':
                case '\'':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }

        private static bool _IsAsciiAlphaNumeric(byte b)
        {
            return (b >= 'a' && b <= 'z')
                || (b >= 'A' && b <= 'Z')
                || (b >= '0' && b <= '9');
        }

        private static byte _IntToHex(int n)
        {
            Assert.BoundedInteger(0, n, 16);
            if (n <= 9)
            {
                return (byte)(n + '0');
            }
            return (byte)(n - 10 + 'A');
        }

        private static int _HexToInt(char h)
        {
            if (h >= '0' && h <= '9')
            {
                return h - '0';
            }

            if (h >= 'a' && h <= 'f')
            {
                return h - 'a' + 10;
            }

            if (h >= 'A' && h <= 'F')
            {
                return h - 'A' + 10;
            }

            Assert.Fail("Invalid hex character " + h);
            return -1;
        }
    }
}
