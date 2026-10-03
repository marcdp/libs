import validateContract from "./validation/contract.js";

// class
export default class Contracts {

    //vars
    _config = null;
    _loader = null;
    _items = null;
    
    // ctor
    constructor( { config, loader } ) {
        this._config = config;
        this._loader = loader;
    }   


    // methods
    async init() {
        // process each module's contracts
        let tasks = [];
        for (const moduleId of Object.keys(this._config.modules)) {
            const module = this._config.modules[moduleId];
            const moduleContractsPath = module.assetsPath + "/contracts";
            for (const file of Object.values(module.files)) {
                if (file.path.startsWith(moduleContractsPath)) {
                    const fileUrl = this._config.app.basePath + file.path;
                    tasks.push(this._processContractFile(fileUrl, file, moduleId, file.size));
                }
            }
        }
        const contractFiles = await Promise.all(tasks); // contracts is a list [{id:...,contract:...},{id:,contract:...},...]
        // convert the list of contracts into a cache object keyed by contract id
        const items = {};
        for (const contractFile of contractFiles) {
            items[contractFile.id] = Object.freeze(contractFile);
        }
        this._items = Object.freeze(items);
    }
    getContractItems() {
        return this._items;
    }
    getContractItemById(id) {
        return this._items ? this._items[id] : undefined;
    }
    
    getContracts() {
        return Object.values(this._items).map(item => item.contract);        
    }
    getContractById(id){
        const item = this.getContractItemById(id);
        return item ? item.contract : undefined;
    }


    // process a single contract file
    async _processContractFile(url, file, moduleId, size) {
        const filename = file.path.split("/").pop().split(".")[0];
        const start = performance.now();
        const contract = await this._loader.load("contract:" + filename);
        await validateContract(url, contract);
        return {
            id: filename,
            url: url,
            moduleId: moduleId,
            contract: contract,
            size: size,
            status: "loaded",
            time: performance.now() - start
        };
    }
    
}
