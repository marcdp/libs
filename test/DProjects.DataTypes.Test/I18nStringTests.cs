using Xunit;
using DProjects.DataTypes;

namespace DProjects.DataTypes.Tests
{
    public class I18nStringTests {

        [Fact]
        public void I18nString_GetValue() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Holla|Hello");
            Assert.Equal("Hello", i18n["en"]);
            Assert.Equal("Holla", i18n["es"]);
        }

        [Fact]
        public void I18nString_SetValue() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Holla|Hello");
            i18n["en"] = "Hi";
            i18n["es"] = "Hola";
            Assert.Equal("Hi", i18n["en"]);
            Assert.Equal("Hola", i18n["es"]);
        }

        [Fact]
        public void I18nString_SetValue_AddNew() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Holla|Hello");
            i18n["fr"] = "Bonjour";
            Assert.Equal("Bonjour", i18n["fr"]);
        }

        [Fact]
        public void I18nString_SetValue_Remove() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Holla|Hello");
            i18n["en"] = "";
            Assert.Equal("Holla", i18n["en"]);
        }   
        [Fact]
        public void MissingLanguage_UsesTheDeterministicTrailingFallback() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Hola|Default");

            Assert.Equal("Default", i18n["fr"]);
            Assert.Equal("Default", i18n[""]);
        }
        [Fact]
        public void UpdateAndRemove_PreserveOnlyExplicitTranslations() {
            var i18n = new I18nString("i18n_en:Hello|i18n_es:Hola|Default");

            i18n["fr"] = "Bonjour";
            i18n["es"] = "";

            Assert.Equal("Bonjour", i18n["fr"]);
            Assert.Equal("i18n_en:Hello|i18n_fr:Bonjour", i18n.ToString());
        }

    }
        
}
