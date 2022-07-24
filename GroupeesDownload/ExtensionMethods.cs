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
            if (coll.Length != 1) System.Diagnostics.Debugger.Break();
            return coll.Single();
        }

        public static IElement GetSingleOrDefaultByClassName(this IElement self, string name)
        {
            var coll = self.QuerySelectorAll($":scope > .{name}");
            if (coll.Length > 1) System.Diagnostics.Debugger.Break();
            return coll.SingleOrDefault();
        }


        public static bool HasClassName(this IElement self, string name)
        {
            return self.ClassList.Contains(name);
        }
    }
}
