using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

//
// Copyright 2026 Benoit Merlet
//

namespace PdfParser
{
    //
    // All the pdf objects
    //
    public class PdfData
    {
        #region Constructors

        public PdfData(string file)
        {
            // Read file
            Bytes = File.ReadAllBytes(file);
        }

        public PdfData(byte[] bytes)
        {
            Bytes = bytes;
        }

        #endregion

        #region Public properties

        // The file content
        public readonly byte[] Bytes;

        // PDF minor version (e.g. 4 for 1.4)
        public int PdfMinorVersion { get; private set; }

        // Where xref starts
        public int StartXrefPosition { get; private set; }

        // Trailer info
        public PdfTrailer Trailer { get; private set; }

        // Encryption info
        public PdfCrypto Crypto { get; private set; }
        public byte[] EncryptionKey { get; private set; }

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

        #endregion

        #region Main parsing

        public void Parse(string password = "")
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

            // Remember version
            PdfMinorVersion = ver;

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
            if (Trailer.Encrypt != null)
            {
                // Load the encryption info into Crypto
                LoadObject(Trailer.Encrypt);
                var encryptDic = Find<PdfDictionary>(Trailer.Encrypt);
                Crypto = new PdfCrypto(encryptDic);

                // ZZZZZ
                //ComputeUserPasswordHash("");
                //ComputeOwnerPasswordHash("", "");

                // Check the passed-in password
                CheckPassword(password);
            }

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

            Trailer = new PdfTrailer(size, catalog, info, encrypt, id);
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

        #endregion

        #region Crypto

        //
        // Encrypt/decrypt a set of bytes
        //
        public byte[] Encrypt(byte[] data, PdfObjectId id)
        {
            if (EncryptionKey == null)
            {
                return data;
            }

            // Decide if using AES
            // ZZZ If I understand correctly AES is only used in "crypt filters"
            //bool usingAES = PdfMinorVersion >= 6;

            // Algorithm 3.1, p119
            // (1) obtain oject id and gen: they are in the "id" variable
            
            // (2) Extend the encryption key...
            var extKey = new List<byte>();
            extKey.AddRange(EncryptionKey);
            
            // ... by adding 3 bytes of the object number, lsb first...
            extKey.Add((byte)(id.Id & 0xff));
            extKey.Add((byte)((id.Id >> 8) & 0xff));
            extKey.Add((byte)((id.Id >> 16) & 0xff));
            
            // ... and 2 bytes of the gen number, lsb first...
            extKey.Add((byte)(id.Gen & 0xff));
            extKey.Add((byte)((id.Gen >> 8) & 0xff));
            
            // And some salt if we are using AES
            //if (usingAES)
            //{
            //    extKey.Add(0x73);
            //    extKey.Add(0x41);
            //    extKey.Add(0x6C);
            //    extKey.Add(0x54);
            //}

            // (3) Compute MD5 hash
            var md5 = new System.Security.Cryptography.MD5Cng();
            var hash = md5.ComputeHash(extKey.ToArray());

            // (4) Use the first EncryptionKey.Length + 5 bytes (up to 16) as the RC4/AES key
            var key = new byte[Math.Min(16, EncryptionKey.Length + 5)];
            for(int i = 0; i < key.Length; i++)
            {
                key[i] = hash[i];
            }

            //if (usingAES)
            //{
            //    var aes = new System.Security.Cryptography.AesCng();
            //    aes.Key = key;
            //    var decryptor = aes.CreateDecryptor();
            //    data = decryptor.TransformFinalBlock(data, 0, data.Length);
            //}
            //else
            {
                data = RC4.Encrypt(key, data);
            }

            // Debug
            //var sb = new System.Text.StringBuilder();
            //foreach (var b in data) sb.Append((char)b);
            //var str = sb.ToString();
            //Console.WriteLine(str);


            return data;
        }

        //
        // Padding, as defined in step 1 of algorithm 3.2 (p125 of PDF spec)
        //
        private readonly byte[] padding = new byte[]
        {
                0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
                0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
        };

        //
        // Pad password
        //
        private byte[] PadPassword(string password)
        {
            var buf = new byte[32];
            int i;

            // Add password (up to 32 bytes)...
            for (i = 0; i < 32 && i < password.Length; i++)
            {
                buf[i] = (byte)password[i];
            }

            // ... and pad with padding bytes
            for (int j = 0; i < 32; i++)
            {
                buf[i] = padding[j++];
            }

            return buf;
        }

