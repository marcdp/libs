
// export
export function getDeepActiveElement(root = document) {
    // Get the active element within the given root (document or shadow root)
    let activeElement = root.activeElement;
    
    // If there's no active element, return null
    if (!activeElement) {
        return null;
    }      
    // If the active element has a shadow root, dive deeper
    if (activeElement.shadowRoot) {
        return getDeepActiveElement(activeElement.shadowRoot);
    }      
    // If active element is inside a shadow DOM slot, check its assigned elements
    if (activeElement.tagName === 'SLOT') {
        const assignedElements = activeElement.assignedElements({ flatten: true });
        for (const assignedElement of assignedElements) {
        if (assignedElement.shadowRoot) {
            const nestedActive = getDeepActiveElement(assignedElement.shadowRoot);
            if (nestedActive) {
            return nestedActive;
            }
        }
        }
    }      
    // Return the active element if it's the deepest focused element
    return activeElement;
}

export function isDescendantOfElement(ancestor, element) {
    // Traverse through the ancestors of the element
    while (element) {

        if (element instanceof DocumentFragment) {
            element = element.host;
        }

        // Check if the current element is the ancestor
        if (element === ancestor) {
            return true;
        }
        
        // Check if the current element is inside a shadow DOM
        if (element.shadowRoot) {
            // Check if the ancestor is in the shadow DOM of the element
            if (element.shadowRoot.host === ancestor) {
                return true;
            }
        }
        
        // Move to the parent node
        element = element.parentNode;
    }
    
    return false;
} 

export function findFocusableElement(element) {
    const focusableSelectors = [
        'a[href]', 
        'button:not([disabled])', 
        'textarea:not([disabled])', 
        'input:not([disabled])', 
        'select:not([disabled])', 
        '[tabindex]:not([tabindex="-1"])'
    ];

    // Helper to determine if an element is focusable
    const isFocusable = (el) => el.matches && el.matches(focusableSelectors.join(','));

    // Check if the current element is focusable
    if (isFocusable(element)) {
        return element;
    }

    // Traverse light DOM children
    const children = element.children;
    for (let i = 0; i < children.length; i++) {
        const child = children[i];

        // Check shadow root if the element has it
        if (child.shadowRoot) {
            // Handle shadow DOM
            const focusableInShadow = findFocusableElement(child.shadowRoot);
            if (focusableInShadow) return focusableInShadow;

            // Check for slots and their assigned nodes
            const slots = child.shadowRoot.querySelectorAll('slot');
            for (let slot of slots) {
                const assignedNodes = slot.assignedNodes({ flatten: true });
                for (let node of assignedNodes) {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        const focusableInSlot = findFocusableElement(node);
                        if (focusableInSlot) return focusableInSlot;
                    }
                }
            }
        }

        // Handle light DOM children
        const focusableInLight = findFocusableElement(child);
        if (focusableInLight) return focusableInLight;
    }

    // If no focusable element is found, return null
    return null;
}    
export function probablyPhone() {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
}