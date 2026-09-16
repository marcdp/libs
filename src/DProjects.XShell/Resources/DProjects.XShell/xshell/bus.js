
// class
export default class Bus {


    //vars
    _channel = new MessageChannel();
    _events = {};

    //ctor
    constructor() {
        this._channel.port1.onmessage = (e) => {
            this._processEvent(e.data);
        };
    }

    //methods
    addEventListener(event, listener) {
        if (!this._events[event]) {
            this._events[event] = [];
        }
        this._events[event].push(listener);
    }
    removeEventListener(event, listener) {
        if (!this._events[event]) return;
        this._events[event] = this._events[event].filter(l => l !== listener);
    }
    emit(event, detail) {
        let data = {
            type: event,
            ts: Date.now(),
            detail: detail
        }
        this._channel.port2.postMessage(data);
    }

    // private methods
    _processEvent(event) {
        // invoke
        if (this._events[event.type]) {
            for(let listener of this._events[event.type]){
                listener(event);
            }
        };
        // broadcast
        if (this._events["*"]) {
            for(let listener of this._events["*"]){
                listener(event);
            }
        }
    }

};

