
//class
export default class I18n {

    //var
    _config = {
        lang: "en",
        langs: [
            { id: 'en', label: 'English'},
            { id: 'es', label: 'Spanish'},
            { id: 'fr', label: 'French' },
            { id: 'de', label: 'German' },
        ],
        datetime: { 
            options: {
                year: 'numeric', 
                month: '2-digit', 
                day: '2-digit', 
                hour: '2-digit', 
                minute: '2-digit', 
                second: '2-digit' 
            },
            formats: {
                time: {
                    timeStyle: "short",
                },
                date: {
                    dateStyle: "short",
                },
                datetime: {
                    timeStyle: "short",
                    dateStyle: "short",
                }
            }
        },
        strings: {
        }
    };
    _cache = {};
    
    //ctor
    constructor(){
    }

    //props
    get config() { return this._config; }

    //methods
    async init(config) { 
        this._config = config;
        Object.freeze(this._config);
    }
    getMainLangs() {
        let result = this._cache.mainLangs;
        if (!result) {
            result = this.config.langs.filter( (lang) => lang.main).map( (lang) => lang.id);
            this._cache.mainLangs = result;
        }
        return [...result];
        
    }
    getDefaultLang(){
        return this.config.lang;
    }
    getLang(id) {
        let result = this._cache[id];
        if (!result) {
            result = this.config.langs.filter( (lang) => lang.id == id)[0] || null;
            this._cache[id] = result;
        }
        return result;
    }
    getLangLabel(id) {
        let lang = this.getLang(id);
        return lang ? lang.label : id;
    }
    translate(label) {
        var translations = this._strings[this.config.lang];
        if (!translations) return label;
        var result = translations[label];
        if (!result) return label;
        return result;    
    }
    formatText(value, lang) {
        if (!value) return "";
        var key = "i18n:" + (lang || this.config.lang) + "=";
        if (value.startsWith(key)) {
            let j = value.indexOf("|");
            return value.substring(key.length, j != -1 ? j : value.length).replaceAll("&#124;","|");
        } else {
            let k = value.indexOf("|" + key);
            if (k != -1) {
                let j = value.indexOf("|", k + 1);
                return value.substring(k + key.length + 1, j != -1 ? j : value.length).replaceAll("&#124;","|");
            }
        }
        return "";   
    }
    formatDateTime(datetime, format, opts) {
        if (datetime=="") return "";
        if (typeof(datetime) == "string") datetime = new Date(datetime);
        let lang = this.config.lang;
        let options = opts || {};
        if (format == "iso") {
            if (typeof(datetime) == "number") datetime = new Date(datetime);
            let result = 
                datetime.getUTCFullYear() + "-" +
                String(datetime.getUTCMonth() + 1).padStart(2, "0") + "-" +
                String(datetime.getUTCDate()).padStart(2, "0") + "T" +
                String(datetime.getUTCHours()).padStart(2, "0") + ":" +
                String(datetime.getUTCMinutes()).padStart(2, "0") + ":" +
                String(datetime.getUTCSeconds()).padStart(2, "0") + "." +
                String(datetime.getUTCMilliseconds()).padStart(3, "0") + "Z";
            return result;
        } else if (format) {
            options = Object.assign(options, this._config.datetime.formats[format]);
            let dateTimeFormat = new Intl.DateTimeFormat(lang, options);
            return dateTimeFormat.format(datetime);
        } else {
            options = Object.assign(options, this._config.datetime.options);
            let dateTimeFormat = new Intl.DateTimeFormat(lang, options);
            return dateTimeFormat.format(datetime);
        }        
    }   
}
