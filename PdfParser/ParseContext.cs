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
        #region Constructor

        // Constructor
        public ParseContext(byte[] bytes, int offset = 0)
        {
            Bytes = bytes;
            BytePos = offset;
            BackwardBytePos = bytes.Length - 1;
        }

        #endregion

        #region Forward parsing properties and services

        // File
        public readonly byte[] Bytes;

        // Position in file, going forward
        public int BytePos { get; private set; }

        public bool EOF => BytePos >= BackwardBytePos;

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
            BytePos + offset < Bytes.Length && 
            (Bytes[BytePos + offset] == 0 ||
             Bytes[BytePos + offset] == 9 ||
             Bytes[BytePos + offset] == 10 ||
             Bytes[BytePos + offset] == 12 ||
             Bytes[BytePos + offset] == 13 ||
             Bytes[BytePos + offset] == 32);

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
        public bool IsHexStringStart => Bytes[BytePos] == '<' && Bytes[BytePos + 1] != '<';
        public bool IsStreamStart(out int length) => IsToken(0, "stream", true, out length);
        public bool IsStreamEnd(out int length) => IsToken(0, "endstream", true, out length);

        public bool IsBeginText => Bytes[BytePos] == 'B' && Bytes[BytePos + 1] == 'T';
        public bool IsEndText => BytePos >= Bytes.Length - 1 || (Bytes[BytePos] == 'E' && Bytes[BytePos + 1] == 'T');

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
            while (bp + length < Bytes.Length && Bytes[bp + length] >= '0' && Bytes[bp + length] <= '9')
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

        public bool IsBool(out bool val, out int length)
        {
            if (GetAsString(4) == "true")
            {
                val = true;
                length = 4;
                return true;
            }
            
            if (GetAsString(5) == "false")
            {
                val = false;
                length = 5;
                return true;
            }

            val = false;
            length = 0;
            return false;
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
                if (CurrentByte == '(')
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
                else if (CurrentByte == ')')
                {
                    depth -= 1;
                    if (depth == 0)
                    {
                        Skip(1);
                        break;
                    }
                    str.Append((char)ReadByte());
                }
                else if (CurrentByte == '\\')
                {
                    Skip(1);
                    switch((char)CurrentByte)
                    {
                        case 'n':
                            str.Append('\n');
                            break;
                        case 'r':
                            str.Append('\r');
                            break;
                        case 't':
                            str.Append('\t');
                            break;
                        case 'b':
                            str.Append('\b');
                            break;
                        case 'f':
                            str.Append('\f');
                            break;
                        case '(':
                            str.Append('(');
                            break;
                        case ')':
                            str.Append(')');
                            break;
                        case '\\':
                            str.Append('\\');
                            break;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                            int octalCode = ReadByte() * 64 + ReadByte() * 8 + CurrentByte;
                            str.Append((char)octalCode);
                            break;
                        default:
                            throw new FormatException($"Unknown escape char {CurrentByte} while parsing paren string");
                    }
                    Skip(1);
                }
                else
                {
                    str.Append((char)ReadByte());
                }
            }

            while (IsWhiteSpace()) BytePos += 1;

            return str.ToString();
        }

        public byte[] ReadHexString()
        {
            var data = new List<byte>();

            while(CurrentByte != '>')
            {
                var b = CharToNibble(ReadByte()) * 16 + CharToNibble(ReadByte());
                data.Add((byte)b);
            }

            Skip(1);

            return data.ToArray();
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

        #endregion

        #region Backward searches

        // Position in file, going backward
        public int BackwardBytePos { get; private set; }

        public byte LastByte => Bytes[BackwardBytePos];
        public byte ReadLastByte() => Bytes[BackwardBytePos--];
        public void BackwardSkip(int len) => BackwardBytePos -= len;

        public void BackwardSkipCRLF()
        {
            if (LastByte == '\n') BackwardBytePos--;
            if (LastByte == '\r') BackwardBytePos--;
        }

        public string BackwardGetAsString(int len)
        {
            var str = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                str.Append((char)Bytes[BackwardBytePos - len + i + 1]);
            }

            return str.ToString();
        }

        public byte[] BackwardGetBlock(string token)
        {
            for(int i = BackwardBytePos - token.Length; i >= BytePos; i--)
            {
                if (Strcmp(i, token))
                {
                    var result = new byte[BackwardBytePos - i - token.Length + 1];
                    for(int j = 0; j < result.Length; j++)
                    {
                        result[j] = Bytes[i + token.Length + j];
                    }
                    BackwardBytePos = i;
                    return result;
                }
            }

            return null;
        }

        #endregion

        #region Private utilities

        private byte CharToNibble(byte b)
        {
            if (b >= '0' && b <= '9')
            {
                return (byte)(b - '0');
            }

            if (b >= 'a' && b <= 'f')
            {
                return (byte)(b - 'a' + 10);
            }

            if (b >= 'A' && b <= 'F')
            {
                return (byte)(b - 'A' + 10);
            }

            throw new FormatException("Malformed hex number");
        }

        private bool Strcmp(int ix, string token)
        {
            for(int i = 0; i < token.Length; i++)
            {
                if (Bytes[ix + i] != token[i])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
