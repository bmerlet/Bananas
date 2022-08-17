using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfParser
{
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
    public abstract class PdfObject
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
