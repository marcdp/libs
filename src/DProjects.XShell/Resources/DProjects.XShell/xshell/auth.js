
// default
export default class Auth {
    
    // vars
    _config = null;
    _loader = null;
    _identity = null;

    // ctor
    constructor({config, loader}) {
        this._config = config;
        this._loader = loader;
    }

    // methods
    async login() {
        let identityConfig = this._config.getAsObject("xshell.identity");
        let identityConfigParams = this._config.getAsObject("xshell.identity.params") || {};
        let identityProviderClass = await this._loader.load("idp:" + identityConfig.provider);
        let identityProvider = new identityProviderClass();
        let identityResolveResult = await identityProvider.resolve(identityConfigParams);
        if (identityResolveResult.status == "authenticated") {
            this._identity = identityResolveResult.identity;
        } else {
            debugger;
        }
        return this._identity;
    }
    async logout() {
        this._identity = null;
        let identityConfig = this._config.getAsObject("xshell.identity");
        let identityProvider = await this._loader.load("idp:" + identityConfig.provider);
        await identityProvider.logout();
    }   
}

