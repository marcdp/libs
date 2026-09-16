// export
export default class IdpConfig {
    async resolve(params) {
        // always return anonymous identity
        return {
            status: "authenticated",
            identity: Object.freeze({
                id: params.id || "anonymous", 
                name: params.name || "Anonymous",
                roles: params.roles || [],
                claims: params.claims || {}
            })
        };
    }
    logout() {
        // reload document
        location.reload();
    }
};