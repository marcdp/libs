// contract
export const contract = {
    description: "Displays a placeholder while content is loading.",
    events: {},
    properties: {
        width:  {type:"string", default:"100%", attribute:true, state:true, description:"The width of the skeleton placeholder."},
        height: {type:"string", default:"1em", attribute:true, state:true, description:"The height of the skeleton placeholder."},
        radius: {type:"string", default:".25em", attribute:true, state:true, description:"The border radius of the skeleton placeholder."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:block;
        }

        .skeleton {
            display:block;
            position:relative;
            overflow:hidden;

            background:var(--x-skeleton-background, var(--x-color-xxxx-gray));

            width:100%;
            height:1em;
            border-radius:.25em;
        }

        .skeleton::after {
            content:"";
            position:absolute;
            inset:0;

            transform:translateX(-100%);

            background:linear-gradient(
                90deg,
                transparent,
                var(--x-skeleton-highlight, rgba(255,255,255,.55)),
                transparent
            );

            animation:x-skeleton-loading
                var(--x-skeleton-duration, 1.5s)
                ease-in-out
                infinite;
        }

        @keyframes x-skeleton-loading {
            100% {
                transform:translateX(100%);
            }
        }
    `,

    template: `
        <div
            class="skeleton"
            x-style:width="state.width"
            x-style:height="state.height"
            x-style:border-radius="state.radius">
        </div>
    `
};
