
// class
export default {
    meta: {
        title: "Page 8",
        description: "This is a sample page 8 description"
    },
    state: {
        varMarc1: { value: 0,  qs: true, reflect:true },
        var2: { value: 0 }
    },
    template: `
        <p>
            This is sample page 8: {{state.varMarc1}}
            <br/>
            <x-button label="Do something" command="doSomething"></x-button>
            <br/>
            <x-button label="Do message" command="doMessage"></x-button>
            <x-button label="Do confirm" command="doConfirm"></x-button>
            <x-button label="Do prompt" command="doPrompt"></x-button>
            <x-button label="Do picker" command="doPicker"></x-button>
            <x-button label="Do lang" command="doLanguage"></x-button>
        </p>
    `,
    script({ state, dialog}) {
        return {
            async onCommand(command, params){
                if (command == "doSomething") {
                    // dosomething
                    state.varMarc1++;
                    state.var2++;

                } else if (command == "doMessage") {
                    // message
                    await dialog.message({title:"hello0", message:"Lorem ipsum asd kjsadhg ajsdhg ajsdgha kdsbye", type:"warning"})
                    
                } else if (command == "doConfirm") {
                    // confirm
                    const result = await dialog.confirm({title:"hello0", message:"bye", variant:"ok"})
                    alert(result)

                } else if (command == "doPrompt") {
                    // prompt
                    const result = await dialog.prompt({title:"hello0", message:"bye", required:true})
                    alert(result)

                } else if (command == "doPicker") {
                    // picker
                    const result = await dialog.picker({title:"hello0", message:"bye", domain:[{value:1,label:"11111"},{value:2,label:"2222"}], required:true})
                    alert(result)

                } else if (command == "doLanguage") {
                    // language
                    const result = await dialog.language()
                    alert(result)
                }
            }
        }
    }

};

