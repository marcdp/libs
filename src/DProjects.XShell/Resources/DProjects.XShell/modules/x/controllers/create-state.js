// func
function createState(value, self) {
    let result = new Proxy(value, {
        listeners:[],
        get: function(target, prop, receiver) {
            if (prop == "addEventListener") {
                return (name, callback) => {
                    this.listeners.push({name, callback});
                };
            } else if (prop == "removeEventListener") {
                return (name, callback) => {
                    for(let i = 0; i < this.listeners.length; i++) {
                        if (this.listeners[i].name == name && this.listeners[i].callback == callback) {
                            this.listeners.splice(i, 1);
                            break;
                        }
                    }
                };                
            } else {
                return target[prop];
            }
        },
        set(target, prop, newValue) {
            let oldValue = target[prop];
            let changed = (oldValue != newValue);
            if (!changed && oldValue && newValue) {
                if (Array.isArray(newValue)) { //dirty check: check if number of elements are the same
                    changed = (oldValue.length != newValue.length);
                } else if (typeof(newValue) == "object") { //dirty check: check if keyslength  are the same
                    changed = (Object.keys(oldValue).length != Object.keys(newValue).length);
                }
            }
            if (changed) {
                target[prop] = newValue;
                self.stateChange(prop, oldValue, newValue);
                for(let listener of this.listeners) {
                    if (listener.name == "change") {
                        listener.callback({name: listener.name, prop, oldValue, newValue});
                    } else if (listener.name == "change:" + prop) {
                        listener.callback({name: listener.name, prop, oldValue, newValue});
                    }
                }
                self.invalidate();
            }
            return true;
        }
    });
    return result;
} 

// export
export default createState;