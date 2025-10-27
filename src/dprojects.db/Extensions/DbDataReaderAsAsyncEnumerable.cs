using DProjects.Db.Readers;
using DProjects.Db.Writers;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DProjects.Db.Extensions {


    public static class DbDataReaderAsAsyncEnumerable {


        //methods
        private sealed class RecordMap(ConstructorInfo constructorInfo, ParameterInfo[] pparams, int[] ordinals) {
            public ConstructorInfo Ctor { get; } = constructorInfo;
            public ParameterInfo[] Params { get; } = pparams;
            public int[] Ordinals { get;  } = ordinals;
        }

        private static readonly ConcurrentDictionary<Type, RecordMap> Cache = new();

        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this DbDataReader reader) {
            var type = typeof(T);
            var map = Cache.GetOrAdd(type, CreateMap<T>(reader));

            while (await reader.ReadAsync()) {
                var args = new object?[map.Params.Length];

                for (int i = 0; i < map.Params.Length; i++) {
                    int ord = map.Ordinals[i];
                    if (ord == -1 || reader.IsDBNull(ord)) {
                        args[i] = GetDefault(map.Params[i].ParameterType);
                        continue;
                    }

                    var raw = reader.GetValue(ord);
                    var targetType = Nullable.GetUnderlyingType(map.Params[i].ParameterType) ?? map.Params[i].ParameterType;
                    args[i] = Convert.ChangeType(raw, targetType);
                }

                yield return (T)map.Ctor.Invoke(args);
            }
        }

        private static RecordMap CreateMap<T>(DbDataReader reader) {
            var ctor = typeof(T).GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Type {typeof(T)} must have a public constructor.");

            var parameters = ctor.GetParameters();

            // Build dictionary of normalized column names → ordinal
            var nameToOrdinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++) {
                string col = reader.GetName(i);
                nameToOrdinal[col] = i;
                nameToOrdinal[ToPascalCase(col)] = i;
                nameToOrdinal[ToSnakeCase(col)] = i;
            }

            // Resolve ordinals for each constructor parameter
            var ordinals = parameters.Select(p =>
                nameToOrdinal.TryGetValue(p.Name!, out int ord) ? ord : -1
            ).ToArray();

            return new RecordMap ( ctor, parameters, ordinals );
        }

        private static object? GetDefault(Type t) =>
            t.IsValueType ? Activator.CreateInstance(t) : null;

        private static string ToPascalCase(string snake) {
            if (string.IsNullOrEmpty(snake)) return snake;
            var sb = new StringBuilder(snake.Length);
            bool upper = true;
            foreach (var c in snake) {
                if (c == '_' || c == '-') { upper = true; continue; }
                sb.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }
            return sb.ToString();
        }

        private static string ToSnakeCase(string pascal) {
            if (string.IsNullOrEmpty(pascal)) return pascal;
            var sb = new StringBuilder(pascal.Length + 8);
            for (int i = 0; i < pascal.Length; i++) {
                var c = pascal[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }


    }


}