using System.Collections;

namespace DProjects.Db.Tests {

    public class DBRowDictionaryTests {

        [Fact]
        public void DictionaryProperties_ReflectColumnsAndValuesInOrder() {
            var (table, row, dictionary) = CreateRow();

            Assert.Equal(table.Columns.Count, dictionary.Count);
            Assert.True(dictionary.IsReadOnly);
            var keys = Assert.IsType<string[]>(dictionary.Keys);
            var values = Assert.IsType<object?[]>(dictionary.Values);
            Assert.Equal(new[] { "id", "name", "optional" }, keys);
            Assert.Equal(new object?[] { 7, "alpha", null }, values);

            keys[0] = "changed";
            values[0] = 8;

            Assert.Equal(3, table.Columns.Count);
            Assert.Equal("id", table.Columns[0].Name);
            Assert.Equal(new object?[] { 7, "alpha", null }, row.Values);
        }

        [Fact]
        public void DictionaryIndexer_GetsAndSetsExistingValuesAndMarksTableChanged() {
            var (table, row, dictionary) = CreateRow();
            table.AcceptChanges();

            Assert.Equal("alpha", dictionary["name"]);

            dictionary["name"] = "beta";

            Assert.Equal("beta", row["name"]);
            Assert.True(table.HasChanges);
        }

        [Fact]
        public void ContainsKeyAndTryGetValue_UseColumnLookupSemantics() {
            var (_, _, dictionary) = CreateRow();

            Assert.True(dictionary.ContainsKey("NAME"));
            Assert.False(dictionary.ContainsKey("missing"));
            Assert.True(dictionary.TryGetValue("NAME", out var value));
            Assert.Equal("alpha", value);
            Assert.False(dictionary.TryGetValue("missing", out value));
            Assert.Null(value);
        }

        [Fact]
        public void ContainsPair_RequiresMatchingKeyAndValueAndHandlesNull() {
            var (_, _, dictionary) = CreateRow();
            var collection = (ICollection<KeyValuePair<string, object?>>)dictionary;

            Assert.Contains(new KeyValuePair<string, object?>("name", "alpha"), collection);
            Assert.DoesNotContain(new KeyValuePair<string, object?>("name", "beta"), collection);
            Assert.Contains(new KeyValuePair<string, object?>("optional", null), collection);
            Assert.DoesNotContain(new KeyValuePair<string, object?>("missing", null), collection);
        }

        [Fact]
        public void Enumeration_ReturnsPairsInColumnOrderForBothInterfaces() {
            var (_, row, dictionary) = CreateRow();
            var expected = new[] {
                new KeyValuePair<string, object?>("id", 7),
                new KeyValuePair<string, object?>("name", "alpha"),
                new KeyValuePair<string, object?>("optional", null)
            };

            Assert.Equal(expected, dictionary.ToArray());

            var actual = new List<KeyValuePair<string, object?>>();
            foreach (var item in (IEnumerable)row) {
                actual.Add(Assert.IsType<KeyValuePair<string, object?>>(item));
            }
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CopyTo_CopiesPairsInColumnOrderAtRequestedIndex() {
            var (_, _, dictionary) = CreateRow();
            var collection = (ICollection<KeyValuePair<string, object?>>)dictionary;
            var destination = new KeyValuePair<string, object?>[5];

            collection.CopyTo(destination, 1);

            Assert.Equal(new KeyValuePair<string, object?>("id", 7), destination[1]);
            Assert.Equal(new KeyValuePair<string, object?>("name", "alpha"), destination[2]);
            Assert.Equal(new KeyValuePair<string, object?>("optional", null), destination[3]);
        }

        [Fact]
        public void CopyTo_ValidatesArguments() {
            var (_, _, dictionary) = CreateRow();
            var collection = (ICollection<KeyValuePair<string, object?>>)dictionary;

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(new KeyValuePair<string, object?>[3], -1));
            Assert.Throws<ArgumentException>(() => collection.CopyTo(new KeyValuePair<string, object?>[3], 1));
        }

        [Fact]
        public void StructuralMutations_ThrowNotSupportedException() {
            var (_, _, dictionary) = CreateRow();
            var collection = (ICollection<KeyValuePair<string, object?>>)dictionary;
            var pair = new KeyValuePair<string, object?>("other", 1);

            Assert.Throws<NotSupportedException>(() => dictionary.Add("other", 1));
            Assert.Throws<NotSupportedException>(() => collection.Add(pair));
            Assert.Throws<NotSupportedException>(() => dictionary.Remove("id"));
            Assert.Throws<NotSupportedException>(() => collection.Remove(pair));
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }

        private static (DBTable Table, DBRow Row, IDictionary<string, object?> Dictionary) CreateRow() {
            var table = new DBTable("test");
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("optional", typeof(string));
            var row = new DBRow(table, 7, "alpha", null);
            return (table, row, row);
        }

    }

}
