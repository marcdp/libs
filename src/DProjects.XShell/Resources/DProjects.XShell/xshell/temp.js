// class
export default class Temp {

    // vars
    _url = null;

    // ctor
    constructor({ url = "/temp" } = {}) {
        this._url = url.replace(/\/+$/, "");
    }

    // methods
    async upload(files) {
        if (files instanceof File) {
            return await this._upload(files);
        }
        const result = [];
        for (const file of files) {
            result.push(await this._upload(file));
        }
        return result;
    }

    // methods (private)
    async _upload(file) {
        if (!(file instanceof File)) {
            throw new TypeError("Temp.upload expects a File or an iterable of File objects");
        }

        const form = new FormData();
        form.append("file", file, file.name);

        const response = await fetch(this._url, {
            method: "POST",
            body: form
        });

        if (!response.ok) {
            throw new Error(`Temp upload failed: ${response.status} ${response.statusText}`);
        }

        return await response.json();
    }
}