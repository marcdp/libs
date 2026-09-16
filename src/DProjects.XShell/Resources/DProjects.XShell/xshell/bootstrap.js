// consts
const meta = name => document.head.querySelector(`meta[name="${name}"]`)?.content;
const configUrl = meta("xshell.app_config_url");
const swUrl = meta("xshell.sw_url");
const appUrl = document.location.origin + document.location.pathname;
const appUrlDir = appUrl.substring(0, appUrl.lastIndexOf("/") + 1)  ;
const appUrlBase = appUrlDir.substring(0, appUrlDir.length - 1);
const bootstrapUrl = new URL(document.currentScript.src);
const bootstrapUrlDir = bootstrapUrl.href.substring(0, bootstrapUrl.href.lastIndexOf("/") );

// utils
function stripJsonComments(s) {let o="",i=0,n=s.length;for(;i<n;){let c=s[i];if(c=='"'||c=="'"){let q=c;o+=c;i++;while(i<n){if(s[i]=="\\"){o+=s[i++]+s[i++];}else if(s[i]==q){o+=s[i++];break;}else{o+=s[i++];}}continue;}if(c=='/'&&s[i+1]=='/'){i+=2;while(i<n&&s[i]!="\n"&&s[i]!="\r")i++;continue;}if(c=='/'&&s[i+1]=='*'){i+=2;while(i<n&&!(s[i]=='*'&&s[i+1]=='/'))i++;i+=2;continue;}o+=c;i++;};return o;}
function combineUrls(t,n){if(-1!=t.indexOf("?")&&(t=t.substring(0,t.indexOf("?"))),-1!=n.indexOf(":"))return n;if(n.startsWith("/")){if(-1!=t.indexOf("://")){let i=t.indexOf("/",t.indexOf("://")+3);return-1!=i&&(t=t.substring(0,i)),t+n}return n}if(n.startsWith("./")||"."==n)return t.endsWith("/")?t=t.substring(0,t.length-1):t.length>0&&(t=t.substring(0,t.lastIndexOf("/"))),t+n.substring(1);if(n.startsWith("../")){t.endsWith("/")?t=t.substring(0,t.length-1):t.length>0&&(t=t.substring(0,t.lastIndexOf("/")));let i=t+"/"+n;if(i.startsWith("/")){i=new URL(i,window.location.origin).pathname}else i=new URL(i).toString();return i}return t.endsWith("/")||-1!=t.indexOf("/")&&(t=t.substring(0,t.lastIndexOf("/")+1)),t+n}
function normalizeUrls(key, obj, path) {if (typeof(obj) == "string") {if (obj.startsWith("url:")) {obj = obj.substring(4).trim();if (obj.startsWith("/") || obj.startsWith("./") || obj.startsWith("../") || obj == ".") {obj = combineUrls(path, obj);}} else if (obj.startsWith("/")) {obj = path + obj;if (obj.startsWith(document.location.origin)) obj = obj.substring(document.location.origin.length);} else if (obj.startsWith("./") || obj.startsWith("../") || obj == ".") {obj = combineUrls(path + "/", obj);if (obj.startsWith(document.location.origin)) obj = obj.substring(document.location.origin.length);}if (obj.indexOf("=/") != -1) {let parts = obj.split(";");for (let i = 1; i < parts.length; i++) {if (parts[i].indexOf("=/") != -1) {let subparts = parts[i].split("=");parts[i] = subparts[0] + "=" + normalizeUrls("", subparts[1].trim(), path);}}obj = parts.join(";");}} else if (Array.isArray(obj)) {for (let i = 0; i < obj.length; i++) {obj[i] = normalizeUrls(i, obj[i], path);}} else if (obj instanceof Object) {for (let subkey in obj) {obj[subkey] = normalizeUrls(subkey, obj[subkey], path);}}return obj;}

