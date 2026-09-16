
// class
export default class Timer {

    // vars
    _handler = null;
    _timeouts = [];
    _intervals = [];

    // ctor
    constructor(handler) {
        this._handler = handler;
    }   
    dispose(){
        for (const { timeout, timeoutId, listener } of this._timeouts) {
            clearTimeout(timeoutId);
        }
        this._timeouts = [];
        for (const { timeout, timeoutId, listener } of this._intervals) {
            clearTimeout(timeoutId);
        }
        this._intervals = [];
    }

    // methods
    setTimeout(timeout, listener) {
        if (typeof(listener) == "string") {
            const command = listener;
            listener = () => {this._handler(command);}
        }
        this._timeouts.push({ timeout, timeoutId: setTimeout(listener, timeout), listener });
    }
    setInterval(timeout, listener) {
        if (typeof(listener) == "string") {
            const command = listener;
            listener = () => {this._handler(command);}
        }
        this._intervals.push({ timeout, timeoutId: setInterval(listener, timeout), listener });
    }

    
}
