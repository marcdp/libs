using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class StreamUtils {


        // read text
        public static string ReadText(Stream stream, Encoding? encoding = null) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            var buffer = ReadBytes(stream);
            return encoding.GetString(buffer, 0, buffer.Length);
        }
        public static async Task<string> ReadTextAsync(Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            var buffer = await ReadBytesAsync(stream, cancellationToken);
            return encoding.GetString(buffer, 0, buffer.Length);
        }

        // read text Lines
        public static string[] ReadTextLines(Stream stream, Encoding? encoding = null) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            using (var streamReader = new StreamReader(stream, encoding, false, 4 * 1024, true)) {
                var result = new List<string>();
                do {
                    var line = streamReader.ReadLine();
                    if (line == null) break;
                    result.Add(line);
                } while (true);
                return result.ToArray();
            }
        }
        public static async Task<string[]> ReadTextLinesAsync(Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            using (var streamReader = new StreamReader(stream, encoding, false, 4 * 1024, true)) {
                var result = new List<string>();
                do {
                    string? line = await streamReader.ReadLineAsync();
                    if (line == null) break;
                    result.Add(line);
                    cancellationToken.ThrowIfCancellationRequested();
                } while (true);
                return result.ToArray();
            }
        }

        //// read HttpHeaders
        //public static HttpUtils.HttpHeaders ReadHttpHeaders(Stream stream, Encoding? encoding = null) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    var result = new HttpUtils.HttpHeaders();
        //    var prevName = "";
        //    do {
        //        var line = ReadLine(stream, encoding);
        //        if (line == null || line.Length == 0) break;
        //        if (line.StartsWith(" ") || line.StartsWith("\t")) {
        //            var value = line.Trim();
        //            result[prevName] += value;
        //        } else {
        //            var i = line.IndexOf(":");
        //            if (i != -1) {
        //                var name = line.Substring(0, i);
        //                var value = line.Substring(i + 1).Trim();
        //                result.Add(name, value);
        //                prevName = name;
        //            }
        //        }
        //    } while (true);
        //    return result;
        //}
        //public static async Task<HttpUtils.HttpHeaders> ReadHttpHeadersAsync(Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    var result = new HttpUtils.HttpHeaders();
        //    var prevName = "";
        //    do {
        //        var line = await ReadLineAsync(stream, encoding, cancellationToken);
        //        if (line == null || line.Length == 0) break;
        //        if (line.StartsWith(" ") || line.StartsWith("\t")) {
        //            var value = line.Trim();
        //            result[prevName] += value;
        //        } else {
        //            var i = line.IndexOf(":");
        //            if (i != -1) {
        //                var name = line.Substring(0, i);
        //                var value = line.Substring(i + 1).Trim();
        //                result.Add(name, value);
        //                prevName = name;
        //            }
        //        }
        //    } while (true);
        //    return result;
        //}

        // read bytes
        public static byte[] ReadBytes(Stream stream) {
            var memoryStream = new MemoryStream();
            int nRead;
            var buffer = new byte[4 * 1024];
            do {
                nRead = stream.Read(buffer, 0, buffer.Length);
                if (nRead > 0) memoryStream.Write(buffer, 0, nRead);
            } while (nRead > 0);
            byte[] result = memoryStream.ToArray();
            memoryStream.Dispose();
            return result;
        }
        public static async Task<byte[]> ReadBytesAsync(Stream stream, CancellationToken cancellationToken) {
            var memoryStream = new MemoryStream();
            int nRead;
            var buffer = new byte[4 * 1024];
            do {
                nRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (nRead > 0) {
                    memoryStream.Write(buffer, 0, nRead);
                }
            } while (nRead > 0);
            return memoryStream.ToArray();
        }
        //public static byte[] ReadBytes(Stream stream, int length) {
        //    int nRead = 0;
        //    int tries = 0;
        //    byte[] data = new byte[length];
        //    do {
        //        int iRead = stream.Read(data, nRead, length - nRead);
        //        if (iRead == 0) {
        //            break;
        //        }
        //        nRead += iRead;
        //        if (tries > 5000) {
        //            throw new Exception("Timeout");
        //        }
        //        tries++;
        //    } while (nRead < length);
        //    if (nRead != length) {
        //        throw new Exception("Expects \'" + length + "\' bytes, but received only " + nRead + " from stream.");
        //    }
        //    return data;
        //}
        //public static async Task<byte[]> ReadBytesAsync(Stream stream, int length, CancellationToken cancellationToken) {
        //    int nRead = 0;
        //    byte[] data = new byte[length];
        //    do {
        //        int iRead = await stream.ReadAsync(data, nRead, length - nRead, cancellationToken);
        //        if (iRead == 0) break;
        //        nRead += iRead;
        //    } while (nRead < length);
        //    if (nRead != length) throw new Exception("Expects \'" + length + "\' bytes, but received only " + nRead + " from stream.");
        //    return data;
        //}
        //public static byte[] ReadMaxNBytes(Stream stream, long length) {
        //    var result = new MemoryStream();
        //    do {
        //        int b = stream.ReadByte();
        //        if (b == -1) break;
        //        result.WriteByte((byte)b);
        //    } while (result.Length < length);
        //    return result.ToArray();
        //}


        //read buffer
        public static int FillBuffer(Stream stream, byte[] buffer) {
            int nRead = 0;
            int tries = 0;
            do {
                int iRead = stream.Read(buffer, nRead, buffer.Length - nRead);
                if (iRead == 0) break;
                nRead += iRead;
                if (tries > 5000) throw new Exception("Timeout");
                tries++;
            } while (nRead < buffer.Length);
            return nRead;
        }
        public static async Task<int> FillBufferAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken) {
            //fill buffer
            int nRead = 0;
            do {
                int i = await stream.ReadAsync(buffer, nRead, buffer.Length - nRead, cancellationToken);
                if (i == 0) break;
                nRead += i;                
            } while (nRead < buffer.Length);
            return nRead;
        }
        //public static bool ReadBuffer(Stream stream, byte[] buffer, int offset, int length, byte[] excluded) {
        //    //fill buffer, skiping certain characters (ex: 13, 10)
        //    var nRead = 0;
        //    do {
        //        int iRead = stream.Read(buffer, offset + nRead, length - nRead);
        //        if (iRead == 0) break;
        //        nRead += iRead;
        //    } while (nRead != length);
        //    if (nRead == 0) return false;
        //    for (var i = offset; i < offset + length; i++) {
        //        if (excluded.Contains(buffer[i])) {
        //            for (var k = i; k < offset + length - 1; k++) buffer[k] = buffer[k + 1];
        //            buffer[offset + length - 1] = 0;
        //            nRead--;
        //        }
        //    }
        //    if (nRead != length) {
        //        return ReadBuffer(stream, buffer, offset + nRead, length - nRead, excluded);
        //    }
        //    if (nRead != length) throw new Exception("Error reading from stream, expected bytes, but received less: " + nRead);
        //    return true;
        //}
        //public static async Task<bool> ReadBufferAsync(Stream stream, byte[] buffer, int offset, int length, byte[] excluded, CancellationToken cancellationToken) {
        //    //fill buffer, skip certain characters (ex: 13, 10)
        //    var nRead = 0;
        //    do {
        //        int iRead = await stream.ReadAsync(buffer, offset + nRead, length - nRead, cancellationToken);
        //        if (iRead == 0) break;
        //        nRead += iRead;
        //    } while (nRead != length);
        //    if (nRead == 0) return false;
        //    for (var i = offset; i < offset + length; i++) {
        //        if (excluded.Contains(buffer[i])) {
        //            for (var k = i; k < offset + length - 1; k++) buffer[k] = buffer[k + 1];
        //            buffer[offset + length - 1] = 0;
        //            nRead--;
        //        }
        //    }
        //    if (nRead != length) {
        //        return await ReadBufferAsync(stream, buffer, offset + nRead, length - nRead, excluded, cancellationToken);
        //    }
        //    if (nRead != length) throw new Exception("Error reading from stream, expected bytes, but received less: " + nRead);
        //    return true;
        //}

        //read line
        public static string? ReadLine(Stream stream, Encoding encoding) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            var memoryStream = new MemoryStream();
            int nRead = 0;
            int b = 0;
            do {
                b = stream.ReadByte();
                if (b != 0 && b != -1) {
                    nRead++;
                }
                if (b != 0 && b != -1 && b != 13 && b != '\n') {
                    memoryStream.WriteByte(Convert.ToByte(b));
                }
            } while (b != 0 && b != '\n' && b != -1);
            if (nRead == 0) return null;
            return encoding.GetString(memoryStream.ToArray());
        }         
        //public static string? ReadLine(Stream stream, Encoding? encoding, ref int bytesReaded, char newline = '\n', int maxlength = 0,  bool ctrlCinterpret = false) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    var memoryStream = new MemoryStream();
        //    bytesReaded = 0;
        //    int b = 0;
        //    do {
        //        b = stream.ReadByte();
        //        if (b != 0 && b != -1) {
        //            bytesReaded++;
        //        }
        //        if (b==3 && ctrlCinterpret) {
        //            throw new AbortedException();
        //        }
        //        if (b != 0 && b != -1 && b != 13 && b != newline) {
        //            memoryStream.WriteByte(Convert.ToByte(b));
        //        }
        //        if (maxlength != 0 && memoryStream.Length >= maxlength) break;
        //    } while (b != 0 && b != newline && b != -1);
        //    if (bytesReaded == 0) {
        //        return null;
        //    }
        //    return encoding.GetString(memoryStream.ToArray());
        //}
        public static async Task<string?> ReadLineAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            var memoryStream = new MemoryStream();
            int nRead = 0;            
            var buffer = new byte[1];
            do {
                var i = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
                if (i == 0) break;
                var b = buffer[0];
                nRead += i;
                if (b != 13 && b != '\n') {
                    memoryStream.WriteByte(Convert.ToByte(b));
                }
                if (b == '\n') break;
            } while (true);
            if (nRead == 0) return null;
            return encoding.GetString(memoryStream.ToArray());
        }
        //public static string? ReadTextDelimitedBy(TextReader reader, char[] delimiters) {
        //    var sb = new StringBuilder();
        //    var buffer = new char[1];
        //    int b = 0;
        //    int bPrevious = 0;
        //    do {
        //        b = reader.Peek();
        //        if (b != -1) {
        //            int i = System.Array.IndexOf(delimiters, Convert.ToChar(b));
        //            if (i != -1) {
        //                if (bPrevious == 92 && b == 34) {
        //                    reader.Read(buffer, 0, 1);
        //                    sb.Append(buffer[0]);
        //                } else {
        //                    b = -1;
        //                }
        //            } else {
        //                if (bPrevious == 92 && b == 92) {
        //                    reader.Read(buffer, 0, 1);
        //                    sb.Append(buffer[0]);
        //                    b = -2;
        //                } else {
        //                    reader.Read(buffer, 0, 1);
        //                    sb.Append(buffer[0]);
        //                }
        //            }
        //            bPrevious = b;
        //        }
        //    } while (b != 0 && b != -1);
        //    return sb.ToString();
        //}

        //consume
        public static long Consume(Stream stream, bool dispose = false) {
            long nRead = 0;
            if (stream.ReadByte() != -1) {
                nRead++;
                byte[] buffer = new byte[1024];
                do {
                    int i = stream.Read(buffer, 0, buffer.Length);
                    if (i == 0) break;
                    nRead += i;
                } while (true);
            }
            if (dispose) stream.Dispose();
            return nRead;
        }
        public static async Task<long> ConsumeAsync(Stream stream, bool dispose = false, CancellationToken cancellationToken = default) {
            long nRead = 0;
            var buffer = new byte[1024];
            do {
                int i = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (i == 0) break;
                nRead += i;
            } while (true);
            if (dispose) stream.Dispose();
            return nRead;
        }
        //public static long Consume(Stream stream, long numberOfBytesToRead) {
        //    long nRead = 0;
        //    if (stream.ReadByte() != -1) {
        //        nRead++;
        //        var buffer = new byte[1024];
        //        do {
        //            long pendingBytesToRead = System.Convert.ToInt64(numberOfBytesToRead - nRead);
        //            long numberOfBytesToReadInThiStep = pendingBytesToRead;
        //            if (numberOfBytesToReadInThiStep > buffer.Length) {
        //                numberOfBytesToReadInThiStep = buffer.Length;
        //            }
        //            int i = stream.Read(buffer, 0, (int)numberOfBytesToReadInThiStep);
        //            if (i == 0) break;
        //            nRead += i;
        //        } while (nRead < numberOfBytesToRead);
        //    }
        //    return nRead;
        //}
        //public static async Task<long> ConsumeAsync(Stream stream, long numberOfBytesToRead) {
        //    long nRead = 0;
        //    if (stream.ReadByte() != -1) {
        //        nRead++;
        //        byte[] buffer = new byte[1024];
        //        do {
        //            long pendingBytesToRead = System.Convert.ToInt64(numberOfBytesToRead - nRead);
        //            long numberOfBytesToReadInThiStep = pendingBytesToRead;
        //            if (numberOfBytesToReadInThiStep > buffer.Length) {
        //                numberOfBytesToReadInThiStep = buffer.Length;
        //            }
        //            int i = await stream.ReadAsync(buffer, 0, (int)numberOfBytesToReadInThiStep);
        //            if (i == 0) break;
        //            nRead += i;
        //        } while (nRead < numberOfBytesToRead);
        //    }
        //    return nRead;
        //}
        //public static long Consume(Stream stream, byte[] delimiterToken) {
        //    return Copy(stream, new NullOutputStream(), delimiterToken);
        //}

        //to Partial stream
        //public static Stream ToPartialStream(Stream stream, long offset, long length) {
        //    if (stream.CanSeek) {
        //        stream.Seek(offset, SeekOrigin.Begin);
        //    } else {
        //        Consume(stream, offset);
        //    }
        //    if (length == 0) {
        //        stream.Dispose();
        //        stream = new NullInputStream();
        //    } else if (length == -1) {
        //        return stream;
        //    } else {
        //        stream = new LimitedInputStream(stream, length);
        //    }
        //    return stream;
        //}


        // copy
        public static long Copy(Stream inputStream, Stream outputStream, long bytesToCopy = 0, int bufferSize = 4 * 1024, bool avoidAutoFlush = true) {
            if (bytesToCopy < 0) bytesToCopy = 0;
            long nRead = 0;
            byte[] buffer = new byte[bufferSize];
            int iRead = 0;
            do {
                if (bytesToCopy == 0) {
                    iRead = inputStream.Read(buffer, 0, bufferSize);
                } else {
                    if (nRead + bufferSize > bytesToCopy) {
                        iRead = inputStream.Read(buffer, 0, (int)(bytesToCopy - nRead));
                    } else {
                        iRead = inputStream.Read(buffer, 0, bufferSize);
                    }
                }
                if (iRead > 0) {
                    outputStream.Write(buffer, 0, iRead);
                    if (!avoidAutoFlush) {
                        outputStream.Flush();
                    }
                }
                nRead += iRead;
                if (iRead == 0) {
                    break;
                }
            } while (!((bytesToCopy == 0 && iRead == 0) || (bytesToCopy != 0 && bytesToCopy == nRead)));
            return nRead;
        } 
        public static async Task<long> CopyAsync(Stream inputStream, Stream outputStream, long bytesToCopy = 0, int bufferSize = 4 * 1024, bool avoidAutoFlush = true, CancellationToken cancellationToken = default) {
            if (bytesToCopy < 0) bytesToCopy = 0;
            long nRead = 0;
            var buffer = new byte[bufferSize];
            int iRead = 0;
            do {
                if (bytesToCopy == 0) {
                    iRead = await inputStream.ReadAsync(buffer, 0, bufferSize, cancellationToken);
                } else {
                    if (nRead + bufferSize > bytesToCopy) {
                        iRead = await inputStream.ReadAsync(buffer, 0, (int)(bytesToCopy - nRead), cancellationToken);
                    } else {
                        iRead = await inputStream.ReadAsync(buffer, 0, bufferSize, cancellationToken);
                    }
                }
                if (iRead > 0) {
                    await outputStream.WriteAsync(buffer, 0, iRead, cancellationToken);
                    if (!avoidAutoFlush) {
                        await outputStream.FlushAsync(cancellationToken);
                    }
                }
                nRead += iRead;
                if (iRead == 0) break;
            } while (!((bytesToCopy == 0 && iRead == 0) || (bytesToCopy != 0 && bytesToCopy == nRead)));
            return nRead;
        }
        //public static long Copy(Stream inputStream, Stream outputStream, byte[] boundary, long maxBytesToCopy = 0) {
        //    long nRead = 0;
        //    var buffer = new MemoryStream(boundary.Length);
        //    do {
        //        int i = inputStream.ReadByte();
        //        if (i == -1) break;
        //        byte b = (byte)i;
        //        if (boundary[buffer.Length] == b) {
        //            buffer.WriteByte(b);
        //            if (buffer.Length == boundary.Length) {
        //                break;
        //            }
        //            continue;
        //        }
        //        if (buffer.Length > 0) {
        //            var subBuffer = buffer.ToArray();
        //            var subBytesToCopy = subBuffer.Length;
        //            for (var k = 0; k < subBuffer.Length; k++) {
        //                outputStream.WriteByte(subBuffer[k]);
        //                nRead++;
        //                if (maxBytesToCopy != 0 && nRead == maxBytesToCopy) break;
        //            }
        //            buffer.SetLength(0);
        //        }
        //        outputStream.WriteByte(b);
        //        nRead++;
        //        if (maxBytesToCopy != 0 && nRead == maxBytesToCopy) break;
        //    } while (true);
        //    //return
        //    return nRead;
        //}



        ////write
        //public static void Write(Stream stream, byte[] buffer) {
        //    stream.Write(buffer, 0, buffer.Length);
        //}
        //public static void Write(Stream stream, string text, Encoding? encoding = null) {
        //    if (encoding == null) {
        //        encoding = EncodingUtils.GetDefault();
        //    }
        //    byte[] buffer = encoding.GetBytes(text);
        //    stream.Write(buffer, 0, buffer.Length);
        //}
        //public static void Write(Stream stream, TextReader reader, Encoding? encoding = null) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    var chars = new char[1024];
        //    var buffer = new byte[chars.Length * 4];
        //    do {
        //        int i = reader.ReadBlock(chars, 0, chars.Length);
        //        if (i == 0) break;
        //        int j = encoding.GetBytes(chars, 0, i, buffer, 0);
        //        stream.Write(buffer, 0, j);
        //    } while (true);
        //}
        //public static void WriteLine(Stream stream, string text, Encoding? encoding = null, string? newLine = null) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    if (newLine == null) newLine = System.Environment.NewLine;
        //    byte[] buffer = encoding.GetBytes(text + newLine);
        //    stream.Write(buffer, 0, buffer.Length);
        //}
        //public static void WriteHeaders(Stream stream, NameValueCollection headers, Encoding? encoding = null, string? newLine = null) {
        //    if (encoding == null) encoding = EncodingUtils.GetDefault();
        //    if (newLine == null) newLine = System.Environment.NewLine;
        //    foreach (var key in headers.AllKeys) {
        //        WriteLine(stream, key + ": " + headers.Get(key), encoding, newLine);
        //    }
        //    WriteLine(stream, "", encoding, newLine);
        //}


    }


}


