using System;
using System.Reflection;
using BepInEx.Unity.Mono;

namespace ConfigurationManager
{
    internal class PropertySettingEntry : SettingEntryBase
    {
        private Type? _settingType;

        public PropertySettingEntry(object instance, PropertyInfo settingProp, BaseUnityPlugin pluginInstance)
        {
            SetFromAttributes(settingProp.GetCustomAttributes(false), pluginInstance);
            if (Browsable == null) Browsable = settingProp.CanRead && settingProp.CanWrite;
            ReadOnly = settingProp.CanWrite;
            Property = settingProp;
            Instance = instance;
        }

        public object Instance { get; internal set; }
        public PropertyInfo Property { get; internal set; }

        public override string DispName
        {
            get => _dispName == null || _dispName == string.Empty ? Property.Name : _dispName;
            set => _dispName = value;
        }
        public override Type SettingType => _settingType ??= Property.PropertyType;
        public override object Get() => Property.GetValue(Instance, null);
        protected override void SetValue(object? newVal) => Property.SetValue(Instance, newVal, null);
    }
}
