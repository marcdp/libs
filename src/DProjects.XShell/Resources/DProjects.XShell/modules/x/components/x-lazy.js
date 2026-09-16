import XElement from "x-element";
import xshell from "xshell";

// class
export default {
    style: `
        :host {display:inline;}
        :host(.no-spinner) x-spinner {display:none;}
    `,
    template: `
        <slot x-if="state.activated"></slot>
        <x-spinner x-else></x-spinner>
    `,
    state: {
        activated: {value:false}
    },
    script({ state, loader }) {
        return {
            async onCommand(command, params){
                if (command == "load") {
                    //load
                    //intersection observer
                    const onIntersection = (entries, observer) => {
                        entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            observer.disconnect(); // Stop observing once loaded
                            this.onCommand("activate");
                        }
                        });
                    };
                    // set up the IntersectionObserver
                    this._loadingObserver = new IntersectionObserver(onIntersection, {
                        rootMargin: '100px' // start loading just before it comes into view
                    });
                    // start observing the element
                    this._loadingObserver.observe(this);
                    
                } else if (command === "activate"){
                    //activate
                    let dependencies = [...new Set(Array.from(this.querySelectorAll('*')).filter(el =>{ 
                        if (el.tagName.includes('-')) {
                            if (el.tagName == "X-LAZY" || el.closest("x-lazy") == this) {
                                return true;
                            }
                        }
                        return false;    
                    }).map(el => "component:" + el.tagName.toLowerCase()))];
                    await loader.load(dependencies);
                    state.activated = true;
                } 
            }
        }
    }
}

   