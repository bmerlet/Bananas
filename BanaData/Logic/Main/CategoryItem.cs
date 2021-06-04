using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Main
{
    public class CategoryItem
    {
        public CategoryItem(string name)
        {
            CategoryFullName = name;
            int cix = name.IndexOf(':');
            if (cix > 0)
            {
                Parent = name.Substring(0, cix);
                Name = name.Substring(cix + 1);
            }
            else
            {
                Parent = null;
                Name = name;
            }
        }

        public string Parent { get; }
        public string Name { get; }
        public string CategoryFullName { get; }

        public override string ToString()
        {
            return (Parent == null ? "  " : "") + Name;
        }

        public bool IsBold => Parent == null;
    }
}
