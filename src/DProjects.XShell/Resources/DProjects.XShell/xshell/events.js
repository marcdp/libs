
// class
export default class Events {

    //vars
    _handler = null;
    _eventListeners =  [];

    //Ctor
    constructor(handler) {
        this._handler = handler;
    }   
    dispose(){
        for (const { element, event, listener } of this._eventListeners) {
            element.removeEventListener(event, listener);
        }
    }

    //methods
    on(element, event, listener) {
        if (typeof(listener) == "string") {
            const command = listener;
            listener = () => {this._handler(command);}
        }
        if (typeof(event) == "string") {
            event = [event];
        }
        for (let ev of event) {
            element.addEventListener(ev, listener);
            this._eventListeners.push({ element, event: ev, listener });
        }   
    }
    
}
