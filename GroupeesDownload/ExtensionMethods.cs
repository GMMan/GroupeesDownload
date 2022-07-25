using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupeesDownload
{
    static class ExtensionMethods
    {
        public static IElement GetSingleByClassName(this IElement self, string name)
        {
            var coll = self.QuerySelectorAll($":scope > .{name}");
            if (coll.Length != 1) throw new ParsingException($"Expected only 1 element with class {name}, found {coll.Length}.");
            return coll[0];
        }

        public static IElement GetSingleOrDefaultByClassName(this IElement self, string name)
        {
            var coll = self.QuerySelectorAll($":scope > .{name}");
            if (coll.Length > 1) throw new ParsingException($"Expected only 0 or 1 elements with class {name}, found {coll.Length}.");
            return coll.Length == 1 ? coll[0] : null;
        }


        public static bool HasClassName(this IElement self, string name)
        {
            return self.ClassList.Contains(name);
        }
    }
}
