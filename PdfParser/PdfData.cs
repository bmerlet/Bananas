using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace PdfParser
{
    //
    // All the pdf objects
    //
    public class PdfData
    {
        public PdfData(string file)
        {
            // Read file
            Bytes = File.ReadAllBytes(file);
        }

        public PdfData(byte[] bytes)
        {
            Bytes = bytes;
        }

        // The file content
        public readonly byte[] Bytes;

        // Where xref starts
        public int StartXrefPosition { get; private set; }

        // Trailer info
        public PdfTrailer PdfTrailer { get; private set; }

        // Xref
        private readonly List<PdfXref> pdfXrefs = new List<PdfXref>();
        public IEnumerable<PdfXref> PdfXrefs => pdfXrefs;

        // Derived info
        public PdfDictionary Catalog { get; private set; }
        public PdfDictionary PageTreeRoot { get; private set; }
        public int NumberOfPages { get; private set; }
        public PdfObjectId[] Pages { get; private set; }

        // All the objects
        private readonly List<PdfObject> pdfObjects = new List<PdfObject>();
        public IEnumerable<PdfObject> PdfObjects => pdfObjects;

        public void Parse()
        {
            //
            // Create parse context
            //
            var parseContext = new ParseContext(Bytes);

            //
            // Check header
            //
            var magic = parseContext.GetAsString(7);
            if (magic != "%PDF-1.")
            {
                throw new FormatException($"Bad magic: {magic}");
            }
            parseContext.Skip(7);

            byte ver = parseContext.ReadByte();
            if (ver < '1' || ver > '7')
            {
                throw new FormatException($"Unsupported PDF version: 1.{ver}");
            }
            parseContext.SkipCRLF();

            //
            // Check eof marker
            //
            parseContext.BackwardSkipCRLF();
            string eof = parseContext.BackwardGetAsString(5);
            if (eof != "%%EOF")
            {
                throw new FormatException($"Bad eof marker: {eof}");
            }

            //
            // Find startxref position
            //
            parseContext.BackwardSkip(eof.Length);
            ParseStartXref(parseContext);

            //
            // Parse the trailer
            //
            ParseTrailer(parseContext);

            //
            // Parse the xref
            //
            ParseXref(parseContext);

            //
            // Deal with encryption
            //
            CheckPasswordIfNeeded();

            //
            // Build the pages
            //
            BuildPages();
        }

        private void ParseStartXref(ParseContext parseContext)
        {
            parseContext.BackwardSkipCRLF();
            var startxrefBody = parseContext.BackwardGetBlock("startxref");
            var parseStartXrefBody = new ParseContext(startxrefBody);
            parseStartXrefBody.SkipCRLF();
            var startXrefPdfInt = PdfParser.ParsePdfElement(parseStartXrefBody, null, null) as PdfInt;
            StartXrefPosition = startXrefPdfInt.Value;
        }

        private void ParseTrailer(ParseContext parseContext)
        {
            // Find the trailer marker going backward
            parseContext.BackwardSkipCRLF();
            var trailerBody = parseContext.BackwardGetBlock("trailer");
            var parseTrailer = new ParseContext(trailerBody);
            parseTrailer.SkipCRLF();

            // Parse the trailer dictionary
            var trailerDictionary = PdfParser.ParsePdfElement(parseTrailer, null, null) as PdfDictionary;

            // Look for the size (Number of objects in the xref)
            int size = trailerDictionary.Find<PdfInt>("Size").Value;

            // Look for the catalog node
            var catalog = trailerDictionary.Find<PdfReference>("Root").Value;

            // Look for the info node
            var info = trailerDictionary.Find<PdfReference>("Root").Value;

            // Look for the encrypt node (optional)
            var encrypt = trailerDictionary.Find<PdfReference>("Encrypt")?.Value;

            PdfTrailer = new PdfTrailer(size, catalog, info, encrypt);
        }

        private void ParseXref(ParseContext parseContext)
        {
            // Extract the block we are interested in
            parseContext.BackwardSkipCRLF();
            var xrefBytes = new byte[parseContext.BackwardBytePos - StartXrefPosition];
            for (int i = 0; i < xrefBytes.Length; i++)
            {
                xrefBytes[i] = parseContext.Bytes[StartXrefPosition + i];
            }

            var parseXrefContext = new ParseContext(xrefBytes);
            parseXrefContext.Skip(4); // skip xref
            parseXrefContext.SkipCRLF();

            while (!parseXrefContext.EOF)
            {
                // Read subsection definition
                parseXrefContext.IsInteger(0, true, out int startObj, out int startObjLen);
                parseXrefContext.Skip(startObjLen);
                parseXrefContext.IsInteger(0, true, out int numObj, out int numObjLen);
                parseXrefContext.Skip(numObjLen);

                // Read all entries
                for (int objId = startObj; objId < startObj + numObj; objId++)
                {
                    parseXrefContext.IsInteger(0, true, out int objPos, out int objPosLen);
                    parseXrefContext.Skip(objPosLen);
                    parseXrefContext.IsInteger(0, true, out int objGen, out int objGenLen);
                    parseXrefContext.Skip(objGenLen);

                    if (parseXrefContext.ReadByte() == 'n')
                    {
                        pdfXrefs.Add(new PdfXref(objId, objGen, objPos));
                    }
                    parseXrefContext.SkipWhiteSpaces();
                }
            }
        }

        private void CheckPasswordIfNeeded()
        {
            if (PdfTrailer.Encrypt == null)
            {
                // Not encrypted
                return;
            }

            // ZZZZ
        }

        // Find the Ids of pages, put them in the Pages list
        public void BuildPages()
        {
            // Load and memorize the catalog
            LoadObject(PdfTrailer.Catalog);
            Catalog = Find<PdfDictionary>(PdfTrailer.Catalog);

            //
            // Populate catalog-derived info
            //

            var pageTreeRootRef = Catalog.Find<PdfReference>("Pages");
            LoadObject(pageTreeRootRef.Value);
            PageTreeRoot = Find<PdfDictionary>(pageTreeRootRef.Value);
            NumberOfPages = PageTreeRoot.Find<PdfInt>("Count").Value;
            Pages = new PdfObjectId[NumberOfPages];

            // Traverse the tree to populate the page ids
            int ix = 0;
            PopulatePages(PageTreeRoot, ref ix);
        }

        private void LoadObject(PdfObjectId id)
        {
            if (pdfObjects.FirstOrDefault(po => id.Equals(po.PdfObjectId)) != null)
            {
                // Object already read
                return;
            }

            var xref = PdfXrefs.FirstOrDefault(x => x.PdfObjectId.Equals(id));
            var parseContext = new ParseContext(Bytes, xref.Position);

            if (!parseContext.IsObjectStart(out int objId, out int objGen, out int objLen))
            {
                throw new FormatException($"Not an object start for object {id}, pos {xref.Position}");
            }

            if (objId != id.Id || objGen != id.Gen)
            {
                throw new FormatException($"Not the expected object. Expected {id}, read {objId}/{objGen}, pos {xref.Position}");
            }

            parseContext.Skip(objLen);
            PdfParser.ParseObject(parseContext, this, id);
        }

        // Add an object
        public void Add(PdfObject pdfObject)
        {
            pdfObjects.Add(pdfObject);
        }

        // Find object by id and type
        public T Find<T>(PdfObjectId id)
        {
            foreach(var po in pdfObjects)
            {
                if (id.Equals(po.PdfObjectId) && po is T result)
                {
                    return result;
                }
            }
 
            return default;
        }

        public PdfStream GetContentForPage(int pageIndex)
        {
            return GetContentForPage(Pages[pageIndex]);
        }

        public PdfStream GetContentForPage(PdfObjectId page)
        {
            LoadObject(page);
            var pageDic = Find<PdfDictionary>(page);
            var contentRef = pageDic.Find<PdfReference>("Contents");
            LoadObject(contentRef.Value);
            var contentStream = Find<PdfStream>(contentRef.Value);
            return contentStream;
        }

        public string[] ExtractTextFromPage(int pageNumber)
        {
            var result = new List<string>();
            var content = GetContentForPage(pageNumber);
            var parseContext = new ParseContext(content.Data);

            while (parseContext.SkipToNextTextBlock())
            {
                while (!parseContext.IsEndText)
                {
                    if (parseContext.CurrentByte == '(')
                    {
                        var str = parseContext.ReadParenString();
                        if (parseContext.IsTextOperator2)
                        {
                            result.Add(str);
                            parseContext.Skip(2);
                        }
                        else if (parseContext.IsTextOperator1)
                        {
                            result.Add(str);
                            parseContext.Skip(1);
                        }
                    }
                    else
                    {
                        parseContext.Skip(1);
                    }
                }
            }

            return result.ToArray();
        }


        private void PopulatePages(PdfDictionary pageTreeNode, ref int ix)
        {
            // Go through the kids
            var kids = pageTreeNode.Find<PdfArray>("Kids");
            foreach(var kid in kids.Values)
            {
                if (kid is PdfReference kidref)
                {
                    LoadObject(kidref.Value);
                    var kidDictionary = Find<PdfDictionary>(kidref.Value);
                    var kidType = kidDictionary.Find<PdfString>("Type").Value;
                    if (kidType == "Pages")
                    {
                        // Tree node - recurse
                        PopulatePages(kidDictionary, ref ix);
                    }
                    else if (kidType == "Page")
                    {
                        // Add page
                        Pages[ix++] = kidDictionary.PdfObjectId;
                    }
                    else
                    {
                        throw new FormatException($"Unexpected type while parsing pages: {kidType}");
                    }
                }
            }
        }
    }

    //
    // Pdf trailer info
    //
    public class PdfTrailer
    {
        public PdfTrailer(int size, PdfObjectId catalog, PdfObjectId info, PdfObjectId encrypt) =>
            (Size, Catalog, Info, Encrypt) = (size, catalog, info, encrypt);

        public readonly int Size;
        public readonly PdfObjectId Catalog;
        public readonly PdfObjectId Info;
        public readonly PdfObjectId Encrypt;
    }

    //
    // Pdf array
    //
    public class PdfArray : PdfObject
    {
        // Constructor
        public PdfArray(PdfObjectId pdfObjectId) : base(pdfObjectId) { }

        // Array
        private readonly List<PdfObject> values = new List<PdfObject>();
        public IEnumerable<PdfObject> Values => values;

        // Add an entry
        public void Add(PdfObject value)
        {
            values.Add(value);
        }
    }

    //
    // Pdf dictionary
    //
    public class PdfDictionary : PdfObject
    {
        // Constructor
        public PdfDictionary(PdfObjectId pdfObjectId) : base(pdfObjectId) { }

        // List of entries
        private readonly List<PdfDictionaryEntry> entries = new List<PdfDictionaryEntry>();
        public IEnumerable<PdfDictionaryEntry> Entries => entries;

        // Add an entry
        public void Add(string key, PdfObject value)
        {
            entries.Add(new PdfDictionaryEntry(key, value));
        }

        // Find object by key and type
        public T Find<T>(string key)
        {
            foreach (var de in entries)
            {
                if (de.Key == key && de.Value is T result)
                {
                    return result;
                }
            }

            return default;
        }

        // Entry itself
        public class PdfDictionaryEntry
        {
            public PdfDictionaryEntry(string key, PdfObject value) => (Key, Value) = (key, value);
            public readonly string Key;
            public readonly PdfObject Value;
        }
    }

    //
    // Pdf Stream
    //
    public class PdfStream : PdfObject
    {
        public PdfStream(PdfDictionary dictionary, byte[] data, string content) 
            : base(dictionary.PdfObjectId) 
            => (Dictionary, Data, Content) = (dictionary, data, content);

        // Associated dictionary 
        public readonly PdfDictionary Dictionary;

        // Content in byte array form
        public readonly byte[] Data;

        // Content in string form (may be null)
        public readonly string Content;
    }

    //
    // Pdf object reference
    //
    public class PdfReference : PdfObject
    {
        // Constructor
        public PdfReference(PdfObjectId pdfObjectId, int id, int gen) : base(pdfObjectId)
        {
            Value = new PdfObjectId(id, gen);
        }

        // Reference
        public readonly PdfObjectId Value;
    }

    //
    // Pdf string
    //
    public class PdfString : PdfObject
    {
        // Constructor
        public PdfString(PdfObjectId pdfObjectId, string value) : base(pdfObjectId) => Value = value;

        // string value
        public readonly string Value;
    }

    //
    // Pdf hex string
    //
    public class PdfHexString : PdfObject
    {
        // Constructor
        public PdfHexString(PdfObjectId pdfObjectId, byte[] value) : base(pdfObjectId) => Value = value;

        // Byte array
        public readonly byte[] Value;
    }

    //
    // Pdf boolean
    //
    public class PdfBool : PdfObject
    {
        // Constructor
        public PdfBool(PdfObjectId pdfObjectId, bool value) : base(pdfObjectId) => Value = value;

        // Bool value
        public readonly bool Value;
    }

    //
    // Pdf integer
    //
    public class PdfInt : PdfObject
    {
        // Constructor
        public PdfInt(PdfObjectId pdfObjectId, int value) : base(pdfObjectId) => Value = value;

        // Int value
        public readonly int Value;
    }

    //
    // Pdf real
    //
    public class PdfReal : PdfObject
    {
        // Constructor
        public PdfReal(PdfObjectId pdfObjectId, decimal value) : base(pdfObjectId) => Value = value;

        // Int value
        public readonly decimal Value;
    }

    //
    // Base class for all Pdf objects
    //
    public class PdfObject
    {
        public PdfObject(PdfObjectId pdfObjectId) => PdfObjectId = pdfObjectId;

        // Object id (may be null)
        public readonly PdfObjectId PdfObjectId;
    }

    //
    // Object xref
    //
    public class PdfXref
    {
        public PdfXref(int id, int gen, int pos)
        {
            PdfObjectId = new PdfObjectId(id, gen);
            Position = pos;
        }

        // Object id
        public readonly PdfObjectId PdfObjectId;

        // Position in file
        public readonly int Position;
    }

    //
    // Object Id ("<id> <gen> obj" ... "objend")
    //
    public class PdfObjectId
    {
        public PdfObjectId(int id, int gen) => (Id, Gen) = (id, gen);
        public readonly int Id;
        public readonly int Gen;

        public override bool Equals(object obj)
        {
            return obj is PdfObjectId o && Id == o.Id && Gen == o.Gen;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + Gen.GetHashCode();
        }
        public override string ToString()
        {
            return $"{Id}/{Gen}";
        }
    }
}
