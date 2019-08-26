using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    // base class for sgml entities
    public abstract class SgmlTag
    {
        public readonly string Tag;

        public SgmlTag(string tag)
        {
            Tag = tag;
        }

        public abstract string ToXml();
    }

    // <tag>value
    public class SgmlElement : SgmlTag
    {
        public readonly string Value;
        public readonly bool NoNewLine;

        public SgmlElement(string tag, string value, bool noNewLine = false)
            : base(tag)
        {
            Value = value;
            NoNewLine = noNewLine;
        }

        public override string ToString()
        {
            return $"<{Tag}>{Value}" + (NoNewLine ? "" : "\r\n");
        }

        public override string ToXml()
        {
            return $"<{Tag}>{Value}</{Tag}>\r\n";
        }
    }

    // <tag>element(s)</tag>
    public class SgmlAggregate : SgmlTag
    {
        public readonly List<SgmlTag> Tags;

        public SgmlAggregate(string tag)
            : base(tag)
        {
            Tags = new List<SgmlTag>();
        }

        public SgmlAggregate(string tag, SgmlTag value)
            : this(tag)
        {
            Tags.Add(value);
        }

        public SgmlAggregate(string tag, SgmlTag val1, SgmlTag val2)
            : this(tag)
        {
            Tags.Add(val1);
            Tags.Add(val2);
        }

        public void AddTag(SgmlTag tag)
        {
            Tags.Add(tag);
        }

        public void AddElement(string tag, string value, bool noNewLine = false)
        {
            Tags.Add(new SgmlElement(tag, value, noNewLine));
        }

        public SgmlAggregate FindAggregate(string[] tagNames)
        {
            return FindAggregate(tagNames, 0);
        }

        private SgmlAggregate FindAggregate(string[] tagNames, int index)
        {
            foreach (var st in Tags)
            {
                if (st.Tag == tagNames[index] && st is SgmlAggregate aggregate)
                {
                    if ((index + 1) < tagNames.Length)
                    {
                        return aggregate.FindAggregate(tagNames, index + 1);
                    }
                    else
                    {
                        return aggregate;
                    }
                }
            }

            return null;
        }

        public string FindValue(string[] tagNames)
        {
            return FindValue(tagNames, 0);
        }

        private string FindValue(string[] tagNames, int index)
        {
            foreach(var st in Tags)
            {
                if (st.Tag == tagNames[index])
                {
                    if (st is SgmlElement element)
                    {
                        return element.Value;
                    }
                    else if (st is SgmlAggregate aggregate)
                    {
                        return aggregate.FindValue(tagNames, index + 1);
                    }
                }
            }

            return null;
        }

        public override string ToString()
        {
            string result;

            result = $"<{Tag}>\r\n";

            foreach (var tag in Tags)
            {
                result += tag.ToString();
            }

            result += $"</{Tag}>\r\n";

            return result;
        }

        public override string ToXml()
        {
            string result;

            result = $"<{Tag}>\r\n";

            foreach (var tag in Tags)
            {
                result += tag.ToXml();
            }

            result += $"</{Tag}>\r\n";

            return result;
        }
    }
}
