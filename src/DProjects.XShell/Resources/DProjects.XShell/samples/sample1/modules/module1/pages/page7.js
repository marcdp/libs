
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
    script({ bus, state, timer, events }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    //load
                    state.var1 = "holaaa";
                    //timer.setInterval(1000, "refresh");
                    events.on(bus, "xshell", "refresh")
                    
                } else if (command == "refresh") { 
                    // refresh
                    console.log("HElllooo" + new Date());

                } else if (command == "mount") { 
                    // mount
                    this.refs.btn1.innerHTML = state.var1;
                    events.on(this.refs.btn1, "click", "do-something");
                    this.refs.btn1.style.border = "2px solid blue";

                } else if (command == "do-something") { 
                    // todo ...
                    alert("aaa2")
                }
            }
        }
    }
};

