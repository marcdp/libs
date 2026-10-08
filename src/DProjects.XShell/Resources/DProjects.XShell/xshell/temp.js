// class
export default class Temp {

    // vars
    _url = null;

    // ctor
    constructor({ config }) {
        this._url = config.xshell.temp.url.replace(/\/+$/, "");
    }

    // methods
    async upload(files, onProgress) {
        if (files instanceof File) files = [files];
        const tasks = [];
        for (const file of files) {
            tasks.push(this._upload(file, onProgress).catch(error => "error: " + error.message));
        }
        return await Promise.all(tasks);
    }
    getAbsoluteUrl(url) {
        // replace urls like "temp:/folder/filename.txt?size=123" to real urls like "https://example.com/temp/folder/filename.txt"
        return this._url + "/" + url.substring(6); // remove "temp:/" prefix        
    }

    // methods (private)
    async _upload(file, onProgress) {
        return new Promise((resolve, reject) => {

            // prepare request
            const xhr = new XMLHttpRequest();

            // progress
            xhr.upload.addEventListener("progress", event => {
                if (event.lengthComputable) {
                    const percent = event.loaded / event.total * 100;
                    onProgress?.({ name: file.name, loaded: event.loaded, total: event.total, percent });
                }
            });

            // load
            xhr.addEventListener("load", () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(xhr.responseText);
                } else {
                    reject(new Error(`HTTP ${xhr.status}: ${xhr.statusText}`));
                }
            });

            // error
            xhr.addEventListener("error", () => {
                reject(new Error("Upload failed"));
            });

            // set up form data and send the request
            xhr.open("POST", this._url);
            const formData = new FormData();
            formData.append("file", file);
            xhr.send(formData);
        });
    }
}