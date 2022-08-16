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

        // Encryption info
        public byte[] RC4Key { get; private set; }

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

            // Look for ID (mandatory with encryption, optional otherwise
            var id = trailerDictionary.Find<PdfArray>("ID")?.Values;

            PdfTrailer = new PdfTrailer(size, catalog, info, encrypt, id);
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

        private void CheckPasswordIfNeeded(string password = "")
        {
            byte[] padding = new byte[]
            {
                0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
                0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
            };

            // Check if file encrypted
            if (PdfTrailer.Encrypt == null)
            {
                // Not encrypted
                return;
            }

            // Load encryption info
            LoadObject(PdfTrailer.Encrypt);
            var encryptDic = Find<PdfDictionary>(PdfTrailer.Encrypt);

            // We support only "Standard"
            if (encryptDic.Find<PdfString>("Filter")?.Value != "Standard")
            {
                throw new FormatException("Unknown encryption");
            }

            // We support only version 1
            if (encryptDic.Find<PdfInt>("V").Value != 1)
            {
                throw new FormatException("Only support version 1 of the standard encryption");
            }

            if (encryptDic.Find<PdfInt>("R").Value != 2)
            {
                throw new FormatException("Only support revision 2 of the standard encryption");
            }

            // Get user and owner passwords
            var userPasswordStr = encryptDic.Find<PdfString>("U").Value;
            var userPassword = new byte[userPasswordStr.Length];
            for(int i = 0; i < userPassword.Length; i++)
            {
                userPassword[i] = (byte)userPasswordStr[i];
            }

            var ownerPassword = encryptDic.Find<PdfString>("O").Value;

            // Get the flags
            uint flags = (uint)encryptDic.Find<PdfInt>("P").Value;

            // Compute the encryption key (Algorithm 3.2, p125 of PDF spec)
            var buf = new List<byte>();

            // (1) Add password (up to 32 bytes)...
            for(int i = 0; i < 32 && i < password.Length; i++)
            {
                buf.Add((byte)password[i]);
            }
            // ... and pad with specific bytes
            for (int i = 0; i < 32 && buf.Count < 32; i++)
            {
                buf.Add(padding[i]);
            }

            // (2) Init MD5
            // (3) Add the owner password
            for(int i = 0; i < ownerPassword.Length; i++)
            {
                buf.Add((byte)ownerPassword[i]);
            }

            // (4) Add the flags as an uint, low-order bits first
            buf.Add((byte)(flags & 0xff));
            buf.Add((byte)((flags >> 8 ) & 0xff));
            buf.Add((byte)((flags >> 16) & 0xff));
            buf.Add((byte)((flags >> 24) & 0xff));

            // (5) First element of the ID array in the trailer
            foreach(PdfHexString str in PdfTrailer.ID)
            {
                for(int i = 0; i < str.Value.Length; i++)
                {
                    buf.Add(str.Value[i]);
                }
                break;
            }

            // (6) For revison 4 (of what?) and higher, if metadata is not encrypted, pass ffffffff
            for(int i = 0; i < 4; i++)
            {
                buf.Add((byte)0xff);
            }

            // (7) Compute the MD5 hash
            var md5 = new System.Security.Cryptography.MD5Cng();
            var hash = md5.ComputeHash(buf.ToArray());

            // (8) for version 3 and above...
            // (9) Take first 5 bytes as the key
            var key = new byte[5];
            for(int i = 0; i < 5; i++)
            {
                key[i] = hash[i];
            }

            //
            // Password validation (algorithm 3.6)
            //
            // 3.6 (1) Perform all but the last step of algorithm 3.4
            //   3.4 (1) Create an encryption key using algorithm 3.2 (done above)
            //   3.4 (2) RC4-Encrypt padding from 3.1 with the key
            var encryptedPadding = RC4.Encrypt(key, padding);

            // 3.6 (2) Compare result to user password
            bool match = true;
            for (int i = 0; i < encryptedPadding.Length; i++)
            {
                if (encryptedPadding[i] != userPassword[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                RC4Key = key;
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
        public PdfTrailer(int size, PdfObjectId catalog, PdfObjectId info, PdfObjectId encrypt, IEnumerable<PdfObject> id) =>
            (Size, Catalog, Info, Encrypt, ID) = (size, catalog, info, encrypt, id);

        public readonly int Size;
        public readonly PdfObjectId Catalog;
        public readonly PdfObjectId Info;
        public readonly PdfObjectId Encrypt;
        public readonly IEnumerable<PdfObject> ID;
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
