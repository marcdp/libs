
// definition
export default {
    style: `
        :host {
            display:block;
            position:relative;
            width:100%;            
            background:#cccccc;
            height:var(--x-loading-height);
            border-radius:.25em;
        }
        :host div {
            display:block;

            animation: progressBar var(--x-loading-duration) ease-in-out; 
            animation-delay: var(--x-loading-delay);   
            animation-fill-mode: both; 
            animation-iteration-count: infinite;

            background:var(--x-loading-color);
            height:var(--x-loading-height);
            border-radius:.25em;
            position:absolute;
            
        }
        @keyframes progressBar {
            0% { left:0; width: 0; }
            50% { left:0; width: 100%;}
            100% {left:100%; width: 0;}
        } 
    `,
    template: `
        <div></div>
    `
}; 
