using System.Collections;

using DProjects.DataObjects;

namespace DProjects.DataObjects.Tests {

    public class VOTests {

        // methods
        [Fact]
        public void ConstructionAndIndexer_ExposeDictionaryAndDerivedProperties() {
            var empty = new VO();
            var populated = new VO(new Dictionary<string, object?> { ["name"] = "Ada" });
            var derived = new DerivedVO { DisplayName = "property" };

            Assert.Null(empty["missing"]);
            Assert.Equal("Ada", populated["name"]);
            populated["name"] = "Grace";
            Assert.Equal("Grace", populated["name"]);
            Assert.Equal("property", derived["DisplayName"]);
        }
        [Fact]
        public void Get_ConvertsSupportedValuesAndUsesDefaultsForMissingOrNullValues() {
            var vo = new VO { ["number"] = "42", ["null"] = null, ["bad"] = "not-a-number" };

            Assert.Equal(42, vo.Get<int>("number"));
            Assert.Equal(42L, Assert.IsType<long>(vo.Get("number", typeof(long), null)));
            Assert.Equal(7, vo.Get("missing", 7));
            Assert.Equal("fallback", vo.Get("null", typeof(string), "fallback"));
            Assert.ThrowsAny<Exception>(() => vo.Get<int>("bad"));
        }
        [Fact]
        public void Select_NavigatesNestedObjectsListsArraysAndNegativeIndexes() {
            var vo = CreateGraph();

            Assert.Equal("root", vo.Select("name"));
            Assert.Equal("child", vo.Select("child.name"));
            Assert.Equal("zero", vo.Select("items[0]"));
            Assert.Equal("two", vo.Select("items[-1]"));
            Assert.Equal("second", vo.Select("child.items[1].name"));
            Assert.Same(vo, vo.Select(""));
        }
        [Fact]
        public void Select_InvalidPathsHaveExplicitFailureSemantics() {
            var vo = CreateGraph();

            Assert.Throws<KeyNotFoundException>(() => vo.Select("missing"));
            Assert.Throws<ArgumentOutOfRangeException>(() => vo.Select("items[9]"));
            Assert.Throws<FormatException>(() => vo.Select("items[bad]"));
            Assert.ThrowsAny<Exception>(() => vo.Select("name.value"));
        }
        [Fact]
        public void Modify_UpdatesOnlyTheSelectedTargetIncludingNegativeIndexes() {
            var vo = CreateGraph();

            Assert.True(vo.Modify("name", "changed"));
            Assert.True(vo.Modify("child.name", "changed-child"));
            Assert.True(vo.Modify("items[-1]", "changed-last"));
            Assert.True(vo.Modify("child.items[1].name", "changed-second"));

            Assert.Equal("changed", vo.Select("name"));
            Assert.Equal("changed-child", vo.Select("child.name"));
            Assert.Equal("zero", vo.Select("items[0]"));
            Assert.Equal("changed-last", vo.Select("items[-1]"));
            Assert.Equal("first", vo.Select("child.items[0].name"));
            Assert.Equal("changed-second", vo.Select("child.items[1].name"));
        }
        [Fact]
        public void Delete_RemovesDictionaryListAndArrayValuesIncludingNegativeIndexes() {
            var vo = CreateGraph();
            vo["array"] = new object?[] { "a", "b", "c" };

            Assert.True(vo.Delete("child.name"));
            Assert.True(vo.Delete("items[-1]"));
            Assert.True(vo.Delete("array[1]"));

            Assert.Null(((VO)vo["child"]!)["name"]);
            Assert.Equal(new object?[] { "zero", "one" }, ((IList)vo["items"]!).Cast<object?>());
            Assert.Equal(new object?[] { "a", "c" }, ((IList)vo["array"]!).Cast<object?>());
        }
        [Fact]
        public void Delete_MissingDictionaryValueReturnsFalse() {
            var vo = new VO();

            Assert.False(vo.Delete("missing"));
        }
        [Fact]
        public void Import_MergesNestedObjectsAndDeduplicatesListsWhileOverwritingScalars() {
            var target = new VO {
                ["scalar"] = "old",
                ["nullable"] = null,
                ["nested"] = new VO { ["left"] = 1 },
                ["items"] = new object[] { "a", "b" }
            };
            var source = new VO {
                ["scalar"] = "new",
                ["nullable"] = "filled",
                ["nested"] = new VO { ["right"] = 2 },
                ["items"] = new object[] { "b", "c" },
                ["added"] = true
            };

            target.Import(source);

            Assert.Equal("new", target["scalar"]);
            Assert.Equal("filled", target["nullable"]);
            Assert.Equal(1, target.Select("nested.left"));
            Assert.Equal(2, target.Select("nested.right"));
            Assert.Equal(new object[] { "a", "b", "c" }, ((IList)target["items"]!).Cast<object>());
            Assert.Equal(true, target["added"]);
        }
        [Fact]
        public void Clone_PreservesUsefulNestedTypesAndDoesNotShareMutableState() {
            var original = CreateGraph();

            var clone = original.Clone();
            clone.Modify("child.name", "clone-child");
            clone.Modify("items[0]", "clone-zero");

            Assert.NotSame(original, clone);
            Assert.IsType<VO>(clone["child"]);
            Assert.Equal("child", original.Select("child.name"));
            Assert.Equal("clone-child", clone.Select("child.name"));
            Assert.Equal("zero", original.Select("items[0]"));
            Assert.Equal("clone-zero", clone.Select("items[0]"));
        }

        // methods (private)
        private static VO CreateGraph() {
            return new VO {
                ["name"] = "root",
                ["items"] = new List<object?> { "zero", "one", "two" },
                ["child"] = new VO {
                    ["name"] = "child",
                    ["items"] = new List<object?> {
                        new VO { ["name"] = "first" },
                        new VO { ["name"] = "second" }
                    }
                }
            };
        }

        private sealed class DerivedVO : VO {

            // props
            public string DisplayName { get; set; } = "";
        }
    }
}
