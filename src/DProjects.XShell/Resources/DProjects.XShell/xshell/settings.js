
// class
export default class Settings {

    //ctor
    constructor() {
    }

    //methods
    setItem(key, value) {
        localStorage.setItem(key, value);
    }
    getItem(key, defaultValue) {
        let value = localStorage.getItem(key);
        if (value == undefined) {
            value = defaultValue;
        } else {
            value = JSON.parse(value);  
        }
        return value;
    }    

};


