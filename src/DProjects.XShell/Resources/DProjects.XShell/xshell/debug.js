
// class
export default class Debug {


    //ctor
    constructor( ) {
    }

    //methods
    log(message, ...params) {
        this.emit("log:info", { message: message, ...params });
        console.log(message, ...params);
    }
    warn(message, ...params) {
        this.emit("log:warn", { message: message, ...params });
        console.warn(message, ...params);
    }
    error(message, ...params) {
        this.emit("log:error", { message: message, ...params });
        console.error(message, ...params);
    }
    emit(event, detail) {
        console.log(" .... ", event, detail);
    }

};
