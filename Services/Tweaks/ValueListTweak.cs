using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Одно значение реестра для простого твика-списка.</summary>
    public sealed class TweakValue
    {
        public TweakValue(RegistryHive hive, string path, string name, object value)
        {
            Hive = hive;
            Path = path;
            Name = name;
            Value = value;
            Kind = value is int
                ? RegistryValueKind.DWord
                : value is byte[]
                    ? RegistryValueKind.Binary
                    : RegistryValueKind.String;
        }

        public RegistryHive Hive { get; private set; }

        public string Path { get; private set; }

        public string Name { get; private set; }

        public object Value { get; private set; }

        public RegistryValueKind Kind { get; private set; }

        public bool Matches(TweakContext context)
        {
            object current = context.Registry.GetValue(Hive, Path, Name);
            if (current == null)
            {
                return false;
            }

            switch (Kind)
            {
                case RegistryValueKind.DWord:
                    return Convert.ToInt32(current) == Convert.ToInt32(Value);

                case RegistryValueKind.Binary:
                    var left = current as byte[];
                    var right = Value as byte[];
                    if (left == null || right == null || left.Length != right.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < left.Length; i++)
                    {
                        if (left[i] != right[i])
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    return string.Equals(
                        Convert.ToString(current),
                        Convert.ToString(Value),
                        StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Твик, который целиком описывается набором значений реестра:
    /// «применён», когда совпадают все значения, откат делает бэкап.
    /// </summary>
    public abstract class ValueListTweak : TweakBase
    {
        protected abstract TweakValue[] Values { get; }

        public override TweakState Detect(TweakContext context)
        {
            TweakValue[] values = Values;
            if (values == null || values.Length == 0)
            {
                return TweakState.NotApplied;
            }

            return values.All(value => value.Matches(context))
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (TweakValue value in Values)
            {
                context.Registry.WriteValue(value.Hive, value.Path, value.Name, value.Value, value.Kind);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Прежние значения возвращает бэкап TweakScope.
        }
    }

    /// <summary>Подсказки и общие данные для набора простых твиков.</summary>
    internal static class TweakValues
    {
        public static TweakValue[] List(params TweakValue[] values)
        {
            return values ?? new TweakValue[0];
        }
    }
}
