using System.Collections.Generic;
using System.Linq;

namespace SFramework.Configs.Runtime
{
    public static partial class SFExtensions
    {
        public static bool IsPasses(this IEnumerable<SFRestriction> restrictions)
        {
            if (restrictions == null)
            {
                return true;
            }

            return restrictions.All(x => x.IsPasses());
        }

        public static bool IsEmpty(this IEnumerable<SFRestriction> restrictions)
        {
            return restrictions == null || !restrictions.Any();
        }
    }
}