        //
        // Compute encryption key
        //
        // Algorithm 3.2, p125 of PDF spec
        private byte[] ComputeEncryptionKey(string password)
        {
            // Buffer
            var buf = new List<byte>();

            // (1) Add padded password
            buf.AddRange(PadPassword(password));

            // (2) Init MD5
            // (3) Add the owner password
            for (int i = 0; i < Crypto.OwnerPassword.Length; i++)
            {
                buf.Add((byte)Crypto.OwnerPassword[i]);
            }

            // (4) Add the flags as an uint, low-order bits first
            buf.Add((byte)(Crypto.Flags & 0xff));
            buf.Add((byte)((Crypto.Flags >> 8) & 0xff));
            buf.Add((byte)((Crypto.Flags >> 16) & 0xff));
            buf.Add((byte)((Crypto.Flags >> 24) & 0xff));

            // (5) First element of the ID array in the trailer
            foreach (PdfHexString str in Trailer.ID)
            {
                for (int i = 0; i < str.Value.Length; i++)
                {
                    buf.Add(str.Value[i]);
                }
                break;
            }

            // (6) For revision 4 (of what?) and higher, if metadata is not encrypted, pass ffffffff
            if (Crypto.Revision >= 4) // ZZZZ Should parse "EncryptMetadata" bool in PdfCrypto, default is TRUE see p123 
            {
                for (int i = 0; i < 4; i++)
                {
                    buf.Add((byte)0xff);
                }
            }

            // (7) Compute the MD5 hash
            var md5 = new System.Security.Cryptography.MD5Cng();
            var hash = md5.ComputeHash(buf.ToArray());

            // (8) for version 3 and above...
            // RFU

            // (9) Take first 5 bytes as the key
            var key = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                key[i] = hash[i];
            }

            return key;
        }

