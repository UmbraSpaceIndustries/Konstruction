using System.Collections.Generic;

namespace Konstruction
{
    public class KonstructionPersistance
    {
        public ConfigNode SettingsNode { get; private set; }
        private List<KonstructionModuleResource> _moduleResources;
        private List<KonstructionCostResource> _costResources;
        private KonstructionConfig _settings;

        public void AddCostResourceNode(KonstructionCostResource cost)
        {
            var count = _costResources.Count;
            for (int i = 0; i < count; ++i)
            {
                if (_costResources[i].resourceName == cost.resourceName)
                    return;
            }
            _costResources.Add(cost);
        }

        public void AddModuleResourceNode(KonstructionModuleResource mod)
        {
            var count = _moduleResources.Count;
            for (int i = 0; i < count; ++i)
            {
                if (_moduleResources[i].moduleName == mod.moduleName)
                    return;
            }
            _moduleResources.Add(mod);
        }

        public void DeleteCostNode(string res)
        {
            var count = _costResources.Count;
            for (int i = 0; i < count; ++i)
            {
                var k = _costResources[i];
                if (k.resourceName == res)
                {
                    _costResources.Remove(k);
                    return;
                }
            }
        }

        public void DeleteModuleNode(string mod)
        {
            var count = _moduleResources.Count;
            for (int i = 0; i < count; ++i)
            {
                var k = _moduleResources[i];
                if (k.moduleName == mod)
                {
                    _moduleResources.Remove(k);
                    return;
                }
            }
        }

        public List<KonstructionCostResource> GetCostResources()
        {
            return _costResources ?? (_costResources = LoadCostResources());
        }

        public List<KonstructionModuleResource> GetModuleResources()
        {
            return _moduleResources ?? (_moduleResources = LoadModuleResources());
        }

        public KonstructionConfig GetSettings()
        {
            return _settings ?? (_settings = LoadKonstructionConfig());
        }

