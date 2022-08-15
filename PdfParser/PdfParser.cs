using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfParser
{
    static public class PdfParser
    {
        #region Object parser

        static public void ParseObject(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
        {
            while (true)
            {
                // End of object
                if (parseContext.IsObjectEnd(out int endObjLen))
                {
                    // Done
                    parseContext.Skip(endObjLen);
                    break;
                }

                // Dictionary
                if (parseContext.IsDictionaryStart)
                {
                    parseContext.Skip(2); // skip <<
                    var pdfDictionary = ParseDictionary(parseContext, pdfData, pdfObjectId);
                    pdfData.Add(pdfDictionary);

                }
                else if (parseContext.IsStreamStart(out int streamLength))
                {
                    parseContext.Skip(streamLength);
                    var pdfStream = ParseStream(parseContext, pdfData, pdfObjectId);
                    pdfData.Add(pdfStream);
                }
                else if (parseContext.CurrentByte == '[')
                {
                    parseContext.Skip(1);
                    var array = ParseArray(parseContext, pdfData, pdfObjectId);
                    pdfData.Add(array);
                    parseContext.SkipWhiteSpaces();
                }
                else
                {
                    throw new FormatException($"Unknown token {parseContext.GetAsString(16)}");
                }
            }
        }

        #endregion

        #region Basic Pdf elements parsers

        static public PdfObject ParsePdfElement(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
        {
            PdfObject result;

            parseContext.SkipWhiteSpaces();

            if (parseContext.CurrentByte == '/')
            {
                // Name
                parseContext.Skip(1);
                var str = parseContext.ReadNameString();
                result = new PdfString(pdfObjectId, str);
            }
            else if (parseContext.CurrentByte == '(')
            {
                // Paren string
                var str = parseContext.ReadParenString();
                result = new PdfString(pdfObjectId, str);
            }
            else if (parseContext.CurrentByte == '[')
            {
                // array
                parseContext.Skip(1); // skip [
                result = ParseArray(parseContext, pdfData, pdfObjectId);
            }
            else if (parseContext.IsDictionaryStart)
            {
                // Dictionary
                parseContext.Skip(2); // skip <<
                result = ParseDictionary(parseContext, pdfData, pdfObjectId);
            }
            else if (parseContext.IsStreamStart(out int streamLength))
            {
                // Stream
                parseContext.Skip(streamLength);
                result = ParseStream(parseContext, pdfData, pdfObjectId);
            }
            else if (parseContext.IsHexStringStart)
            {
                // Hex string
                parseContext.Skip(1); // skip <
                var data = parseContext.ReadHexString();
                result = new PdfHexString(pdfObjectId, data);
            }
            else if (parseContext.IsObjectRef(out int refId, out int refGen, out int refLength))
            {
                // Object reference
                parseContext.Skip(refLength);
                result = new PdfReference(pdfObjectId, refId, refGen);
            }
            else if (parseContext.IsInteger(0, false, out int intVal, out int intLength))
            {
                // Int value
                parseContext.Skip(intLength);
                if (!parseContext.EOF && parseContext.CurrentByte == '.')
                {
                    // Real value
                    parseContext.Skip(1);
                    if (parseContext.IsInteger(0, false, out int fracVal, out int fracLength))
                    {
                        parseContext.Skip(fracLength);
                        decimal val = (decimal)intVal + ((decimal)fracVal / (decimal)Math.Pow(10, fracLength));
                        result = new PdfReal(pdfObjectId, val);
                    }
                    else
                    {
                        throw new FormatException($"Malformed integer {parseContext.GetAsString(16)}");
                    }
                }
                else
                {
                    result = new PdfInt(pdfObjectId, intVal);
                }
            }
            else if (parseContext.IsBool(out bool val, out int boolLength))
            {
                parseContext.Skip(boolLength);
                result = new PdfBool(pdfObjectId, val);
            }
            else
            {
                throw new FormatException($"Malformed pdf element {parseContext.GetAsString(10)}");
            }

            return result;
        }

        static public PdfDictionary ParseDictionary(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
        {
            var pdfDictionary = new PdfDictionary(pdfObjectId);

            while (true)
            {
                parseContext.SkipWhiteSpaces();

                // End of dictionary
                if (parseContext.IsDictionaryEnd)
                {
                    parseContext.Skip(2); // skip >>
                    parseContext.SkipWhiteSpaces();
                    return pdfDictionary;
                }

                // Read key
                if (parseContext.ReadByte() != '/')
                {
                    throw new FormatException($"Malformed dictionary key {parseContext.GetAsString(16)}");
                }
                var key = parseContext.ReadNameString();

                // Read value
                var value = ParsePdfElement(parseContext, pdfData, pdfObjectId);

                pdfDictionary.Add(key, value);
            }
        }

        static private PdfArray ParseArray(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
        {
            var pdfArray = new PdfArray(pdfObjectId);

            parseContext.SkipWhiteSpaces();

            while (parseContext.CurrentByte != ']')
            {
                var obj = ParsePdfElement(parseContext, pdfData, pdfObjectId);
                pdfArray.Add(obj);
                parseContext.SkipWhiteSpaces();
            }

            parseContext.Skip(1);

            return pdfArray;
        }

        static private PdfStream ParseStream(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
        {
            // Find dictionary for this object id
            //var dic = parseContext.PdfData.PdfObjects.FirstOrDefault(p => pdfObjectId.Equals(p.PdfObjectId) && p is PdfDictionary) as PdfDictionary;
            var dic = pdfData.Find<PdfDictionary>(pdfObjectId);
            PdfStream pdfStream;

            // Find length and filter
            int len = 0;
            bool deflate = false;
            bool image = false;

            foreach (var entry in dic.Entries)
            {
                if (entry.Key == "Length" && entry.Value is PdfInt intLen)
                {
                    len = intLen.Value;
                }
                else if (entry.Key == "Filter" && entry.Value is PdfString filStr)
                {
                    var filter = filStr.Value;
                    if (filter == "FlateDecode")
                    {
                        deflate = true;
                    }
                    else
                    {
                        throw new FormatException($"Unknown filter {filter}");
                    }
                }
                else if (entry.Key == "Filter" && entry.Value is PdfArray filArray)
                {
                    var filterArray = filArray.Values;
                    foreach (var filterObj in filterArray)
                    {
                        if (filterObj is PdfString filterStr)
                        {
                            if (filterStr.Value == "FlateDecode")
                            {
                                deflate = true;
                            }
                            else if (filterStr.Value == "DCTDecode")
                            {
                                // DCTDecode - for now jusst do notthing
                            }
                            else if (filterStr.Value == "CCITTFaxDecode")
                            {
                                // CCITTFaxDecode - do nothing
                            }
                            else
                            {
                                throw new FormatException($"Unknown filter {filterStr.Value}");
                            }
                        }
                    }
                }
                else if (entry.Key == "Subtype" && entry.Value is PdfString subtypeStr && subtypeStr.Value == "Image")
                {
                    image = true;
                }
            }

            // Get the bytes
            byte[] data = parseContext.ReadArray(len);

            // Decompress if needed
            if (deflate && !image)
            {
                // ZZZ Remove ICsharp
                //var inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater(true);
                //inflater.SetInput(data);
                //var inflated = new byte[65536*4];
                //int inflatedLen = inflater.Inflate(inflated);

                var compressedStream = new MemoryStream(data);

                // Drop zlib header
                if (data[0] == 0x78 && ((data[0] * 256 + data[1]) % 31) == 0)
                {
                    compressedStream.ReadByte();
                    compressedStream.ReadByte();
                    if ((data[1] & 0x20) == 0x20) // FDICT
                    {
                        compressedStream.ReadByte();
                        compressedStream.ReadByte();
                        compressedStream.ReadByte();
                        compressedStream.ReadByte();
                    }
                }

                var decompressedStream = new MemoryStream();
                var deflater = new DeflateStream(compressedStream, CompressionMode.Decompress);
                //var deflater = new GZipStream(compressedStream, CompressionMode.Decompress);

                try
                {
                    deflater.CopyTo(decompressedStream);
                    data = decompressedStream.GetBuffer();
                    Console.WriteLine("Success decompress");
                }
                catch (InvalidDataException) 
                {
                    Console.WriteLine("Failed decompress");
                }
            }


            // Add content to pdf data as a string
            string content = null;
            if (!image)
            {
                StringBuilder contentBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    contentBuilder.Append((char)data[i]);
                }

                content = contentBuilder.ToString();

                // ZZZZ
                //Console.WriteLine(content);
                //if (content.IndexOf("BND") >= 0)
                //{
                //    Console.WriteLine("BND");
                //}
                // ZZZ end
            }

            // Finish parsing
            parseContext.SkipCRLF();
            if (!parseContext.IsStreamEnd(out int streamEndLength))
            {
                throw new FormatException($"Expected end of stream got {parseContext.GetAsString(16)}");
            }
            parseContext.Skip(streamEndLength);

            // Create and return object
            pdfStream = new PdfStream(dic, data, content);

            return pdfStream;
        }

        #endregion
        
    }
}
