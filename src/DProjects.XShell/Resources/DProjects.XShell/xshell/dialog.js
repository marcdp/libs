
export default class Dialog {

    // vars
    _config = null;
    _navigation = null;
    _i18n = null;

    // ctor
    constructor( { config, navigation, i18n } ) {
        this._config = config;
        this._navigation = navigation;
        this._i18n = i18n;
    }

    // methods
    async confirm({ title, message, variant }) {
        const href = this._config.get("xshell.dialog.confirm");
        return await this._navigation.navigate({ 
            href, 
            open:"dialog", 
            context: {
                title,
                message,
                variant
            } 
        });
    }
    async message({ type, title, message }    ) {
        const href = this._config.get("xshell.dialog.message");
        return await this._navigation.navigate({ 
            href, 
            open:"dialog",
            context: {
                type,
                title,
                message
            } 
        });
    }
    async prompt({ title, message, defaultValue, inputType, placeholder, required }) {
        const href = this._config.get("xshell.dialog.prompt");
        return await this._navigation.navigate({ 
            href, 
            open:"dialog",
            context: {
                title,
                message,
                defaultValue, 
                inputType, 
                placeholder, 
                required  
            } 
        });
    }
    async picker({ title, message, defaultValue, inputType, placeholder, domain, multiple, required }) {
        const href = this._config.get("xshell.dialog.picker");
        return await this._navigation.navigate({ 
            href, 
            open:"dialog",
            context: {
                title,
                message,
                domain,
                defaultValue, 
                inputType, 
                placeholder, 
                multiple,
                required  
            } 
        });
    }
    async language({ title = "Select language", message = "Language", defaultValue, required=true } =  {}) {
        let domain = [];
        for (let item of this._i18n.config.langs) {
            domain.push({
                value: item.id,
                label: item.label + " (" + item.id + ")"
                //disabled: (disabled.indexOf(item.id) != -1),
            });
        }
        return await this.picker({ title, message, inputType: "radios", domain, required});
    }
    async open({ href, params, title, breadcrumb, icon, context }) {
        return await this._navigation.navigate({ 
            href, 
            params,
            open:"dialog",
            nav: { 
                title,
                breadcrumb,
                icon
            }, 
            context
        });
    }
}

