
// class
export default {
    meta: {
        title: "Sample Page 7",
        icon: "icon-sample7",
        description: "This is a sample page 7 description",
        renderEngine: "plain"
    },
    template: `
        <p>This is sample page <b>7</b><br/></p>
        <hr/>
        navigation page relative: <a href="page8.js">Link</a><br/>
        navigation module relative: <a href="/pages/page8.js">Link</a><br/>
        <hr/>
        resource page relative:<img src="../images/apple.jpg" style="width:2em"/><br/>
        resource module relative:<img src="/images/apple.jpg" style="width:2em"/><br/>
        <hr/>
        <br/>
        <button ref="btn1">Do Something</button>
    `,
    state: {
        var1: {value:"value1"},
        var2: {value:"value2"}
    },
    controller({ bus, state, timer, events }) {
        return {
            load(params) {
                //load
                state.var1 = "holaaa";
                //timer.setInterval(1000, "refresh");
                events.on(bus, "xshell", "refresh")
            },

            refresh(params) {
                // refresh
                console.log("HElllooo" + new Date());
            },

            mount(params) {
                // mount
                this.refs.btn1.innerHTML = state.var1;
                events.on(this.refs.btn1, "click", "do-something");
                this.refs.btn1.style.border = "2px solid blue";
            },

            "do-something"(params) {
                // todo ...
                alert("aaa2")
            }
        }
    }
};

