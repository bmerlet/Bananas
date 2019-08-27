using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OfxClient.Data;

namespace OfxClient.Serializers
{
    public static class ResponseParser
    {
        //
        // Entry point: Parse all
        //
        static public OfxDocument Parse(string str)
        {
            OfxDocument result = null;

            try
            {
                // Parse header
                var header = ParseHeader(str);

                // Parse SGML
                var sgml = ParseSgml(str);

                result = new OfxDocument(null, header, sgml);
            }
            catch(InvalidOperationException e)
            {
                result = new OfxDocument(e.Message, null, null);
            }

            return result;
        }

        //
        // Parse header
        //
        static private OfxHeader ParseHeader(string str)
        {
            int version = 102;

            // Get rid of the sgml part
            int startOfSgmlix = str.IndexOf("<OFX>");
            if (startOfSgmlix > 0)
            {
                var headerStr = str.Substring(0, startOfSgmlix - 1);

                // Find version
                var versionStr = FindValue(headerStr, "VERSION:");
                if (versionStr != null)
                {
                    version = int.Parse(versionStr);
                }
            }

            return new OfxHeader(version);
        }

        //
        // Parse top-level SGML
        //
        static private SgmlAggregate ParseSgml(string str)
        {
            var ofxAggregate = new SgmlAggregate("OFX");

            // Find OFX main aggregate
            string ofxTag = "<OFX>";
            int startOfSgmlix = str.IndexOf(ofxTag);
            if (startOfSgmlix >= 0)
            {
                var ofxStr = str.Substring(startOfSgmlix + ofxTag.Length);

                int endOfSgmlIx = ofxStr.IndexOf("</OFX>");
                if (endOfSgmlIx > 0)
                {
                    ofxStr = ofxStr.Substring(0, endOfSgmlIx);
                    ParseSgml(ofxStr, ofxAggregate);
                }
            }

            return ofxAggregate;
        }

        //
        // Recursively parse the SGML
        //
        static private void ParseSgml(string str, SgmlAggregate aggregate)
        {
            char[] blanks = { ' ', '\t', '\n', '\r' };
            char[] stops = { '<', '\n', '\r' };

            // Get next tag
            int indexOfOpenBracket = str.IndexOf('<');
            if (indexOfOpenBracket >= 0)
            {
                // There are still tags - isolate the tag
                int indexOfCloseBracket = str.IndexOf('>', indexOfOpenBracket + 1);
                if (indexOfCloseBracket > indexOfOpenBracket)
                {
                    string restOfString = null;

                    // We got a tag
                    string tag = str.Substring(indexOfOpenBracket + 1, indexOfCloseBracket - indexOfOpenBracket - 1);
                    string value = str.Substring(indexOfCloseBracket + 1);

                    // Find beginning of value
                    value = value.TrimStart(blanks);

                    if (value.Length == 0)
                    {
                        throw new InvalidOperationException("No value for tag " + tag);
                    }

                    // See if this tag is an element or an aggregate
                    if (value[0] == '<')
                    {
                        // Aggregate
                        string endTag = "</" + tag + ">";
                        int endOfAggregate = value.IndexOf(endTag);
                        if (endOfAggregate < 0)
                        {
                            throw new InvalidOperationException("No closing tag for " + tag);
                        }

                        restOfString = value.Substring(endOfAggregate + endTag.Length);
                        value = value.Substring(0, endOfAggregate);

                        var subAggregate = new SgmlAggregate(tag);
                        aggregate.AddTag(subAggregate);

                        // Recurse to parse this aggregate
                        ParseSgml(value, subAggregate);
                    }
                    else
                    {
                        // Element
                        int endOfValue = value.IndexOfAny(stops);

                        if (endOfValue > 0)
                        {
                            // More stuff after this element
                            restOfString = value.Substring(endOfValue);
                            value = value.Substring(0, endOfValue);
                        }

                        // Add the element to the current aggregate
                        aggregate.AddElement(tag, value);
                    }

                    // Go on with the rest of the string
                    if (restOfString != null)
                    {
                        restOfString.TrimStart(blanks);
                        if (restOfString.Length > 0)
                        {
                            ParseSgml(restOfString, aggregate);
                        }
                    }
                }
            }
        }

        //
        // Find value for a tag
        //
        static private string FindValue(string str, string tag)
        {
            string val = null;
            char[] terminators = { '<', '\n', '\r' };

            int tagIndex = str.IndexOf(tag);
            if (tagIndex >= 0)
            {
                int valueIndex = tagIndex + tag.Length;
                int endIndex = str.IndexOfAny(terminators, valueIndex);
                if (endIndex > valueIndex)
                {
                    val = str.Substring(valueIndex, endIndex - valueIndex);
                }
            }

            return val;
        }
    }
}
