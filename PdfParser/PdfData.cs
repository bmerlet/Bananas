using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfParser
{
    //
    // All the pdf objects
    //
    public class PdfData
    {
        private readonly List<PdfObject> pdfObjects = new List<PdfObject>();
        public IEnumerable<PdfObject> PdfObjects => pdfObjects;

        // Trailer info
        public PdfTrailer PdfTrailer { get; private set; }

        // Derived info
        public PdfDictionary Catalog { get; private set; }
        public PdfDictionary PageTreeRoot { get; private set; }
        public int NumberOfPages { get; private set; }
        public PdfObjectId[] Pages { get; private set; }

        // Add an object
        public void Add(PdfObject pdfObject)
        {
            pdfObjects.Add(pdfObject);
        }

        // Set trailer info
        public void SetPdfTrailer(int size, PdfObjectId catalog, PdfObjectId info)
        {
            PdfTrailer = new PdfTrailer(size, catalog, info);

            // Populate derived info
            Catalog = Find<PdfDictionary>(catalog);
            var pageTreeRootRef = Catalog.Find<PdfReference>("Pages");
            PageTreeRoot = Find<PdfDictionary>(pageTreeRootRef.Value);
            NumberOfPages = PageTreeRoot.Find<PdfInt>("Count").Value;
            Pages = new PdfObjectId[NumberOfPages];

            // Traverse the tree to populate the page ids
            int ix = 0;
            PopulatePages(PageTreeRoot, ref ix);
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
            var pageDic = Find<PdfDictionary>(page);
            var contentRef = pageDic.Find<PdfReference>("Contents");
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
        public PdfTrailer(int size, PdfObjectId catalog, PdfObjectId info) => (Size, Catalog, Info) = (size, catalog, info);

        public readonly int Size;
        public readonly PdfObjectId Catalog;
        public readonly PdfObjectId Info;
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
    // Base class for all Pdf objects
    //
    public class PdfObject
    {
        public PdfObject(PdfObjectId pdfObjectId) => PdfObjectId = pdfObjectId;

        // Object id (may be null)
        public readonly PdfObjectId PdfObjectId;
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
