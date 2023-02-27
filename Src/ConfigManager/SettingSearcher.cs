using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;

using ConfigurationManager.Utilities;

namespace ConfigurationManager
{
    internal static class SettingSearcher
    {
        private static readonly ICollection<string> _updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        public static void CollectSettings(out IEnumerable<SettingEntryBase> results, out List<string> modsWithoutSettings, bool showDebug)
        {
            modsWithoutSettings = new List<string>();

            try
            {
                results = GetBepInExCoreConfig();
            }
            catch (Exception ex)
            {
                results = Enumerable.Empty<SettingEntryBase>();
                ConfigurationManager.Logger.LogError(ex);
            }

            try
            {
                var plugins = Utils.FindPlugins();
                foreach (var plugin in plugins)
                {

                    ConfigurationManager.Logger.LogWarning("Found Plugin " + plugin);
                    var type = plugin.GetType();

                    var pluginInfo = plugin.Info.Metadata;

                    if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                        .Any(x => !x.Browsable))
                    {
                        modsWithoutSettings.Add(pluginInfo.Name);
                        continue;
                    }

                    var detected = new List<SettingEntryBase>();

                    detected.AddRange(GetPluginConfig(plugin).Cast<SettingEntryBase>());

                    detected.RemoveAll(x => x.Browsable == false);

                    if (detected.Count == 0)
                        modsWithoutSettings.Add(pluginInfo.Name);

                    // Allow to enable/disable plugin if it uses any update methods ------
                    if (showDebug && type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(x => _updateMethodNames.Contains(x.Name)))
                    {
                        var enabledSetting = new PropertySettingEntry(plugin, type.GetProperty("enabled"), plugin);
                        enabledSetting.DispName = "!Allow plugin to run on every frame";
                        enabledSetting.Description = "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                        enabledSetting.IsAdvanced = true;
                        detected.Add(enabledSetting);
                    }

                    if (detected.Count > 0)
                        results = results.Concat(detected);
                }
            }
            catch (Exception ex)
            {
                ConfigurationManager.Logger.LogError(ex);
            }
            
        }

        /// <summary>
        /// Get entries for all core BepInEx settings
        /// </summary>
        private static IEnumerable<SettingEntryBase> GetBepInExCoreConfig()
        {
            var coreConfig = ConfigFile.CoreConfig;
            var bepinMeta = new BepInPlugin("BepInEx", "BepInEx", typeof(BepInEx.Unity.Mono.BaseUnityPlugin).Assembly.GetName().Version.ToString());

            return coreConfig.Select(kvp => (SettingEntryBase)new ConfigSettingEntry(kvp.Value, null) { IsAdvanced = true, PluginInfo = bepinMeta });
        }

        /// <summary>
        /// Get entries for all settings of a plugin
        /// </summary>
        private static IEnumerable<ConfigSettingEntry> GetPluginConfig(BaseUnityPlugin plugin)
        {
            return plugin.Config.Select(kvp => new ConfigSettingEntry(kvp.Value, plugin));
        }
    }
}