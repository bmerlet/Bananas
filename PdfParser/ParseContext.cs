using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfParser
{
    //
    // Pdf parse context
    //
    public class ParseContext
    {
        // Constructor
        public ParseContext(byte[] bytes) => Bytes = bytes;

        // File
        public readonly byte[] Bytes;

        // Position in file
        public int BytePos { get; private set; }

        public byte CurrentByte => Bytes[BytePos];
        public byte ReadByte() => Bytes[BytePos++];

        public void Skip(int numBytes) { BytePos += numBytes; }

        public byte[] ReadArray(int len)
        {
            var result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = ReadByte();
            }

            return result;
        }

        public bool IsWhiteSpace(int offset = 0) =>
            Bytes[BytePos + offset] == 0 ||
            Bytes[BytePos + offset] == 9 ||
            Bytes[BytePos + offset] == 10 ||
            Bytes[BytePos + offset] == 12 ||
            Bytes[BytePos + offset] == 13 ||
            Bytes[BytePos + offset] == 32;

        public void SkipWhiteSpaces()
        {
            while (IsWhiteSpace()) BytePos++;
        }

        public void SkipUntilWhiteSpace()
        {
            while (!IsWhiteSpace()) BytePos++;
            BytePos++;
        }

        public void SkipUntilCRLF()
        {
            while (Bytes[BytePos] != '\n') BytePos++;
            BytePos++;
        }

        public void SkipCRLF()
        {
            if (Bytes[BytePos] == '\r') BytePos++;
            if (Bytes[BytePos] == '\n') BytePos++;
        }

        public bool IsCommentStart => Bytes[BytePos] == '%';
        public bool IsDictionaryStart => Bytes[BytePos] == '<' && Bytes[BytePos + 1] == '<';
        public bool IsDictionaryEnd => Bytes[BytePos] == '>' && Bytes[BytePos + 1] == '>';
        public bool IsStreamStart(out int length) => IsToken(0, "stream", true, out length);
        public bool IsStreamEnd(out int length) => IsToken(0, "endstream", true, out length);

        public bool IsBeginText => Bytes[BytePos] == 'B' && Bytes[BytePos + 1] == 'T';
        public bool IsEndText => Bytes[BytePos] == 'E' && Bytes[BytePos + 1] == 'T';
        public bool IsTextOperator1 => Bytes[BytePos] == '\'' || Bytes[BytePos] == '"';
        public bool IsTextOperator2 => 
            Bytes[BytePos] == 'T' && 
            (Bytes[BytePos + 1] == 'd' || Bytes[BytePos + 1] == 'D' || Bytes[BytePos + 1] == '*' || Bytes[BytePos + 1] == 'j');

        public bool IsObjectStart(out int id, out int gen, out int length)
        {
            gen = -1;
            length = 0;
            if (IsInteger(0, true, out id, out int idLength))
            {
                if (IsInteger(idLength, true, out gen, out int genLength))
                {
                    if (IsToken(idLength + genLength, "obj", true, out int objLength))
                    {
                        length = idLength + genLength + objLength;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsObjectRef(out int id, out int gen, out int length)
        {
            gen = -1;
            length = 0;
            if (IsInteger(0, true, out id, out int idLength))
            {
                if (IsInteger(idLength, true, out gen, out int genLength))
                {
                    if (IsToken(idLength + genLength, "R", false, out int objLength))
                    {
                        length = idLength + genLength + objLength;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsObjectEnd(out int length) => IsToken(0, "endobj", true, out length);

        public bool IsXrefStart(out int length) => IsToken(0, "xref", true, out length);
        public bool IsTrailerStart(out int length) => IsToken(0, "trailer", false, out length);

        public bool IsToken(int offset, string token, bool checkCrLf, out int length)
        {
            int bp = BytePos + offset;
            length = token.Length;

            for (int i = 0; i < token.Length; i++)
            {
                if (token[i] != Bytes[bp + i])
                {
                    return false;
                }
            }

            if (checkCrLf)
            {
                if (Bytes[bp + length] == '\n')
                {
                    length += 1;
                }
                else if (Bytes[bp + length] == '\r' && Bytes[bp + length + 1] == '\n')
                {
                    length += 2;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsInteger(int offset, bool checkWhiteSpaceAfter, out int val, out int length)
        {
            bool result = false;
            bool minus = false;
            int bp = BytePos + offset;
            val = 0;
            length = 0;

            // Skip white spaces before
            while (IsWhiteSpace(offset + length))
            {
                length += 1;
            }

            // Read minus sign
            if (Bytes[bp + length] == '-')
            {
                minus = true;
                length += 1;
            }

            // Read number
            while (Bytes[bp + length] >= '0' && Bytes[bp + length] <= '9')
            {
                val = val * 10 + (int)(Bytes[bp + length] - '0');
                length += 1;
                result = true; // Need at least one digit to make a number
            }

            if (minus)
            {
                val = -val;
            }

            // Make sure there is a white space after the number
            if (checkWhiteSpaceAfter)
            {
                if (IsWhiteSpace(offset + length))
                {
                    length += 1;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        public string ReadNameString()
        {
            var str = new StringBuilder();
            while (
                !IsWhiteSpace() &&
                Bytes[BytePos] != '[' && Bytes[BytePos] != ']' && Bytes[BytePos] != '/' &&
                Bytes[BytePos] != '>' && Bytes[BytePos] != '<')
            {
                str.Append((char)ReadByte());
            }
            while (IsWhiteSpace()) BytePos += 1;

            return str.ToString();
        }

        public string ReadParenString()
        {
            int depth = 0;
            var str = new StringBuilder();
            while (true)
            {
                if (Bytes[BytePos] == '(')
                {
                    depth += 1;
                    if (depth != 1)
                    {
                        str.Append((char)ReadByte());
                    }
                    else
                    {
                        BytePos += 1;
                    }
                }
                else if (Bytes[BytePos] == ')')
                {
                    depth -= 1;
                    if (depth == 0)
                    {
                        BytePos += 1;
                        break;
                    }
                    str.Append((char)ReadByte());
                }
                else
                {
                    str.Append((char)ReadByte());
                }
            }

            while (IsWhiteSpace()) BytePos += 1;

            return str.ToString();
        }

        public bool SkipToNextTextBlock()
        {
            while(!IsBeginText)
            {
                if (++BytePos > Bytes.Length - 2)
                {
                    return false;
                }
            }

            Skip(2);
            return true;
        }

        public string GetAsString(int len)
        {
            var str = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                str.Append((char)Bytes[BytePos + i]);
            }

            return str.ToString();
        }
    }
}
