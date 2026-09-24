// check whether two declarative values have the same structure and values
export function areDeclarativeValuesEqual(left, right) {
    if (left === right) {
        return true;
    }
    if (left === null || right === null || typeof(left) !== "object" || typeof(right) !== "object") {
        return false;
    }
    if (Array.isArray(left) || Array.isArray(right)) {
        if (!Array.isArray(left) || !Array.isArray(right) || left.length !== right.length) {
            return false;
        }
        return left.every((value, index) => areDeclarativeValuesEqual(value, right[index]));
    }
    if (!isPlainObject(left) || !isPlainObject(right)) {
        return false;
    }
    const leftKeys = Object.keys(left);
    const rightKeys = Object.keys(right);
    if (leftKeys.length !== rightKeys.length) {
        return false;
    }
    return leftKeys.every(key => Object.prototype.hasOwnProperty.call(right, key) && areDeclarativeValuesEqual(left[key], right[key]));
}

// build state with contract defaults as the canonical values for public properties
export function createStateSkeleton(src, definition, contract, kind) {
    const stateSkeleton = {};
    const properties = contract.properties || {};
    const state = definition.state || {};
    const name = definition.meta?.name || src;
    for (const [propName, property] of Object.entries(properties)) {
        if (property.state === true) {
            stateSkeleton[propName] = property.default;
        }
    }
    for (const [stateName, value] of Object.entries(state)) {
        const property = properties[stateName];
        if (!property) {
            stateSkeleton[stateName] = value;
            continue;
        }
        if (property.state !== true) {
            throw new Error(`${kind} '${name}' declares public property '${stateName}' in definition.state, but the contract property is not state-backed.`);
        }
        if (!areDeclarativeValuesEqual(property.default, value)) {
            throw new Error(`${kind} '${name}' declares different defaults for public property '${stateName}' in contract.properties and definition.state.`);
        }
    }
    return stateSkeleton;
}

// check that a value is a JSON-style plain object
function isPlainObject(value) {
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
}