        public static int GetValue(ConfigNode config, string name, int currentValue)
        {
            if (config.HasValue(name) && int.TryParse(config.GetValue(name), out int newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public static bool GetValue(ConfigNode config, string name, bool currentValue)
        {
            if (config.HasValue(name) && bool.TryParse(config.GetValue(name), out bool newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public static float GetValue(ConfigNode config, string name, float currentValue)
        {
            if (config.HasValue(name) && float.TryParse(config.GetValue(name), out float newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public static List<KonstructionCostResource> ImportCostNodeList(ConfigNode[] nodes)
        {
            var nList = new List<KonstructionCostResource>();
            var count = nodes.Length;
            for (int i = 0; i < count; ++i)
            {
                var node = nodes[i];
                var res = ResourceUtilities.LoadNodeProperties<KonstructionCostResource>(node);
                nList.Add(res);
            }
            return nList;
        }

        public static List<KonstructionModuleResource> ImportModuleNodeList(ConfigNode[] nodes)
        {
            var nList = new List<KonstructionModuleResource>();
            var count = nodes.Length;
            for (int i = 0; i < count; ++i)
            {
                var node = nodes[i];
                var res = ResourceUtilities.LoadNodeProperties<KonstructionModuleResource>(node);
                nList.Add(res);
            }
            return nList;
        }

        public KonstructionConfig ImportConfig(ConfigNode node)
        {
            var config = ResourceUtilities.LoadNodeProperties<KonstructionConfig>(node);
            return config;
        }

        public bool IsLoaded()
        {
            return SettingsNode != null;
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("KONSTRUCTION_SETTINGS"))
            {
                SettingsNode = node.GetNode("KONSTRUCTION_SETTINGS");
                _moduleResources = LoadModuleResources();
                _costResources = LoadCostResources();
                _settings = LoadKonstructionConfig();
            }
            else
            {
                _moduleResources = new List<KonstructionModuleResource>();
                _costResources = new List<KonstructionCostResource>();
                _settings = null;
            }
        }

        private List<KonstructionCostResource> LoadCostResources()
        {
            var configNodes = GameDatabase.Instance.GetConfigNodes("KONSTRUCTION_COST_RESOURCE");
            var nodeList = new List<KonstructionCostResource>();
            foreach (var n in configNodes)
            {
                var node = ResourceUtilities.LoadNodeProperties<KonstructionCostResource>(n);
                nodeList.Add(node);
            }
            return nodeList;
        }

        private KonstructionConfig LoadKonstructionConfig()
        {
            var kNodes = GameDatabase.Instance.GetConfigNodes("KONSTRUCTION_SETTINGS");
            var finalSettings = new KonstructionConfig
            {
                massResourceName = "MaterialKits",
                massMultiplier = 2,
                costResourceName = "SpecializedParts"
            };

            foreach (var node in kNodes)
            {
                var settings = ResourceUtilities.LoadNodeProperties<KonstructionConfig>(node);
                finalSettings.massResourceName = settings.massResourceName;
                finalSettings.massMultiplier = settings.massMultiplier;
                finalSettings.costResourceName = settings.costResourceName;
            }
            return finalSettings;
        }

        private List<KonstructionModuleResource> LoadModuleResources()
        {
            var configNodes = GameDatabase.Instance.GetConfigNodes("KONSTRUCTION_MODULE_COST");
            var nodeList = new List<KonstructionModuleResource>();
            foreach (var n in configNodes)
            {
                var node = ResourceUtilities.LoadNodeProperties<KonstructionModuleResource>(n);
                nodeList.Add(node);
            }
            return nodeList;
        }

        public void SaveConfig(KonstructionConfig config)
        {
            _settings.massResourceName = config.massResourceName;
            _settings.massMultiplier = config.massMultiplier;
            _settings.costResourceName = config.costResourceName;
        }

        public void SaveCostNode(KonstructionCostResource cost)
        {
            KonstructionCostResource c = null;
            var count = _costResources.Count;
            for (int i = 0; i < count; ++i)
            {
                var n = _costResources[i];
                if (n.resourceName == cost.resourceName)
                {
                    c = n;
                    break;
                }
            }

            if (c == null)
            {
                c = new KonstructionCostResource
                {
                    resourceName = cost.resourceName,
                    maxMass = cost.maxMass
                };
                _costResources.Add(c);
            }
        }

        public void Save(ConfigNode node)
        {
            if (node.HasNode("KONSTRUCTION_SETTINGS"))
            {
                SettingsNode = node.GetNode("KONSTRUCTION_SETTINGS");
            }
            else
            {
                SettingsNode = node.AddNode("KONSTRUCTION_SETTINGS");
            }

            if (_settings == null)
                _settings = LoadKonstructionConfig();

            if (_moduleResources != null)
            {
                var count = _moduleResources.Count;
                for (int i = 0; i < count; ++i)
                {
                    var r = _moduleResources[i];
                    var rNode = new ConfigNode("KONSTRUCTION_MODULE_COST");
                    rNode.AddValue("moduleName", r.moduleName);
                    rNode.AddValue("resourceName", r.resourceName);
                    rNode.AddValue("massMultiplier", r.massMultiplier);
                    SettingsNode.AddNode(rNode);
                }
            }

            if (_costResources != null)
            {
                var count = _costResources.Count;
                for (int i = 0; i < count; ++i)
                {
                    var r = _costResources[i];
                    var rNode = new ConfigNode("KONSTRUCTION_COST_RESOURCE");
                    rNode.AddValue("resourceName", r.resourceName);
                    rNode.AddValue("maxMass", r.maxMass);
                    SettingsNode.AddNode(rNode);
                }
            }

            if (_settings != null)
            {
                var sNode = new ConfigNode("KONSTRUCTION_SETTINGS");
                sNode.AddValue("massResourceName", _settings.massResourceName);
                sNode.AddValue("massMultiplier", _settings.massMultiplier);
                sNode.AddValue("costResourceName", _settings.costResourceName);
                SettingsNode.AddNode(sNode);
            }
        }

        public void SaveModuleNode(KonstructionModuleResource mod)
        {
            KonstructionModuleResource c = null;
            var count = _moduleResources.Count;
            for (int i = 0; i < count; ++i)
            {
                var n = _moduleResources[i];
                if (n.moduleName == mod.moduleName)
                {
                    c = n;
                    break;
                }
            }

            if (c == null)
            {
                c = new KonstructionModuleResource
                {
                    moduleName = mod.moduleName,
                    resourceName = mod.resourceName,
                    massMultiplier = mod.massMultiplier
                };
                _moduleResources.Add(c);
            }
        }
    }
}
