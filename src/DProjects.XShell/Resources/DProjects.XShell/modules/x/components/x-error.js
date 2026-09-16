import XElement from "x-element";

// export
export default {
    style: `
        table {
            width: 100%;
            border-collapse: collapse;                
        }
        td {vertical-align:top; padding-right:1em;}
        td:first-child {width:5em;}
        tr.pre td {padding-top:0;}
        pre { margin-top:0.2em; font-size:0.9em; }
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
                <tr x-if="state.module">
                    <td><b>Module:</b></td>
                    <td>{{state.module}}</td>
                </tr>
                <tr class="pre" x-if="state.stack">
                    <td colspan="2">
                        <b>Stack:</b><br/>
                        <pre>{{state.stack}}</pre>
                    </td>
                </tr>
            </table>
        </x-notice>
    `,
    state: {
        code:       {value: 0,  attr:true, prop:true},
        message:    {value: "", attr:true, prop:true},
        src:        {value: "", attr:true, prop:true},
        module:     {value: "", attr:true, prop:true},
        stack:      {value: "", attr:true, prop:true}
    }
};

