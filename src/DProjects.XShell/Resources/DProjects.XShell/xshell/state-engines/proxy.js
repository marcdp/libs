// export
export default function createStateEngineFactoryProxy(stateSkeleton, stateDefinition) {
	// init state definition json
	const stateSkeletonJson = JSON.stringify(stateSkeleton || {});
	// returns a state instance factory
	return {
		create: (handler) => {
			const value = JSON.parse(stateSkeletonJson);
			let listeners = [];
			return new Proxy(value, {
				get: function(target, prop, receiver) {
					// return real value
					if (prop == "addEventListener") {
						return function(event, listener) {
							if (event.startsWith("change:")) {
								let prop = event.substring("change:".length);
								listeners.push({prop: prop, listener});
							}
						}
					} else if (prop == "removeEventListener") {
						return function(event, listener) {
							if (event.startsWith("change:")) {
								let prop = event.substring("change:".length);
								listeners = listeners.filter(l => l.prop !== prop || l.listener !== listener);
							}
						}
					}
					return target[prop];
				},
				set(target, prop, newValue) {
					// set value and notify handler
					let oldValue = target[prop];
					let changed = (oldValue != newValue);
					if (!changed && oldValue && newValue) {
						if (Array.isArray(newValue)) { 
							//dirty check: check if number of elements are the same
							changed = (oldValue.length != newValue.length);
						} else if (typeof(oldValue) == "object" && typeof(newValue) == "object") { 
							//supose objects has changed
							changed = true;
						} else if (typeof(newValue) == "object") { 
							//dirty check: check if keyslength  are the same
							changed = (Object.keys(oldValue).length != Object.keys(newValue).length);
						}
					}
					if (changed) {
						target[prop] = newValue;
						for (let listener of listeners) {
							if (listener.prop === prop) {
								listener.listener({ prop, newValue, oldValue});
							}
						}
						handler.stateChange(prop, oldValue, newValue);
						handler.invalidate(prop);
					}
					return true;
				}
			});
		}
	};
}