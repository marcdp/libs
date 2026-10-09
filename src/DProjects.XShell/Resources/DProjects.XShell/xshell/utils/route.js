export function compileRoute(path) {
    // compile literal segments and whole-segment parameters for route matching
    if (typeof path !== "string" || !path.startsWith("/")) {
        throw new Error(`Invalid route path '${path}': routes must start with '/'.`);
    }

    const parameters = [];
    const routeSegments = [];
    const segments = path.substring(1).split("/");
    const matcherSegments = segments.map(segment => {
        const parameter = segment.match(/^\{([A-Za-z_][A-Za-z0-9_]*)\}$/);
        if (parameter) {
            const name = parameter[1];
            if (parameters.includes(name)) throw new Error(`Invalid route path '${path}': duplicate parameter '${name}'.`);
            parameters.push(name);
            routeSegments.push({ parameter: name });
            return "([^/]+)";
        }
        if (segment.includes("{") || segment.includes("}") || segment.includes("*") || segment.includes("?")) {
            throw new Error(`Invalid route path '${path}': unsupported parameter or wildcard syntax.`);
        }
        routeSegments.push({ literal: segment });
        return segment.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    });

    return {
        path,
        parameters,
        segments: routeSegments,
        matcher: new RegExp(`^/${matcherSegments.join("/")}$`)
    };
}