// bootstrap methods
async function loadXShellConfig(config) {
    // load app config
    console.log("bootstrap: loading xshell config ...");
    const url = bootstrapUrlDir + "/xshell.jsonc";
    const json = stripJsonComments(await (await fetch(url)).text());
    const xshellConfig = JSON.parse(json);
    const assetsPrefix = xshellConfig["xshell.assetsPrefix"];
    config["xshell.src"] = url;
    normalizeUrls("", xshellConfig, "/" + assetsPrefix + "/xshell");
    for(let key in xshellConfig) {
        config[key] = xshellConfig[key];
    }
    return config;
}
async function loadAppConfig(config) {
    // load app config
    console.log("bootstrap: loading app config ...");
    const url = new URL(configUrl, document.baseURI).href;
    const json = stripJsonComments(await (await fetch(url)).text());
    const appConfig = JSON.parse(json);
    normalizeUrls("", appConfig, appUrlDir);
    for(var key in appConfig) {
        config[key] = appConfig[key];
    }
    config["app.base"] = appUrlBase;
    return config;
}
async function loadModulesConfig(config) {
    const assetsPrefix = config["xshell.assetsPrefix"];
    // load modules config
    let names = [];
    for(let key in config) {
        if (key.startsWith("modules.")) {
            let name = key.split(".")[1];
            if (!names.includes(name)) {
                names.push(name);
            }
        }
    }
    // load each module config
    let loadModuleTasks = [];
    for(let name of names) {
        const moduleSrc = config[`modules.${name}.src`];
        console.log("bootstrap: loading module config ...", name + " (" + moduleSrc + ")");
        loadModuleTasks.push(fetch(moduleSrc));
    }
    let results = await Promise.all(loadModuleTasks);
    // parse each module config
    for(let result of results) {
        let name = names[results.indexOf(result)];
        if (!result.ok) throw new Error(`Failed to load module: ${result.url}`);
        let json = await result.text();
        let moduleConfig = JSON.parse(stripJsonComments(json));
        // defaults
        moduleConfig[`name`] = `${name}`;
        moduleConfig[`icon`] = moduleConfig[`icon`] || "x-file";
        moduleConfig[`label`] = moduleConfig[`label`] || "";
        moduleConfig[`version`] = moduleConfig[`version`] || "";
        moduleConfig[`depends`] = moduleConfig[`depends`] || [];
        moduleConfig[`styles`] = moduleConfig[`styles`] || [];
        // normalize urls
        normalizeUrls("", moduleConfig, "/" + assetsPrefix + "/" + name);
        // merge module config into app config
        for(let key in moduleConfig) {
            let value = moduleConfig[key];
            if (key.startsWith("global.")){
                key = key.substring(key.indexOf(".")+1).replaceAll("{module}", name);
                value = value.replaceAll("{module}", name).trim()
                if (key.startsWith("resolver.")) {
                    value += `${(!value.endsWith(";") ? ";": "")} module=${name}; modulePath=/${assetsPrefix}/${name};`;
                }
                config[key] = value;
            } else {
                config["modules." + name + "." + key] = value;
            }
        }
        // add resolvers
        config[`resolver.icon:${name}-{name}`] = `/${assetsPrefix}/${name}/icons/{name}.svg; loader=icon-svg; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.layout:${name}-layout-{name}`] = `/${assetsPrefix}/${name}/layouts/${name}-layout-{name}.js; loader=component-js; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.component:${name}-{name}`] = `/${assetsPrefix}/${name}/components/${name}-{name}.js; loader=component-js; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.page:/${assetsPrefix}/${name}/{path}.js`] = `/${assetsPrefix}/${name}/{path}.js; loader=page-js; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.page:/${assetsPrefix}/${name}/{path}.html`] = `/${assetsPrefix}/${name}/{path}.html; loader=page-html; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.page:/${assetsPrefix}/${name}/{path}.md`] = `/${assetsPrefix}/${name}/{path}.md; loader=page-md; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
        config[`resolver.module:/${assetsPrefix}/${name}/{path}.js`] = `/${assetsPrefix}/${name}/{path}.js; loader=module-js; cache=true; module=${name}; modulePath=/${assetsPrefix}/${name};`;
    }
    // log
    console.log("bootstrap: config", config);
    // return
    return config;
}
async function installServiceWorker(config) {
    // install service worker
    console.log("bootstrap: installing service worker ...");
    const bootstrapUrlRaw = bootstrapUrl.toString();
    const realSwUrl = bootstrapUrlRaw.substring(0, bootstrapUrlRaw.lastIndexOf("/")) + "/sw.js";
    const reg = await navigator.serviceWorker.register(swUrl + "?" + encodeURIComponent(realSwUrl), {
        scope: document.location.pathname
    });
    // creates rules to send to service worker
    const xshellVersion = config["xshell.version"];
    const assetsPrefix = config["xshell.assetsPrefix"];
    let rules = [];
    rules.push({ src: combineUrls(appUrl, assetsPrefix + "/xshell"), dst: bootstrapUrlDir, version: xshellVersion, name:"xshell", exceptions:[bootstrapUrlDir + "/xshell.jsonc"]});
    for(var key in config) {
        if (key.startsWith("modules.") && key.endsWith(".src")) {
            const moduleName = key.split(".")[1];
            const moduleSrc = config[key];
            const moduleSrcDir = moduleSrc.substring(0, moduleSrc.lastIndexOf("/"));
            const moduleVersion = config[`modules.${moduleName}.version`];
            rules.push({ src: combineUrls(appUrl, assetsPrefix + "/" + moduleName), dst: moduleSrcDir, version: moduleVersion, name: moduleName, exceptions: [moduleSrc]});
        }
    }
    // wait for ready
    console.log("bootstrap: waiting for ready ...");
    await navigator.serviceWorker.ready;
    // ensure page is controlled by service worker
    if (!navigator.serviceWorker.controller) {
        console.log("bootstrap: page is not controlled ... forcing reload");
        location.reload();
        return false;
    }
    // send init message to service
    console.log("bootstrap: send init message to service worker ...");
    await new Promise((resolve, reject) => {
        const channel = new MessageChannel();
        channel.port1.onmessage = (event) => {
            resolve(event.data);
            channel.port1.close();
        };
        // safety timeout
        setTimeout(() => {
            reject(new Error("Service Worker did not reply in time"));
            channel.port1.close();
        }, 5000);
        // send init + transfer reply port
        reg.active.postMessage({ type: "init", payload: {rules: rules} }, [channel.port2]);
    });
    //
    console.log("bootstrap: service worker ready to receive requests");
    // return
    return true;
}
async function loadXShell(config) {
    // load/import XShell
    console.log("bootstrap: loading xshell ...");
    return (await import("xshell")).default;
}
async function bootstrap() {
    let config = {};
    // load xshell config
    config = await loadXShellConfig(config);
    // load app config
    config = await loadAppConfig(config);
    // load modules config
    config = await loadModulesConfig(config);
    // installServiceWorker
    if (!await installServiceWorker(config)){
        return;
    }
    // create importmap
    let imports = {};
    for (let key in config) {
        if (key.startsWith("resolver.import:")) {
            let importName = key.substring(key.indexOf(":") + 1);
            let importSrc = config[key].split(";")[0].trim();
            imports[importName] = (importSrc.indexOf(":")!=-1 ? importSrc : appUrlDir + importSrc.substring(1));
        }
    }
    const importMap = document.createElement("script");
    importMap.type = "importmap";
    importMap.textContent = JSON.stringify({ imports }, null, 2);
    document.head.appendChild(importMap);
    // load xshell
    let xshell = await loadXShell(config);
    // init xshell
    await xshell.init(config);
}

// exec bootstrap
bootstrap();

