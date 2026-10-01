
// class
export default class Contracts {

    //vars
    _config = null;
    _items = null;
    
    // ctor
    constructor( { config } ) {
        this._config = config;
    }   

    // props
    get items() {
        return this._items;
    }

    // methods
    async init() {
        // for each module
        let tasks = [];
        for (const module of Object.values(this._config.modules)) {
            // process each module's contracts
            const moduleContractsPath = module.assetsPath + "/contracts";
            for (const file of Object.values(module.files)) {
                if (file.path.startsWith(moduleContractsPath)) {
                    tasks.push(this._processContractFile(file));
                }
            }
        }
        const contracts = await Promise.all(tasks); // contracts is a list [{id:...,contract:...},{id:,contract:...},...]
        // convert the list of contracts into a cache object keyed by contract id
        const items = {};
        for (const contract of contracts) {
            items[contract.id] = Object.freeze(contract.content);
        }
        this._items = Object.freeze(items);
    }


    // process a single contract file
    async _processContractFile(file) {
        const filename = file.path.split("/").pop();
        const response = await fetch(file.path);
        const result = await response.json();
        return {
            id: filename,
            content: result
        };
    }
    
}
