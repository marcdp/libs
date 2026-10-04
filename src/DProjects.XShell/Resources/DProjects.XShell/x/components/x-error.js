// contract
export const contract = {
    description: "Displays an error code, message, source, module, and stack trace.",
    events: {},
    properties: {
        code:    {type:"number", default:0, attribute:true, state:true, description:""},
        message: {type:"string", default:"", attribute:true, state:true, description:""},
        src:     {type:"string", default:"", attribute:true, state:true, description:""},
        moduleId:{type:"string", default:"", attribute:true, state:true, description:""},
        areaId:  {type:"string", default:"", attribute:true, state:true, description:""},
        stack:   {type:"string", default:"", attribute:true, state:true, description:""},
        errors:  {type:"object", default:null, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        table {
            width: 100%;
            border-collapse: collapse;                
        }
        td {vertical-align:top; padding-right:1em;}
        td:first-child {width:5em;}
        tr.pre td {padding-top:0;}
        pre { margin-top:0.2em; font-size:0.9em; white-space: pre-wrap; max-width:90%}
        @media (max-width: 768px) { 
            table {display:block; margin-left:.25em; margin-bottom:.25em;}
            tr {display:block;}
            td {display:block;word-break:break-word;}
            pre {white-space: break-spaces;}
            tr.pre td:last-child {padding-top:0em;}
        }
    `,
    template: `
        <x-notice type="error">
            <div >
                <table>
                    <tr>
                        <td><b>Error:</b></td>
                        <td>{{state.code}}</td>
                    </tr>
                    <tr>
                        <td><b>Message:</b></td>
                        <td>{{state.message}}</td>
                    </tr>
                    <tr>
                        <td><b>Page:</b></td>
                        <td>{{state.src}}</td>
                    </tr>
                    <tr x-if="state.moduleId">
                        <td><b>Module:</b></td>
                        <td>{{state.moduleId}}</td>
                    </tr>
                    <tr x-if="state.areaId">
                        <td><b>Area:</b></td>
                        <td>{{state.areaId}}</td>
                    </tr>
                    <tr class="pre" x-if="state.stack">
                        <td colspan="1"><b>Stack:</b><br/></td>
                        <td><pre>{{state.stack}}</pre></td>
                    </tr>
                </table>
                <div x-if="state.errors">
                    <b>Errors:</b>
                    <ul>
                        <li x-for="error in state.errors">
                            <x-json x-prop:value="error" indent="2"></x-json>
                        </li>
                    </ul>                    
                </div>
            </div>
        </x-notice>
    `,
    state: {
    }
};

