using System;
using System.Collections.Generic;
using System.Linq;

namespace Metaflow.Orleans
{
    public sealed class UpgradeMap
    {
        private readonly Dictionary<Type, Type> _upgradeSourceTypes;

        private UpgradeMap(Dictionary<Type, Type> upgradeSourceTypes)
        {
            this._upgradeSourceTypes = upgradeSourceTypes;
        }

        public static UpgradeMap Initialize(IEnumerable<Type> restfulTypes)
        {
            List<Type> enumerable = restfulTypes.ToList();
            IEnumerable<Type> upgradedTypes = enumerable.Where(t => t.ModelVersion() > 1);
            var d = new Dictionary<Type, Type>();
            foreach (var restfulType in upgradedTypes)
            {
                var upgradeSource = restfulTypes.FirstOrDefault(t =>
                    t.Name == restfulType.Name && restfulType.ModelVersion() == t.ModelVersion() + 1);
                if (upgradeSource != null) d.Add(restfulType, upgradeSource);
            }

            return new UpgradeMap(d);
        }

        public Type For<T>()
        {
            if (_upgradeSourceTypes.ContainsKey(typeof(T))) return _upgradeSourceTypes[typeof(T)];
            return null;
        }
    }
}