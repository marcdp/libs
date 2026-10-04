
// export
export function findObjectsPath(obj, keyToFind, valueToFind) {
    // Base case: if obj is not an object, return null
    if (typeof obj !== 'object' || obj === null) return null;
    for (const key in obj) {
        if (obj.hasOwnProperty(key)) {
            // If the key matches, return the current object
            if (key == keyToFind && obj[key] == valueToFind) {
                return [obj];
            }    
            // Recursively search within child objects
            if (typeof obj[key] === 'object') {
                const result = findObjectsPath(obj[key], keyToFind, valueToFind);
                if (result) {
                    if (!Array.isArray(obj)) result.unshift(obj);
                    return result;
                }
            }
        }
    }    
    // If no matching object is found, return null
    return null;
}