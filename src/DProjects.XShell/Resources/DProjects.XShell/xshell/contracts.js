import validateContract from "./validation/contract.js";
import { deepFreeze } from "./utils/object.js";

// class
export default class Contracts {

    //vars
    _config = null;
    _loader = null;
    _items = null;
    
    // props
    get registry() {
        // expose contract metadata without returning mutable contract documents
        return Object.freeze(Object.values(this._items || {}).map(item => Object.freeze({
            id: item.id,
            url: item.url,
            path: item.path,
            moduleId: item.moduleId,
            label: item.contract.label || "",
            description: item.contract.description || "",
            icon: item.contract.icon || null,
            size: item.size,
            status: item.status,
            time: item.time
        })));
    }

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
                if (file.path.startsWith(moduleContractsPath + "/") && file.path.endsWith(".json")) {
                    const contractId = file.path.substring(moduleContractsPath.length + 1, file.path.length - ".json".length);
                    const fileUrl = this._config.app.basePath + file.path;
                    tasks.push(this._processContractFile(contractId, fileUrl, file, moduleId, file.size));
                }
            }
        }
        const contractFiles = await Promise.all(tasks); // contracts is a list [{id:...,contract:...},{id:,contract:...},...]
        // convert the list of contracts into a cache object keyed by contract id
        const items = {};
        for (const contractFile of contractFiles) {
            if (items[contractFile.id]) {
                const first = items[contractFile.id];
                throw new Error(`Duplicate contract '${contractFile.id}' declared by module '${first.moduleId}' in '${first.path}' and module '${contractFile.moduleId}' in '${contractFile.path}'.`);
            }
            items[contractFile.id] = Object.freeze(contractFile);
        }
        this._items = Object.freeze(items);
    }
    getContractItemById(id) {
        return this._items ? this._items[id] : undefined;
    }
    getContractById(id){
        const item = this.getContractItemById(id);
        return item ? item.contract : undefined;
    }


    // process a single contract file
    async _processContractFile(id, url, file, moduleId, size) {
        const start = performance.now();
        const contract = await this._loader.load("contract:" + id);
        await validateContract(url, contract);
        deepFreeze(contract);
        return {
            id: id,
            url: url,
            path: file.path,
            moduleId: moduleId,
            contract: contract,
            size: size,
            status: "loaded",
            time: performance.now() - start
        };
    }
    
}
