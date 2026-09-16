
// class
export default class Tabs {

    // vars
    _bus = null;
    _tabId = null;
    _channel = null;
    _tabs = new Map();
    
    // ctor
    constructor( { bus } ) {
        this._bus = bus;
        // tab id
        this._tabId = crypto.randomUUID();
        // broadcast channel
        this._channel = new BroadcastChannel("xshell:tabs");
        setInterval(() => {
            this._channel.postMessage({
                type: "heartbeat",
                tabId: this._tabId,
                ts: Date.now()
            });
        }, 2000);
        // listen for messages
        this._channel.onmessage = (event) => {
            const { type, tabId, ts } = event.data;
            if (type === "heartbeat") {
                let changed = false;
                if (!this._tabs.has(tabId)) {
                    this._bus.emit("xshell:tabs:opened", tabId);
                    changed = true;
                }
                this._tabs.set(tabId, ts);
                if (changed) {
                    this._bus.emit("xshell:tabs:changed");
                }
            }
        }
        // prune tabs periodically
        setInterval(() => {
            let changed = false;
            const now = Date.now();
            for (const [id, lastSeen] of this._tabs) {
                if (now - lastSeen > 5000) {
                    this._tabs.delete(id);
                    this._bus.emit("xshell:tabs:closed", id);
                    changed = true;
                }
            }
            if (changed) {
                this._bus.emit("xshell:tabs:changed");
            }
        }, 5000);
        
    }

}

