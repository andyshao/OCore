using System;
using System.Collections.Generic;
using System.Linq;

namespace OCore.Modules.Manifest
{
    /// <summary>
    /// Defines a Module which is composed of features.�����ɹ�����ɵ�ģ�顣
    /// If the Module has only one default feature, it may be defined there.���ģ��ֻ��һ��Ĭ�Ϲ��ܣ�������ڴ˴����塣
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleAttribute : FeatureAttribute
    {
        public ModuleAttribute()
        {
        }

        public virtual string Type => "Module";
        public new bool Exists => Id != null;

        /// <Summary>
        /// This identifier is overridden at runtime by the assembly name�ñ�ʶ��������ʱ���������Ƹ���
        /// </Summary>
        public new string Id { get; internal set; }

        /// <Summary>The name of the developer.�����ߵ�����</Summary>
        public string Author { get; set; } = String.Empty;

        /// <Summary>The URL for the website of the developer.������Ա����վURL��</Summary>
        public string Website { get; set; } = String.Empty;

        /// <Summary>The version number in SemVer format.SemVer��ʽ�İ汾�š�</Summary>
        public string Version { get; set; } = "0.0";

        /// <Summary>A list of tags.��ǩ�б�</Summary>
        public string[] Tags { get; set; } = Enumerable.Empty<string>().ToArray();

        /// <summary>
        /// ��ģ������Ĺ����б�
        /// </summary>
        public List<FeatureAttribute> Features { get; } = new List<FeatureAttribute>();
    }

    /// <summary>
    /// Defines a Feature in a Module, can be used multiple times.��ģ���ж���һ�����ܣ����Զ�����
    /// If at least one Feature is defined, the Module default feature is ignored.��������˶�����ܣ���ģ��Ĭ�Ϲ��ܽ�������
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class FeatureAttribute : Attribute
    {
        public FeatureAttribute()
        {
        }

        public bool Exists => Id != null;

        /// <Summary>The identifier of the feature.</Summary>
        public string Id { get; set; }

        /// <Summary>
        /// Human-readable name of the feature. If not provided, the identifier will be used.
        /// Ϊ�ù����ṩһ�������������ѣ����ࣩ�Ķ������ƣ����û���ṩ����ʹ�ñ�ʶ��
        /// </Summary>
        public string Name { get; set; }

        /// <Summary>A brief summary of what the feature does.�ù��ܵļ�Ҫ˵��</Summary>
        public string Description { get; set; } = String.Empty;

        /// <Summary>
        /// A list of features that the feature depends on.�ù������������������б�
        /// So that its drivers / handlers are invoked after those of dependencies.
        /// </Summary>
        public string[] Dependencies { get; set; } = Enumerable.Empty<string>().ToArray();

        /// <Summary>
        /// The priority of the feature without breaking the dependencies order.�ù��ܵ����ȼ��������ȼ������ƻ�������
        /// higher is the priority, later the drivers / handlers are invoked.
        /// </Summary>
        public string Priority { get; set; } = "0";

        /// <Summary>
        /// The group (by category) that the feature belongs.�ù����������飨����𣩡�
        /// If not provided, defaults to 'Uncategorized'.���û���ṩ��Ĭ��Ϊ'Uncategorized'��δ���ࣩ��
        /// </Summary>
        public string Category { get; set; }

        /// <summary>
        /// Set to <c>true</c> to only allow the Default tenant to enable it.
        /// </summary>
        public bool DefaultTenantOnly { get; set; }
    }

    /// <summary>
    /// Marks an assembly as a module of a given type, auto generated on building.�����򼯱��Ϊָ�����͵�ģ�飬�ڹ���ʱ�Զ����ɡ�
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleMarkerAttribute : ModuleAttribute
    {
        private string _type;

        public ModuleMarkerAttribute(string name, string type)
        {
            Name = name;
            _type = type;
        }

        public override string Type => _type;
    }

    /// <summary>
    /// Enlists the package or project name of a referenced module, auto generated on building.���ģ������ƣ�package��project�����Թ����ø�ģ��ʱʹ�ã��ڹ���ʱ�Զ����ɡ�
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ModuleNameAttribute : Attribute
    {
        public ModuleNameAttribute(string name)
        {
            Name = name ?? String.Empty;
        }

        /// <Summary>
        /// The package or project name of the referenced module.��ȡ���øð�����ĿʱҪʹ�õ�����
        /// </Summary>
        public string Name { get; }
    }

    /// <summary>
    /// Maps a module asset to its project location while in debug mode, auto generated on building.�ڵ���ģʽ�½�ģ���ʲ�ӳ�䵽����Ŀλ�ã��ڹ���ʱ�Զ����ɡ�
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ModuleAssetAttribute : Attribute
    {
        public ModuleAssetAttribute(string asset)
        {
            Asset = asset ?? String.Empty;
        }

        /// <Summary>
        /// A module asset in the form of '{ModuleAssetPath}|{ProjectAssetPath}'.ģ���ʲ�����ʽ��{ModuleAssetPath}|{ProjectAssetPath}��
        /// </Summary>
        public string Asset { get; }
    }
}