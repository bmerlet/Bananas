using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfParser
{
    public class PdfParser
    {
        #region Main parser

        public PdfData Parse(string file)
        {
            var pdfData = new PdfData();
            var bytes = File.ReadAllBytes(file);

            // Get and check header
            string magic = System.Text.Encoding.UTF8.GetString(bytes, 0, 7);
            if (magic != "%PDF-1.")
            {
                throw new FormatException($"Bad magic: {magic}");
            }

            char ver = (char)bytes[7];
            if (ver < '1' || ver > '5')
            {
                throw new FormatException($"Unsupported PDF version: 1.{ver}");
            }

            var parseContext = new ParseContext(bytes);
            parseContext.Skip(9);

            while (true)
            {
                if (parseContext.IsCommentStart)
                {
                    Console.WriteLine($"Found comment");
                    parseContext.SkipUntilCRLF();
                }
                else if (parseContext.IsObjectStart(out int objId, out int objGen, out int objLen))
                {
                    Console.WriteLine($"Found object {objId}/{objGen}");

                    var pdfObjectId = new PdfObjectId(objId, objGen);

                    parseContext.Skip(objLen);
                    ReadObject(parseContext, pdfData, pdfObjectId);
                }
                //else if (parseContext.IsDictionaryStart)
                //{
                //    Console.WriteLine($"Found dictionary");
                //    ParseDictionary(parseContext);
                //}
                else if (parseContext.IsXrefStart(out int xrefLength))
                {
                    Console.WriteLine($"Found xref start - looking for trailer");
                    parseContext.Skip(xrefLength);

                    while (!parseContext.IsTrailerStart(out xrefLength))
                    {
                        parseContext.Skip(1);
                    }
                    Console.WriteLine($"Found trailer");
                    parseContext.Skip(xrefLength);
                    ParseTrailer(parseContext, pdfData);
                    break;
                }
                else
                {
                    throw new FormatException($"Unknown token {parseContext.GetAsString(16)}");
                }
            }

            return pdfData;
        }

        private void ReadObject(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
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
                else
                {
                    throw new FormatException($"Unknown token {parseContext.GetAsString(16)}");
                }
            }
        }

        private PdfDictionary ParseDictionary(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
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

        private PdfObject ParsePdfElement(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
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
                result = new PdfInt(pdfObjectId, intVal);
            }
            else
            {
                throw new FormatException($"Malformed pdf element {parseContext.GetAsString(10)}");
            }

            return result;
        }

        private PdfArray ParseArray(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
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

        private PdfStream ParseStream(ParseContext parseContext, PdfData pdfData, PdfObjectId pdfObjectId)
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
            if (deflate)
            {
                var compressedStream = new MemoryStream(data);

                // Drop zlib header
                compressedStream.ReadByte();
                compressedStream.ReadByte();

                var decompressedStream = new MemoryStream();
                var deflater = new DeflateStream(compressedStream, CompressionMode.Decompress);
                //var deflater = new GZipStream(compressedStream, CompressionMode.Decompress);
                deflater.CopyTo(decompressedStream);

                data = decompressedStream.GetBuffer();
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

        private void ParseTrailer(ParseContext parseContext, PdfData pdfData)
        {
            // Parse the trailer dictionary
            var trailerDictionary = ParsePdfElement(parseContext, pdfData, null) as PdfDictionary;

            // Look for the size (Number of objects in the xref)
            int size = trailerDictionary.Find<PdfInt>("Size").Value;

            // Look for the catalog node
            var catalog = trailerDictionary.Find<PdfReference>("Root").Value;

            // Look for the info node
            var info = trailerDictionary.Find<PdfReference>("Root").Value;

            pdfData.SetPdfTrailer(size, catalog, info);
        }

        #endregion
    }
}