        private void CheckPassword(string password)
        {
            //
            // Password validation (algorithm 3.6)
            //
            // 3.6 (1) Perform all but the last step of algorithm 3.4
            //   3.4 (1) Create an encryption key using algorithm 3.2
            var key = ComputeEncryptionKey(password);

            //   3.4 (2) RC4-Encrypt padding from 3.1 with the key
            var encryptedPadding = RC4.Encrypt(key, padding);

            // 3.6 (2) Compare result to user password
            bool match = true;
            for (int i = 0; i < encryptedPadding.Length; i++)
            {
                if (encryptedPadding[i] != Crypto.UserPassword[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                EncryptionKey = key;
            }
            else
            {
                throw new FormatException("Password does not match");
            }
        }

        // Algorithm 3.3
#pragma warning disable IDE0051 // Remove unused private members
        private byte[] ComputeOwnerPasswordHash(string ownerPassword, string userPassword)
#pragma warning restore IDE0051 // Remove unused private members
        {
            // (1) Get padded owner password
            var buf = PadPassword(ownerPassword);

            // (2) Get MD5 hash
            var md5 = new System.Security.Cryptography.MD5Cng();
            var hash = md5.ComputeHash(buf);

            // (3) Revision 3 or greater...

            // (4) use first 5 bytes of the MD5 as key 
            var key = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                key[i] = hash[i];
            }

            // (5) get passed user password
            buf = PadPassword(userPassword);

            // (6) encrypt user password using key from 4
            var o = RC4.Encrypt(key, buf);

            // (7) Revision 3 or greater...

            // (8)Store O as owner password
            return o;
        }

        // Algorithm 3.4
#pragma warning disable IDE0051 // Remove unused private members
        private byte[] ComputeUserPasswordHash(string userPassword)
#pragma warning restore IDE0051 // Remove unused private members
        {
            // (1) Get encryption key
            var key = ComputeEncryptionKey(userPassword);

            // (2) encrypt padding using key from (1)
            var u = RC4.Encrypt(key, padding);

            // (3) Store as U
            return u;
        }

        #endregion

        #region Page tree

        //
        // Find the Ids of pages, put them in the Pages list
        //
        private void BuildPages()
        {
            // Load and memorize the catalog
            LoadObject(Trailer.Catalog);
            Catalog = Find<PdfDictionary>(Trailer.Catalog);

            // Populate Pages
            var pageTreeRootRef = Catalog.Find<PdfReference>("Pages");
            LoadObject(pageTreeRootRef.Value);
            PageTreeRoot = Find<PdfDictionary>(pageTreeRootRef.Value);
            NumberOfPages = PageTreeRoot.Find<PdfInt>("Count").Value;
            Pages = new PdfObjectId[NumberOfPages];

            // Traverse the tree to populate the page ids
            int ix = 0;
            PopulatePages(PageTreeRoot, ref ix);
        }

        // Recursively find the pages
        private void PopulatePages(PdfDictionary pageTreeNode, ref int ix)
        {
            // Go through the kids
            var kids = pageTreeNode.Find<PdfArray>("Kids");
            foreach (var kid in kids.Values)
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

        #endregion

        #region Page content

        // Get content for a page
        public IEnumerable<PdfStream> GetContentForPage(int pageIndex, out List<PdfDictionary> fontsWithUnicodeMapping)
        {
            return GetContentForPage(Pages[pageIndex], out fontsWithUnicodeMapping);
        }

        public IEnumerable<PdfStream> GetContentForPage(PdfObjectId page, out List<PdfDictionary> fontsWithUnicodeMapping)
        {
            var streams = new List<PdfStream>();
            fontsWithUnicodeMapping = new List<PdfDictionary>();

            // Load the page object
            LoadObject(page);

            // Find and load the "Content" object
            var pageDic = Find<PdfDictionary>(page);
            var contentRef = pageDic.Find<PdfReference>("Contents");
            LoadObject(contentRef.Value);

            // Add the content object's stream to the list
            streams.Add(Find<PdfStream>(contentRef.Value));

            // If we have a resources dictionary...
            if (pageDic.Find<PdfDictionary>("Resources") is PdfDictionary resourceDic)
            {
                // ... that contains an XObject dictionary...
                if (resourceDic.Find<PdfDictionary>("XObject") is PdfDictionary xobjectDic)
                {
                    // ... then go throught the XObjects to find form ones with content streams
                    foreach(var entry in xobjectDic.Entries)
                    {
                        var stream = LookForFormXObjectWithContent(entry.Key, entry.Value);
                        if (stream != null)
                        {
                            streams.Add(stream);
                            // ZZZZ
                            //Console.WriteLine($"=== {entry.Key} === {stream.Content}");
                            //if (entry.Key == "X21")
                            //{
                            //    System.Diagnostics.Debugger.Break();
                            //    LoadObject(new PdfObjectId(273, 0));
                            //}
                        }
                    }
                }

                // Also look for fonts with a ToUnicode entry
                if (resourceDic.Find<PdfDictionary>("Font") is PdfDictionary fontRefDic)
                {
                    foreach(var entry in fontRefDic.Entries)
                    {
                        if (entry.Value is PdfReference fontRef)
                        {
                            LoadObject(fontRef.Value);
                            var fontDic = Find<PdfDictionary>(fontRef.Value);
                            if (fontDic.Find<PdfReference>("ToUnicode") is PdfReference toUnicodeRef)
                            {
                                LoadObject(toUnicodeRef.Value);
                                fontsWithUnicodeMapping.Add(fontDic);
                            }
                        }
                    }
                }
            }
            // All done
            return streams;
        }

        private PdfStream LookForFormXObjectWithContent(string xobjectName, PdfObject obj)
        {
            if (obj is PdfReference reference)
            {
                LoadObject(reference.Value);
                var xobjectDic = Find<PdfDictionary>(reference.Value);
                if (xobjectDic.Find<PdfString>("Subtype").Value == "Form")
                {
                    return Find<PdfStream>(reference.Value);
                }
            }
            return null;
        }

        #endregion

        #region Text extraction

        public string[] ExtractTextFromPage(int pageNumber)
        {
            var result = new List<string>();
            var contents = GetContentForPage(pageNumber, out List<PdfDictionary> fontsWithUnicodeMapping);
            var tokens = new List<string>();
            foreach (var content in contents)
            {
                var parseContext = new ParseContext(content.Data);

                // Look for BT/ET (Begin text/End Text) blocks 
                while (parseContext.SkipToNextTextBlock())
                {
                    byte[] toUnicodeCMap = null;

                    while (!parseContext.IsEndText)
                    {
                        // Parse token
                        string token = null;
                        if (parseContext.CurrentByte == '(')
                        {
                            token = parseContext.ReadParenString();
                        }
                        else if (parseContext.CurrentByte == '/')
                        {
                            parseContext.Skip(1);
                            token = parseContext.ReadNameString();
                        }
                        else if (parseContext.CurrentByte == '[')
                        {
                            var array = PdfParser.ParsePdfElement(parseContext, null, null) as PdfArray;
                            foreach(var a in array.Values)
                            {
                                if (a is PdfString str)
                                {
                                    token = str.Value;
                                }
                            }
                            parseContext.SkipWhiteSpaces();
                        }
                        else
                        {
                            var sb = new StringBuilder();
                            while(!parseContext.IsWhiteSpace(0))
                            {
                                sb.Append((char)parseContext.ReadByte());
                            }
                            token = sb.ToString();
                            parseContext.SkipWhiteSpaces();
                        }
                        if (token == null)
                        {
                            continue;
                        }

                        // Is the token an operator?
                        bool isOperator = true;
                        if (token == "Tf")
                        {
                            // Font operator
                            if (tokens.Count >= 2)
                            {
                                toUnicodeCMap = null;
                                string fontName = tokens[tokens.Count - 2];
                                foreach(var mappingFont in fontsWithUnicodeMapping)
                                {
                                    if (mappingFont.Find<PdfString>("Name").Value == fontName)
                                    {
                                        var toUnicodeRef = mappingFont.Find<PdfReference>("ToUnicode").Value;
                                        toUnicodeCMap = Find<PdfStream>(toUnicodeRef).Data;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (token == "'" || token == "\"" || token == "Tj" || token == "TJ")
                        {
                            // Show text operator
                            if (tokens.Count >= 1)
                            {
                                var text = tokens[tokens.Count - 1];
                                if (toUnicodeCMap != null)
                                {
                                    text = MapToUnicode(text, toUnicodeCMap);
                                }
                                result.Add(text);
                            }

                        }
                        else
                        {
                            isOperator = false;
                        }

                        if (isOperator)
                        {
                            tokens.Clear();
                        }
                        else
                        {
                            tokens.Add(token);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private string MapToUnicode(string text, byte[] cmap)
        {
            var dic = new Dictionary<int, int>();

            // Parse the cmap roughly
            var parseContext = new ParseContext(cmap);
            while(parseContext.GetAsString(11) != "beginbfchar")
            {
                parseContext.Skip(1);
            }
            parseContext.Skip(11);
            parseContext.SkipWhiteSpaces();
            while (parseContext.GetAsString(9) != "endbfchar")
            {
                var charInString = (PdfParser.ParsePdfElement(parseContext, null, null) as PdfHexString).Value[0];
                var unicodeChar = (PdfParser.ParsePdfElement(parseContext, null, null) as PdfHexString).Value;
                dic.Add(charInString, unicodeChar[0] * 256 + unicodeChar[1]);
                parseContext.SkipCRLF();
            }

            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                int c = text[i];
                if (dic.ContainsKey(c))
                {
                    sb.Append((char)dic[c]);
                }
                else
                {
                    sb.Append((char)c);
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Utilities

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

        #endregion

        #region Supporting classes

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
        // Pdf encryption info
        //
        public class PdfCrypto
        {
            public PdfCrypto(PdfDictionary dic)
            {
                // Get the filter
                Filter = dic.Find<PdfString>("Filter")?.Value;

                // We support only "Standard"
                if (Filter != "Standard")
                {
                    throw new FormatException($"Unknown encryption filter {Filter}");
                }

                // Get the version
                Version = dic.Find<PdfInt>("V").Value;

                // We support only version 1
                if (Version != 1)
                {
                    throw new FormatException("Only support version 1 of the standard security handler");
                }

                // Get key length (optional)
                var KeyLengthInt = dic.Find<PdfInt>("Length");
                KeyLength = (KeyLengthInt == null) ? 40 : KeyLengthInt.Value;
                if (KeyLength != 40)
                {
                    throw new FormatException("Only support key length of 40 bits for standard security handler");
                }

                // Get the revision of the standard security handler
                Revision = dic.Find<PdfInt>("R").Value;
                if (Revision != 2)
                {
                    throw new FormatException("Only support revision 2 of the standard security handler");
                }

                // Get the owner password hash
                OwnerPassword = FindPasswordHash(dic, "O");

                // Get the user password hash
                UserPassword = FindPasswordHash(dic, "U");

                // Get the flags
                Flags = (uint)dic.Find<PdfInt>("P").Value;
            }

            private byte[] FindPasswordHash(PdfDictionary dic, string key)
            {
                byte[] password;

                var passwordHexStr = dic.Find<PdfHexString>(key);
                if (passwordHexStr != null)
                {
                    password = passwordHexStr.Value;
                }
                else
                {
                    var passwordStr = dic.Find<PdfString>(key);
                    if (passwordStr != null)
                    {
                        password = new byte[passwordStr.Value.Length];
                        for (int i = 0; i < password.Length; i++)
                        {
                            password[i] = (byte)passwordStr.Value[i];
                        }
                    }
                    else
                    {
                        throw new FormatException("Cannot find owner password hash in encryption dictionary");
                    }
                }

                if (password.Length != 32)
                {
                    throw new FormatException($"{key} password hash is {password.Length} bytes instead of 32");
                }

                return password;
            }

            // Filter used (always "Standard")
            public readonly string Filter;

            // Encryption/decryption version (only "1" supported so far)
            public readonly int Version;

            // Encrytion key length in bits
            public readonly int KeyLength;

            // Revision of the standard security handler
            public readonly int Revision;

            // Owner password hash
            public readonly byte[] OwnerPassword;

            // User password hash
            public readonly byte[] UserPassword;

            // Flags
            public readonly uint Flags;
        }

        #endregion

    } // PdfData

}
